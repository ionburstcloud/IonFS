// Copyright Ionburst Limited 2018-2021

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Linq;
using Ionburst.Apps.IonFS;
using Ionburst.Apps.IonFS.Model;
using Ionburst.Apps.IonFS.Exceptions;
using Figgle;
using System.Reflection;
using System.Text.RegularExpressions;
using System.CommandLine.NamingConventionBinder;

namespace IonFS
{
    class Program
    {
        private static Command Secrets()
        {
            Command command = new Command("secrets", "manage secrets");
            return command;
        }

        private static Command List()
        {
            var folderArgument = new Argument<String>("folder", "path in the remote file system")
                {Arity = ArgumentArity.ZeroOrOne};
            var recursiveOption = new Option<bool>(new[] {"--recursive", "-r"});
            var quietOption = new Option<bool>(new[] {"--quiet", "-q"});

            Command command = new("list", "show the contents of a remote path, prefix remote paths with ion://")
            {
                folderArgument,
                recursiveOption,
                quietOption               
            };
            command.AddAlias("ls");
            command.SetHandler(async (folder, recursive, quiet) =>
            {
                try
                {
                    IonburstFS fs = new IonburstFS();
                    IonFSObject fso = fs.FromRemoteFolder(folder);

                    if (!quiet)
                        Console.WriteLine(Logo());

                    List<IonFSObject> items = await fs.ListAsync(fso, recursive);

                    if (!fso.IsSecret)
                    {
                        Console.WriteLine($"Directory of {fso}\n");
                        if (items.Count == 0)
                        {
                            Console.WriteLine(" Remote directory is empty");
                        }
                        else
                        {
                            int maxLen = items.Max(i => i.FullName.Length);
                            
                            var folders = items.Where(i => i.IsFolder);
                            var files = items.Where(i => !i.IsFolder);
                            foreach (var item in folders)
                            {
                                Console.WriteLine("{0} {1} {2}",
                                    item.IsFolder ? "d" : " ", item.FullName.PadRight(maxLen + 2, ' '),
                                    !item.IsFolder ? item.LastModified.ToString() : "");
                            }
                            foreach (var item in files)
                            {
                                Console.WriteLine("{0} {1} {2}",
                                    item.IsFolder ? "d" : " ", item.FullName.PadRight(maxLen + 2, ' '),
                                    !item.IsFolder ? item.LastModified.ToString() : "");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Secrets Repo of {fso}\n");
                        if (items.Count == 0)
                        {
                            Console.WriteLine(" Remote Secrets repository is empty");
                        }
                        else
                        {
                            int maxLen = items.Max(i => i.FullName.Length);
                            foreach (var item in items)
                            {
                                Console.WriteLine("{0} {1} {2}",
                                    item.IsFolder ? "d" : " ", item.FullName.PadRight(maxLen + 2, ' '),
                                    !item.IsFolder ? item.LastModified.ToString() : "");
                            }
                        }
                    }
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, folderArgument, recursiveOption, quietOption);

            return command;
        }

        private static Command Get()
        {
            var fromArgument = new Argument<string>("from", "remote path to the source data")
                {Arity = ArgumentArity.ExactlyOne};
            var toArgument = new Argument<string>("to", "optional local target name")
                {Arity = ArgumentArity.ZeroOrOne};

            var nameOption = new Option<string>(new[] {"--name", "-n"});
            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});
            var keyOption = new Option<string>(new[] {"--key", "-k"}, "path to symmetric key");
            var passphraseOption = new Option<string>(new[] {"--passphrase", "-pp"}, "passphrase to generate key");

            Command command = new Command("get", "download a file, prefix remote paths with ion://");
            command.Add(fromArgument);
            command.Add(toArgument);
            command.Add(nameOption);
            command.Add(verboseOption);
            command.Add(keyOption);
            command.Add(passphraseOption);
            command.SetHandler(async (from, to, name, verbose, key, passphrase) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(from))
                        throw new ArgumentNullException(nameof(from));

                    IonburstFS fs = new() {Verbose = verbose};

                    if (!string.IsNullOrEmpty(key))
                    {
                        IonFSCrypto crypto = new();
                        crypto.KeyFromFile(key);

                        fs.Encrypt = true;
                        fs.KeyPath = key;
                        fs.Crypto = crypto;
                    }
                    else if (!string.IsNullOrEmpty(passphrase))
                    {
                        IonFSCrypto crypto = new IonFSCrypto();
                        crypto.KeyFromPassphrase(passphrase);

                        fs.Encrypt = true;
                        fs.Crypto = crypto;
                    }

                    IonFSObject fromFso = fs.FromRemoteFile(from);
                    IonFSObject toFso = IonFSObject.FromLocalFile((to is null) ? fromFso.Name : to);

                    var results = await fs.GetAsync(fromFso, toFso);
                    if (!results.All(r => r.Value == 200))
                    {
                        Console.WriteLine($"Error receiving data from Ionburst Cloud!");
                        foreach (var r in results)
                            Console.WriteLine($" {r.Key} {r.Value}");
                    }
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSChecksumException e)
                {
                    Console.WriteLine($"Checksum Error:");
                    Console.WriteLine($" Actual: {e.hash}");
                    Console.WriteLine($" Expected: {e.metadata.Hash}");
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, fromArgument, toArgument, nameOption, verboseOption, keyOption, passphraseOption);

            return command;
        }

        private static Command Put()
        {
            var localFileArgument = new Argument<string>("localfile", "file to upload")
                {Arity = ArgumentArity.ExactlyOne};
            var folderArgument = new Argument<string>("folder", "destination folder, prefixed with ion://")
                {Arity = ArgumentArity.ExactlyOne};

            var nameOption = new Option<string>(new[] {"--name", "-n"}, "rename the uploaded file");
            var classificationOption =
                new Option<string>(new[] {"--classification", "-c"}, "Ionburst Cloud Classification");
            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"}, "Verbose output");
            var keyOption = new Option<string>(new[] {"--key", "-k"}, "Path to symmetric key");
            var passPhraseOption = new Option<string>(new[] {"--passphrase", "-pp"}, "Passphrase to generate key");
            var blockSizeOption = new Option<int>(new[] {"--blocksize", "-bs"}, "Block size in bytes");
            var manifestOption = new Option<bool>(new[] {"--manifest", "-m"}, "Store large objects using SDK Manifest");
            //var nativeOption = new Option<bool>(new[] {"--native"}, "Store large objects using native chunking");

            var tagOption = new Option<string>(new[] { "--tags" }, "Search tags in the format tag=value[:tag=value]...");
            
            Command command = new("put", "upload a file, prefix remote paths with ion://")
            {
                localFileArgument,
                folderArgument,
                nameOption,
                classificationOption,
                verboseOption,
                keyOption,
                passPhraseOption,
                blockSizeOption,
                manifestOption,
                //nativeOption,
                tagOption
            };
            //command.SetHandler(async (localfile, folder, name, classification, verbose, key, passphrase, blocksize) => 
            command.SetHandler(async (context) =>
            {
                try
                {
                    string localfile = context.ParseResult.GetValueForArgument(localFileArgument);
                    string folder = context.ParseResult.GetValueForArgument(folderArgument);
                    string name = context.ParseResult.GetValueForOption(nameOption);
                    string classification = context.ParseResult.GetValueForOption(classificationOption);
                    bool verbose = context.ParseResult.GetValueForOption(verboseOption);
                    string key = context.ParseResult.GetValueForOption(keyOption);
                    string passphrase = context.ParseResult.GetValueForOption(passPhraseOption);
                    int blocksize = context.ParseResult.GetValueForOption(blockSizeOption);
                    bool manifest = context.ParseResult.GetValueForOption(manifestOption);
                    //bool native = context.ParseResult.GetValueForOption(nativeOption);
                    string tags = context.ParseResult.GetValueForOption(tagOption); // tag=value:tag=value:tag=value

                    if (string.IsNullOrEmpty(localfile))
                        throw new ArgumentNullException(nameof(localfile));
                    if (string.IsNullOrEmpty(folder))
                        throw new ArgumentNullException(nameof(folder));

                    IonburstFS fs = new()
                    {
                        UseManifest = manifest,
                        Verbose = verbose
                    };
                    
                    // Overrides the default value set in the config
                    if (blocksize > 0)
                    {
                        fs.MaxSize = blocksize;
                    }

                    // Client-side encryption
                    if (!string.IsNullOrEmpty(key))
                    {
                        IonFSCrypto crypto = new();
                        crypto.KeyFromFile(key);

                        fs.Encrypt = true;
                        fs.KeyPath = key;
                        fs.Crypto = crypto;
                    }
                    else if (!string.IsNullOrEmpty(passphrase))
                    {
                        IonFSCrypto crypto = new();
                        crypto.KeyFromPassphrase(passphrase);

                        fs.Encrypt = true;
                        fs.Crypto = crypto;
                    }

                    IonFSObject fsoFrom = IonFSObject.FromLocalFile(localfile);
                    IonFSObject fsoTo = fs.FromRemoteFolder(folder);
                    
                    string toName = "";
                    if (!string.IsNullOrEmpty(name))
                        toName = name;
                    else
                        toName = fsoFrom.Name;
                    
                    IonFSObject fsoNewTo = fs.FromRemoteFile(fsoTo.FullFSName + toName);

                    if (fsoFrom.IsFolder)
                    {
                        Console.WriteLine("Cannot put a directory (yet)");
                        return;
                    }

                    if (!string.IsNullOrEmpty(classification))
                        fs.Classification = classification;

                    // Add any tags
                    if (!string.IsNullOrEmpty(tags))
                    {
                        foreach (var rawtag in tags.Split(':'))
                        {
                            var tag = rawtag.Split("=");
                            fsoNewTo.Tags.Add(new(tag[0], tag[1]));
                        }
                    }

                    Stopwatch sw = new();
                    if (verbose)
                        sw.Start();

                    var results = await fs.PutAsync(fsoFrom, fsoNewTo);

                    if (!results.All(r => r.Value == 200))
                    {
                        Console.WriteLine($"Error sending data to Ionburst Cloud!");
                        foreach (var r in results)
                            Console.WriteLine($" {r.Key} {r.Value}");
                    }
                    
                    if (verbose)
                    {
                        sw.Stop();
                        Console.WriteLine($"Duration: {sw.ElapsedMilliseconds} ");
                    }

                    if (verbose && results.All(r => r.Value == 200))
                    {
                        Console.WriteLine($"Bursts:");
                        foreach (var r in results)
                            Console.WriteLine($" {r.Key} {r.Value}");
                    }
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine("REMOTE EXCEPTION: {0}", e.Message);
                }
                catch (IonFSException e)
                {
                    Console.WriteLine("IONFS EXCEPTION: {0}", e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    if (e.InnerException != null)
                        Console.WriteLine(e.InnerException.Message);
                }
                //}, localFileArgument, folderArgument, nameOption, classificationOption, verboseOption, keyOption, passPhraseOption, blockSizeOption);
            });

            return command;
        }

        private static Command ManifestGet()
        {
            var fromArgument = new Argument<string>("from", "remote path to the source data")
                {Arity = ArgumentArity.ExactlyOne};
            var toArgument = new Argument<string>("to", "optional local target name")
                {Arity = ArgumentArity.ZeroOrOne};

            var nameOption = new Option<string>(new[] {"--name", "-n"});
            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});
            var keyOption = new Option<string>(new[] {"--key", "-k"}, "path to symmetric key");
            var passphraseOption = new Option<string>(new[] {"--passphrase", "-pp"}, "passphrase to generate key");

            Command command = new("mget", "download a file using a Manifest, prefix remote paths with ion://")
            {
                fromArgument,
                toArgument,
                nameOption,
                verboseOption,
                keyOption,
                passphraseOption
            };
            command.SetHandler(async (from, to, name, verbose, key, passphrase) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(from))
                        throw new ArgumentNullException(nameof(from));

                    IonburstFS fs = new() {Verbose = verbose};

                    if (!string.IsNullOrEmpty(key))
                    {
                        IonFSCrypto crypto = new();
                        crypto.KeyFromFile(key);

                        fs.Encrypt = true;
                        fs.KeyPath = key;
                        fs.Crypto = crypto;
                    }
                    else if (!string.IsNullOrEmpty(passphrase))
                    {
                        IonFSCrypto crypto = new IonFSCrypto();
                        crypto.KeyFromPassphrase(passphrase);

                        fs.Encrypt = true;
                        fs.Crypto = crypto;
                    }

                    IonFSObject fromFso = fs.FromRemoteFile(from);
                    IonFSObject toFso = IonFSObject.FromLocalFile((to is null) ? fromFso.Name : to);

                    var results = await fs.ManifestGetAsync(fromFso, toFso);
                    if (!results.All(r => r.Value == 200))
                    {
                        Console.WriteLine($"Error receiving data from Ionburst Cloud!");
                        foreach (var r in results)
                            Console.WriteLine($" {r.Key} {r.Value}");
                    }
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSChecksumException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, fromArgument, toArgument, nameOption, verboseOption, keyOption, passphraseOption);

            return command;
        }

        private static Command ManifestPut()
        {
            var localFileArgument = new Argument<string>("localfile", "file to upload")
                {Arity = ArgumentArity.ExactlyOne};
            var folderArgument = new Argument<string>("folder", "destination folder, prefixed with ion://")
                {Arity = ArgumentArity.ExactlyOne};

            var nameOption = new Option<string>(new[] {"--name", "-n"}, "rename the uploaded file");
            var classificationOption =
                new Option<string>(new[] {"--classification", "-c"}, "Ionburst Cloud Classification");
            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"}, "Verbose output");
            var keyOption = new Option<string>(new[] {"--key", "-k"}, "Path to symmetric key");
            var passPhraseOption = new Option<string>(new[] {"--passphrase", "-pp"}, "Passphrase to generate key");
            var blockSizeOption = new Option<int>(new[] {"--blocksize", "-bs"}, "Block size in bytes");

            Command command = new("mput", "upload a file using a Manifest, prefix remote paths with ion://")
            {
                localFileArgument,
                folderArgument,
                nameOption,
                classificationOption,
                verboseOption,
                keyOption,
                passPhraseOption,
                blockSizeOption
            };
            command.SetHandler(async (localfile, folder, name, classification, verbose, key, passphrase, blocksize) =>
                {
                    try
                    {
                        if (string.IsNullOrEmpty(localfile))
                            throw new ArgumentNullException(nameof(localfile));
                        if (string.IsNullOrEmpty(folder))
                            throw new ArgumentNullException(nameof(folder));

                        IonburstFS fs = new();
                        fs.Verbose = verbose;
                        //if (blocksize > 0)
                        //{
                        //    fs.MaxSize = blocksize;
                        //}

                        if (!string.IsNullOrEmpty(key))
                        {
                            IonFSCrypto crypto = new IonFSCrypto();
                            crypto.KeyFromFile(key);

                            fs.Encrypt = true;
                            fs.KeyPath = key;
                            fs.Crypto = crypto;
                        }
                        else if (!string.IsNullOrEmpty(passphrase))
                        {
                            IonFSCrypto crypto = new IonFSCrypto();
                            crypto.KeyFromPassphrase(passphrase);

                            fs.Encrypt = true;
                            fs.Crypto = crypto;
                        }

                        IonFSObject fsoFrom = IonFSObject.FromLocalFile(localfile);
                        IonFSObject fsoTo = fs.FromRemoteFolder(folder);
                        string toName = "";
                        if (!string.IsNullOrEmpty(name))
                            toName = name;
                        else
                            toName = fsoFrom.Name;
                        IonFSObject fsoNewTo = fs.FromRemoteFile(fsoTo.FullFSName + toName);

                        if (fsoFrom.IsFolder)
                        {
                            Console.WriteLine("Cannot put a directory (yet)");
                            return;
                        }

                        if (!string.IsNullOrEmpty(classification))
                            fs.Classification = classification;

                        var results = await fs.ManifestPutAsync(fsoFrom, fsoNewTo);

                        if (!results.All(r => r.Value == 200))
                        {
                            Console.WriteLine($"Error sending data to Ionburst Cloud!");
                            foreach (var r in results)
                                Console.WriteLine($" {r.Key} {r.Value}");
                        }
                    }
                    catch (RemoteFSException e)
                    {
                        Console.WriteLine("REMOTE EXCEPTION: {0}", e.Message);
                    }
                    catch (IonFSException e)
                    {
                        Console.WriteLine("IONFS EXCEPTION: {0}", e.Message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        if (e.InnerException != null)
                            Console.WriteLine(e.InnerException.Message);
                    }
                }, localFileArgument, folderArgument, nameOption, classificationOption, verboseOption, keyOption,
                passPhraseOption, blockSizeOption);

            return command;
        }

        private static Command SecretPut()
        {
            var dataArgument = new Argument<string>("data", "data to store") {Arity = ArgumentArity.ExactlyOne};
            var vaultArgument = new Argument<string>("vault", "destination vault location, prefixed with ion://")
                {Arity = ArgumentArity.ExactlyOne};
            var nameArgument = new Argument<string>("name", "name of secret") {Arity = ArgumentArity.ExactlyOne};

            var classificationOption =
                new Option<string>(new[] {"--classification", "-c"}, "Ionburst Cloud Classification");
            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});
            var keyOption = new Option<string>(new[] {"--key", "-k"}, "path to symmetric key");
            var passphraseOption = new Option<string>(new[] {"--passphrase", "-pp"}, "passphrase to generate key");

            Command command = new ("put", "store secret, prefix remote paths with ion://")
            {
                dataArgument,
                vaultArgument,
                nameArgument,
                classificationOption,
                verboseOption,
                keyOption,
                passphraseOption
            };
            command.SetHandler(async (data, vault, name, classification, verbose, key, passphrase) =>
                {
                    IonburstFS fs = new IonburstFS();
                    fs.Verbose = verbose;

                    if (!string.IsNullOrEmpty(key))
                    {
                        IonFSCrypto crypto = new IonFSCrypto();
                        crypto.KeyFromFile(key);

                        fs.Encrypt = true;
                        fs.KeyPath = key;
                        fs.Crypto = crypto;
                    }
                    else if (!string.IsNullOrEmpty(passphrase))
                    {
                        IonFSCrypto crypto = new IonFSCrypto();
                        crypto.KeyFromPassphrase(passphrase);

                        fs.Encrypt = true;
                        fs.Crypto = crypto;
                    }

                    IonFSObject fsoFrom = new IonFSObject() {Text = data, IsText = true};
                    IonFSObject fsoTo = fs.FromRemoteFolder(vault);
                    string toName = "";
                    if (!string.IsNullOrEmpty(name))
                        toName = name;
                    else
                        throw new IonFSException("name parameter is blank");

                    IonFSObject fsoNewTo = fs.FromRemoteFile(fsoTo.FullFSName + toName);

                    if (!string.IsNullOrEmpty(classification))
                        fs.Classification = classification;

                    try
                    {
                        var results = await fs.PutAsync(fsoFrom, fsoNewTo);

                        if (!results.All(r => r.Value == 200))
                        {
                            Console.WriteLine($"Error sending data to Ionburst Cloud!");
                            foreach (var r in results)
                                Console.WriteLine($" {r.Key} {r.Value}");
                        }
                    }
                    catch (IonFSException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }, dataArgument, vaultArgument, nameArgument, classificationOption, verboseOption, keyOption,
                passphraseOption);

            return command;
        }

        private static Command SecretGet()
        {
            var fromArgument = new Argument<string>("from", "remote path to the source secret")
                {Arity = ArgumentArity.ExactlyOne};

            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});
            var keyOption = new Option<string>(new[] {"--key", "-k"}, "path to symmetric key");
            var passphraseOption = new Option<string>(new[] {"--passphrase", "-pp"}, "passphrase to generate key");

            Command command = new Command("get", "retrieve secret, prefix remote paths with ion://");
            command.Add(fromArgument);
            command.Add(verboseOption);
            command.Add(keyOption);
            command.Add(passphraseOption);
            command.SetHandler(async (from, verbose, key, passphrase) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(from))
                        throw new ArgumentNullException(nameof(from));

                    IonburstFS fs = new IonburstFS {Verbose = verbose};

                    if (!string.IsNullOrEmpty(key))
                    {
                        IonFSCrypto crypto = new IonFSCrypto();
                        crypto.KeyFromFile(key);

                        fs.Encrypt = true;
                        fs.KeyPath = key;
                        fs.Crypto = crypto;
                    }
                    else if (!string.IsNullOrEmpty(passphrase))
                    {
                        IonFSCrypto crypto = new IonFSCrypto();
                        crypto.KeyFromPassphrase(passphrase);

                        fs.Encrypt = true;
                        fs.Crypto = crypto;
                    }

                    IonFSObject fromFso = fs.FromRemoteFile(from);
                    IonFSObject toFso = new IonFSObject() {IsText = true};

                    var results = await fs.GetAsync(fromFso, toFso);

                    Console.WriteLine(toFso.Text);

                    if (!results.All(r => r.Value == 200))
                    {
                        Console.WriteLine($"Error receiving data from Ionburst Cloud!");
                        foreach (var r in results)
                            Console.WriteLine($" {r.Key} {r.Value}");
                    }
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSChecksumException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, fromArgument, verboseOption, keyOption, passphraseOption);

            return command;
        }

        private static Command Del()
        {
            var pathArgument = new Argument<string>("path", "path to remove") {Arity = ArgumentArity.ExactlyOne};

            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});
            var recursiveOption = new Option<bool>(new[] {"--recursive", "-r"});

            Command command = new Command("del", "delete an object, prefix remote paths with ion://");
            command.Add(pathArgument);
            command.Add(verboseOption);
            command.Add(recursiveOption);
            command.SetHandler(async (path, verbose, recursive) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(path))
                        throw new ArgumentNullException(nameof(path));

                    IonburstFS fs = new() {Verbose = verbose};
                    IonFSObject fso = fs.FromRemoteFile(path);

                    Stopwatch sw = new();
                    if (verbose)
                        sw.Start();
                    
                    if (fso.IsFolder)
                        await fs.DeleteDirAsync(fso, recursive);
                    else
                    {
                        var results = await fs.DelAsync(fso);
                        if (!results.All(r => r.Value == 200))
                        {
                            Console.WriteLine($"Error removing data from Ionburst Cloud!");
                            foreach (var r in results)
                                Console.WriteLine($" {r.Key} {r.Value}");
                        }
                    }
                    
                    if (verbose)
                    {
                        sw.Stop();
                        Console.WriteLine($"Duration: {sw.ElapsedMilliseconds} ");
                    }
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, pathArgument, verboseOption, recursiveOption);

            return command;
        }

        private static Command ManifestDel()
        {
            var pathArgument = new Argument<string>("path", "path to remove") {Arity = ArgumentArity.ExactlyOne};

            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});
            var recursiveOption = new Option<bool>(new[] {"--recursive", "-r"});

            Command command = new Command("mdel", "delete an object using a Manifest, prefix remote paths with ion://");
            command.Add(pathArgument);
            command.Add(verboseOption);
            command.Add(recursiveOption);
            command.SetHandler(async (path, verbose, recursive) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(path))
                        throw new ArgumentNullException(nameof(path));

                    IonburstFS fs = new IonburstFS() {Verbose = verbose};
                    IonFSObject fso = fs.FromRemoteFile(path);

                    if (fso.IsFolder)
                        await fs.DeleteDirAsync(fso, recursive);
                    else
                    {
                        var results = await fs.ManifestDelAsync(fso);
                        if (!results.All(r => r.Value == 200))
                        {
                            Console.WriteLine($"Error removing data from Ionburst Cloud!");
                            foreach (var r in results)
                                Console.WriteLine($" {r.Key} {r.Value}");
                        }
                    }
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, pathArgument, verboseOption, recursiveOption);

            return command;
        }

        private static Command SecretDel()
        {
            var dataArgument = new Argument<string>("data", "secret to remove") {Arity = ArgumentArity.ExactlyOne};

            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});

            Command command = new Command("del", "delete a secret, prefix remote paths with ion://");
            command.Add(dataArgument);
            command.Add(verboseOption);
            command.SetHandler(async (data, verbose) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(data))
                        throw new ArgumentNullException(nameof(data));

                    IonburstFS fs = new IonburstFS() {Verbose = verbose};
                    IonFSObject fso = fs.FromRemoteFile(data);

                    if (fso.IsFolder)
                        await fs.DeleteDirAsync(fso, false);
                    else
                    {
                        var results = await fs.DelAsync(fso);
                        if (!results.All(r => r.Value == 200))
                        {
                            Console.WriteLine($"Error removing secret from Ionburst Cloud!");
                            foreach (var r in results)
                                Console.WriteLine($" {r.Key} {r.Value}");
                        }
                    }
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, dataArgument, verboseOption);

            return command;
        }

        private static Command Move()
        {
            var fromArgument = new Argument<string>("from") {Arity = ArgumentArity.ExactlyOne};
            var toArgument = new Argument<string>("to") {Arity = ArgumentArity.ExactlyOne};

            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});
            var keyOption = new Option<string>(new[] {"--key", "-k"}, "path to symmetric key");
            var passphraseOption = new Option<string>(new[] {"--passphrase", "-pp"}, "passphrase to generate key");

            Command command = new Command("move",
                "move a file to/from a remote file system, prefix remote paths with ion://");
            command.Add(fromArgument);
            command.Add(toArgument);
            //command.Add(new Option(new[] { "--name", "-n" }));
            command.Add(verboseOption);
            command.Add(keyOption);
            command.Add(passphraseOption);
            command.SetHandler(async (from, to, verbose, key, passphrase) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(from))
                        throw new ArgumentNullException(nameof(from));
                    if (string.IsNullOrEmpty(to))
                        throw new ArgumentNullException(nameof(to));

                    IonburstFS fs = new IonburstFS();

                    if (!string.IsNullOrEmpty(key))
                    {
                        IonFSCrypto crypto = new IonFSCrypto();
                        crypto.KeyFromFile(key);

                        fs.Encrypt = true;
                        fs.KeyPath = key;
                        fs.Crypto = crypto;
                    }
                    else if (!string.IsNullOrEmpty(passphrase))
                    {
                        IonFSCrypto crypto = new IonFSCrypto();
                        crypto.KeyFromPassphrase(passphrase);

                        fs.Encrypt = true;
                        fs.Crypto = crypto;
                    }

                    IonFSObject fromFso = fs.FromRemoteFile(from);
                    IonFSObject toFso = fs.FromRemoteFile(to);

                    await fs.MoveAsync(fromFso, toFso);
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, fromArgument, toArgument, verboseOption, keyOption, passphraseOption);

            return command;
        }

        private static Command Copy()
        {
            var fromArgument = new Argument<string>("from") {Arity = ArgumentArity.ExactlyOne};
            var toArgument = new Argument<string>("to") {Arity = ArgumentArity.ExactlyOne};

            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});
            var keyOption = new Option<string>(new[] {"--key", "-k"}, "path to symmetric key");
            var passphraseOption = new Option<string>(new[] {"--passphrase", "-pp"}, "passphrase to generate key");
            
            Command command = new("copy", "copy a file to/from a remote file system, prefix remote paths with ion://")
            {
                fromArgument,
                toArgument,
                verboseOption,
                keyOption,
                passphraseOption
            };
            command.SetHandler(async (from, to, verbose, key, passphrase) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(from))
                        throw new ArgumentNullException(nameof(from));
                    if (string.IsNullOrEmpty(to))
                        throw new ArgumentNullException(nameof(to));

                    IonburstFS fs = new() {Verbose = verbose};

                    if (!string.IsNullOrEmpty(key))
                    {
                        IonFSCrypto crypto = new();
                        crypto.KeyFromFile(key);

                        fs.Encrypt = true;
                        fs.KeyPath = key;
                        fs.Crypto = crypto;
                    }
                    else if (!string.IsNullOrEmpty(passphrase))
                    {
                        IonFSCrypto crypto = new();
                        crypto.KeyFromPassphrase(passphrase);

                        fs.Encrypt = true;
                        fs.Crypto = crypto;
                    }

                    IonFSObject fromFso = fs.FromString(from);
                    IonFSObject toFso = fs.FromString(to);

                    await fs.CopyAsync(fromFso, toFso);
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, fromArgument, toArgument, verboseOption, keyOption, passphraseOption);

            return command;
        }

        private static Command MakeDir()
        {
            var folderArgument = new Argument<string>("folder") {Arity = ArgumentArity.ExactlyOne};
            Command command = new Command("mkdir", "create a folder, prefix remote paths with ion://");
            command.Add(folderArgument);
            command.SetHandler(async (folder) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(folder))
                        throw new ArgumentNullException(nameof(folder));

                    IonburstFS fs = new();
                    IonFSObject fso = fs.FromRemoteFolder(folder);

                    await fs.MakeDirAsync(fso);
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, folderArgument);

            return command;
        }

        private static Command RemoveDir()
        {
            var folderArgument = new Argument<string>("folder") {Arity = ArgumentArity.ExactlyOne};

            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});
            var recursiveOption = new Option<bool>(new[] {"--recursive", "-r"});

            Command command = new("rmdir", "remove a folder, prefix remote paths with ion://");
            command.Add(folderArgument);
            command.Add(verboseOption);
            command.Add(recursiveOption);
            command.SetHandler(async (folder, verbose, recursive) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(folder))
                        throw new ArgumentNullException(nameof(folder));

                    IonburstFS fs = new IonburstFS() {Verbose = verbose};
                    IonFSObject fso = fs.FromRemoteFolder(folder);

                    await fs.DeleteDirAsync(fso, recursive);
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine($"*** ERROR: {e.Message}");
                }
                catch (IonFSException e)
                {
                    Console.WriteLine($"*** ERROR: {e.Message}");
                }
            }, folderArgument, verboseOption, recursiveOption);

            return command;
        }

        private static Command DeleteById()
        {
            var repoArgument = new Argument<string>("repo") { Arity = ArgumentArity.ExactlyOne };
            var guidArgument = new Argument<string>("guid") { Arity = ArgumentArity.ExactlyOne };

            Command command = new("rm-id") {IsHidden = true};
            command.Add(guidArgument);
            command.Add(repoArgument);
            command.SetHandler(async (repo, guid) =>
            {
                if (guid == null)
                    throw new ArgumentNullException(nameof(guid));

                IonburstFS fs = new IonburstFS();
                await fs.RemoveById(repo, Guid.Parse(guid));
            }, repoArgument, guidArgument);

            return command;
        }

        private static Command GetChunkById()
        {
            var repoArgument = new Argument<string>("repo") { Arity = ArgumentArity.ExactlyOne };
            var guidArgument = new Argument<string>("guid") { Arity = ArgumentArity.ExactlyOne };

            var verboseOption = new Option<bool>(new[] {"--verbose", "-v"});

            Command command = new("get-id") {IsHidden = true};
            command.Add(guidArgument);
            command.Add(repoArgument);
            command.Add(verboseOption);
            command.SetHandler(async (repo, guid, verbose) =>
            {
                if (guid == null)
                    throw new ArgumentNullException(nameof(guid));

                IonburstFS fs = new() {Verbose = verbose};
                var results = await fs.GetChunk(repo, guid);

                if (!results.All(r => r.Value == 200))
                {
                    Console.WriteLine($"Error receiving data from Ionburst Cloud!");
                    foreach (KeyValuePair<string, int> r in results)
                        Console.WriteLine($" {r.Key} {r.Value}");
                }
            }, repoArgument, guidArgument, verboseOption);

            return command;
        }

        private static Command DeleteMetadata()
        {
            var fileArgument = new Argument<string>("file") {Arity = ArgumentArity.ExactlyOne};

            Command command = new("rm-meta") {IsHidden = true};
            command.Add(fileArgument);
            command.SetHandler(async (file) =>
            {
                if (file == null)
                    throw new ArgumentNullException(nameof(file));

                IonburstFS fs = new IonburstFS();
                IonFSObject fso = fs.FromRemoteFile(file);

                await fs.RemoveMetadata(fso);
            }, fileArgument);

            return command;
        }

        private static Command GetMetadata()
        {
            var fileArgument = new Argument<string>("file", "prefix remote paths with ion://")
                {Arity = ArgumentArity.ZeroOrOne};

            var quietOption = new Option<bool>(new[] {"--quiet", "-q"});

            Command command = new("meta") {IsHidden = true};
            command.Add(fileArgument);
            command.Add(quietOption);
            command.SetHandler(async (file, quiet) =>
            {
                if (string.IsNullOrEmpty(file))
                    throw new ArgumentNullException(nameof(file));

                try
                {
                    if (!quiet)
                        Console.WriteLine(Logo());

                    IonburstFS fs = new IonburstFS();
                    IonFSObject fso = fs.FromRemoteFile(file);
                    IonFSMetadata metadata = await fs.GetMetadata(fso);

                    Console.WriteLine($"Metadata for {fso}\n");
                    Console.WriteLine(metadata.ToString());
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, fileArgument, quietOption);

            return command;
        }

        private static Command AddMetadata()
        {
            var metadataArgument = new Argument<string>("metadata", "metadata object to upload")
                {Arity = ArgumentArity.ExactlyOne};
            var folderArgument = new Argument<string>("folder", "destination folder, prefixed with ion://")
                {Arity = ArgumentArity.ExactlyOne};

            var verboseOption = new Option<bool>(new[] {"--verbose"});

            Command command = new("add-meta", "") {IsHidden = true};
            command.Add(metadataArgument);
            command.Add(folderArgument);
            command.Add(verboseOption);
            command.SetHandler(async (metadata, folder, verbose) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(metadata))
                        throw new ArgumentNullException(nameof(metadata));
                    if (string.IsNullOrEmpty(folder))
                        throw new ArgumentNullException(nameof(folder));

                    IonburstFS fs = new IonburstFS();
                    fs.Verbose = verbose;

                    IonFSObject fsoMetadata = IonFSObject.FromLocalFile(metadata);
                    IonFSObject fsoTo = fs.FromRemoteFolder(folder);

                    await fs.AddMetadata(fsoMetadata, fsoTo);
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, metadataArgument, folderArgument, verboseOption);

            return command;
        }

        private static Command GetClassifications()
        {
            var repoArgument = new Argument<string>("repo") { Arity = ArgumentArity.ExactlyOne };

            Command command = new("policy", "list the current Ionburst Cloud Classification Policies") {IsHidden = false};
            command.Add(repoArgument);
            command.SetHandler(async (repo) =>
            {
                try
                {
                    IonburstFS fs = new();

                    Console.WriteLine(Logo());

                    var classifications = await fs.GetClassifications(repo);

                    Console.WriteLine("Available Classifications:\n");
                    foreach (var c in classifications.OrderBy(x => x.Key))
                    {
                        Console.WriteLine($" {c.Key}:{c.Value}");
                    }
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, repoArgument);

            return command;
        }

        private static Command GetRepos()
        {
            Command command = new("repos", "list the currently registered Repositories") {IsHidden = false};
            command.SetHandler(() =>
            {
                try
                {
                    IonburstFS fs = new();

                    Console.WriteLine(Logo());
                    Console.WriteLine($"Available Repositories (*default):\n");

                    List<IonFSRepository> repos = fs.Repositories;
                    foreach (var r in repos)
                    {
                        Console.WriteLine(
                            $" {(r.IsDefault ? "*" : " ")} [{r.Usage.Substring(0, 1).ToLower()}] {"ion://" + r.Repository + "/",-32} ({r.Metadata.GetType()})");
                    }
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            });

            return command;
        }

        private static Command KeyGen()

        {
            var passphraseArgument = new Argument<string>("passphrase") {Arity = ArgumentArity.ExactlyOne};

            Command command = new Command("keygen") {IsHidden = true};
            command.Add(passphraseArgument);
            command.SetHandler((passphrase) =>
            {
                try
                {
                    IonFSCrypto crypto = new IonFSCrypto();
                    byte[] key_256 = crypto.KeyGen(passphrase);
                    Console.WriteLine("256:{0}", BitConverter.ToString(key_256).Replace("-", ""));
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            }, passphraseArgument);

            return command;
        }

        private static Command Search()
        {
            //var fileArgument = new Argument<string>("file", "prefix remote paths with ion://") { Arity = ArgumentArity.ZeroOrOne };
            var repoArgument = new Argument<string>("repo", "prefix with ion://") { Arity = ArgumentArity.ExactlyOne };

            var quietOption = new Option<bool>(new[] { "--quiet", "-q" });
            var tagOption = new Option<string>(new[] { "--tag" });
            var valueOption = new Option<string>(new[] { "--value" }) { };
            var recursiveOption = new Option<bool>(new[] { "--recursive", "-r" });

            Command command = new("search")
            {
                repoArgument,
                quietOption,
                tagOption,
                valueOption,
                recursiveOption
            };
            command.SetHandler(async (repo,quiet, tag, value, recursive) =>
            {
                try
                {
                    if (!quiet)
                        Console.WriteLine(Logo());
                    
                    value ??= ".*";
                    tag ??= ".*";

                    IonburstFS fs = new();

                    // Get Search
                    IonFSObject o = fs.FromRemoteFolder(repo); // Path
                    IIonFSMetadata m = fs.GetMetadataHandler(o);
                    List<IonFSSearchResult> results = await m.Search(o, tag, value, recursive);

                    Console.WriteLine($"Search Results ({o.FullFSName}):\n");
                    if (results.Count > 0)
                    {
                        foreach (var r in results)
                            Console.WriteLine($" {r.Name}: {r.Tag}={r.Value}");
                    }
                    else
                        Console.WriteLine(" No matches found");
                    
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (RegexParseException e)
                {
                    Console.WriteLine(e.Message);
                }

            }, repoArgument, quietOption, tagOption, valueOption, recursiveOption);

            return command;
        }

        private static String Logo()
        {
            //string ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string ver = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

            return
                @"     ____            ___________" + "\n" +
                @"    /  _/___  ____  / ____/ ___/" + "\n" +
                @"    / // __ \/ __ \/ /_   \__ \" + "\n" +
                @"  _/ // /_/ / / / / __/  ___/ /" + "\n" +
                @" /___/\____/_/ /_/_/    /____/     v" + ver + "\n\n";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "Main Program")]
        public static void Main(string[] args)
        {
            try
            {
                var infoOption = new Option<bool>(new[] {"--info", "-i"});
                var command = new RootCommand("Securing your data on Ionburst Cloud.");
                command.Add(infoOption);
                command.SetHandler((info) =>
                {
                    Console.WriteLine(Logo());
                    Console.WriteLine($"We may guard your data, but we'll never take its freedom!");
                    Console.WriteLine("\nUsage: ionfs --help");
                    Console.WriteLine();

                    if (info)
                    {
                        try
                        {
                            IonburstFS ionburst = new();

                            Console.WriteLine("Ionburst Cloud {1} is {0}\n",
                                (ionburst.IonburstStatus) ? "Online" : "Offline", ionburst.IonburstUri);
                            foreach (var v in ionburst.IonburstVersion)
                            {
                                Console.WriteLine($"{v}");
                            }

                            Console.WriteLine($"Max Upload:  {ionburst.MaxUploadSize} bytes");
                            Console.WriteLine($"Max Size:    {ionburst.MaxSize} bytes");
                        }
                        catch (IonFSException e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }, infoOption);

                #region Register Commands

                command.AddCommand(List());
                command.AddCommand(Get());
                command.AddCommand(Put());
                //command.AddCommand(ManifestGet());
                //command.AddCommand(ManifestPut());
                command.AddCommand(Del());
                //command.AddCommand(ManifestDel());
                command.AddCommand(Move());
                command.AddCommand(Copy());
                command.AddCommand(MakeDir());
                command.AddCommand(RemoveDir());
                command.AddCommand(DeleteById());
                command.AddCommand(DeleteMetadata());
                command.AddCommand(GetChunkById());
                command.AddCommand(GetMetadata());
                command.AddCommand(AddMetadata());
                command.AddCommand(GetClassifications());
                command.AddCommand(GetRepos());
                command.AddCommand(KeyGen());
                command.AddCommand(Search());
                #endregion

                Command secrets = Secrets();
                {
                    secrets.AddCommand(SecretPut());
                    secrets.AddCommand(SecretGet());
                    secrets.AddCommand(SecretDel());
                }
                command.AddCommand(secrets);

                command.InvokeAsync(args).Wait();

                Console.WriteLine();
            }
            catch (RemoteFSException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (IonFSException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
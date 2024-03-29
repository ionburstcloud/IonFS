﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;

using Ionburst.Apps.IonFS.Model;
using Ionburst.Apps.IonFS.Exceptions;
using Figgle;

namespace Ionburst.Apps.IonFS
{
    class Program
    {
        private static Command List()
        {
            Command command = new Command("list", "show the contents of a remote path, prefix remote paths with ion://");
            command.AddArgument(new Argument<string>("folder", "path in the remote file system") { Arity = ArgumentArity.ZeroOrOne });
            command.AddOption(new Option(new[] { "--recursive", "-r" }));
            command.Handler = CommandHandler.Create<string, bool>(async (folder, recursive) =>
            {
                try
                {
                    IonburstFS fs = new IonburstFS();
                    IonFSObject fso = fs.FromRemoteFolder(folder);

                    Console.WriteLine(FiggleFonts.Slant.Render("IonFS"));

                    List<IonFSObject> items = await fs.ListAsync(fso, recursive);

                    Console.WriteLine($"Directory of {fso}\n");
                    if (items.Count == 0) 
                    { 
                        Console.WriteLine(" Remote directory is empty");
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
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
            });

            return command;
        }

        private static Command Get()
        {
            Command command = new Command("get", "download a file, prefix remote paths with ion://");
            command.AddArgument(new Argument<string>("from") { Arity = ArgumentArity.ExactlyOne });
            command.AddOption(new Option(new[] { "--name", "-n" }) { Argument = new Argument<string>("name", "new filename") { Arity = ArgumentArity.ExactlyOne } });
            command.AddOption(new Option(new[] { "--verbose", "-v" }));
            command.AddOption(new Option(new[] { "--key", "-k" }, "path to symmetric key") { Argument = new Argument<string>("key", "path to private key") { Arity = ArgumentArity.ExactlyOne } });
            command.AddOption(new Option(new[] { "--passphrase", "-pp" }, "passphrase to generate key") { Argument = new Argument<string>("key", "path to private key") { Arity = ArgumentArity.ExactlyOne } });
            command.Handler = CommandHandler.Create<string, string, bool, string, string>(async (from, name, verbose, key, passphrase) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(from))
                        throw new ArgumentNullException(nameof(from));

                    IonburstFS fs = new IonburstFS { Verbose = verbose };
                    
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
                    IonFSObject toFso = IonFSObject.FromLocalFile((name is null) ? fromFso.Name : name);

                    var results = await fs.GetAsync(fromFso, toFso);
                    if (!results.All(r => r.Value == 200))
                    {
                        Console.WriteLine($"Error receiving data Ionburst!");
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
                return 0;
            });

            return command;
        }

        private static Command Put()
        {
            Command command = new Command("put", "upload a file, prefix remote paths with ion://");
            command.AddArgument(new Argument<string>("localfile", "file to upload") { Arity = ArgumentArity.ExactlyOne });
            command.AddArgument(new Argument<string>("folder", "destination folder, prefixed with ion://") { Arity = ArgumentArity.ExactlyOne });
            command.AddOption(new Option(new[] { "--name", "-n" }) { Argument = new Argument<string>("name", "rename the uploaded file") { Arity = ArgumentArity.ExactlyOne } });
            command.AddOption(new Option(new[] { "--classification", "-c" }) { Argument = new Argument<string>("classification", "Ionburst Classification") { Arity = ArgumentArity.ExactlyOne } });
            command.AddOption(new Option(new[] { "--verbose", "-v" }));
            command.AddOption(new Option(new[] { "--key", "-k" }, "path to symmetric key") { Argument = new Argument<string>("key", "path to private key") { Arity = ArgumentArity.ExactlyOne } });
            command.AddOption(new Option(new[] { "--passphrase", "-pp" }, "passphrase to generate key") { Argument = new Argument<string>("key", "path to private key") { Arity = ArgumentArity.ExactlyOne } });
            command.Handler = CommandHandler.Create<string, string, string, string, bool, string, string>(async (localfile, folder, name, classification, verbose, key, passphrase) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(localfile))
                        throw new ArgumentNullException(nameof(localfile));
                    if (string.IsNullOrEmpty(folder))
                        throw new ArgumentNullException(nameof(folder));


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
                        return 1;
                    }

                    if (!string.IsNullOrEmpty(classification))
                        fs.Classification = classification;

                    var results = await fs.PutAsync(fsoFrom, fsoNewTo);
                    
                    if (!results.All(r => r.Value == 200))
                    {
                        Console.WriteLine($"Error sending data to Ionburst!");
                        foreach (var r in results)
                            Console.WriteLine($" {r.Key} {r.Value}");
                    }
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                }
                return 0;
            });

            return command;
        }

        private static Command Del()
        {
            Command command = new Command("del", "delete an object, prefix remote paths with ion://");
            command.AddArgument(new Argument<string>("path", "path to remove") { Arity = ArgumentArity.ExactlyOne });
            command.AddOption(new Option(new[] { "--verbose", "-v" }));
            command.AddOption(new Option(new[] { "--recursive", "-r" }));
            command.Handler = CommandHandler.Create<string, bool, bool>(async (path, verbose, recursive) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(path))
                        throw new ArgumentNullException(nameof(path));

                    IonburstFS fs = new IonburstFS() { Verbose = verbose };
                    IonFSObject fso = fs.FromRemoteFile(path);

                    if (fso.IsFolder)
                        await fs.DeleteDirAsync(fso, recursive);
                    else
                    {
                        var results = await fs.DelAsync(fso);
                        if (!results.All(r => r.Value == 200))
                        {
                            Console.WriteLine($"Error removing data Ionburst!");
                            foreach (var r in results)
                                Console.WriteLine($" {r.Key} {r.Value}");
                        }
                    }

                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                    return 1;
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                    return 1;
                }
                return 0;
            });

            return command;
        }

        private static Command Move()
        {
            Command command = new Command("move", "move a file to/from a remote file system, prefix remote paths with ion://");
            command.AddArgument(new Argument<string>("from") { Arity = ArgumentArity.ExactlyOne });
            command.AddArgument(new Argument<string>("to") { Arity = ArgumentArity.ExactlyOne });
            command.AddOption(new Option(new[] { "--name", "-n" }) { Argument = new Argument<string>("name", "new filename") { Arity = ArgumentArity.ExactlyOne } });
            command.AddOption(new Option(new[] { "--verbose", "-v" }));
            command.AddOption(new Option(new[] { "--key", "-k" }, "path to symmetric key") { Argument = new Argument<string>("key", "path to private key") { Arity = ArgumentArity.ExactlyOne } });
            command.AddOption(new Option(new[] { "--passphrase", "-pp" }, "passphrase to generate key") { Argument = new Argument<string>("key", "path to private key") { Arity = ArgumentArity.ExactlyOne } });
            command.Handler = CommandHandler.Create<string, string, string, bool, string, string>(async (from, to, name, verbose, key, passphrase) =>
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
                return 0;
            });

            return command;
        }

        private static Command Copy()
        {
            Command command = new Command("copy", "copy a file to/from a remote file system, prefix remote paths with ion://");
            command.AddArgument(new Argument<string>("from") { Arity = ArgumentArity.ExactlyOne });
            command.AddArgument(new Argument<string>("to") { Arity = ArgumentArity.ExactlyOne });
            command.AddOption(new Option(new[] { "--name", "-n" }) { Argument = new Argument<string>("name", "new filename") { Arity = ArgumentArity.ExactlyOne } });
            command.AddOption(new Option(new[] { "--verbose", "-v" }));
            command.AddOption(new Option(new[] { "--key", "-k" }, "path to symmetric key") { Argument = new Argument<string>("key", "path to private key") { Arity = ArgumentArity.ExactlyOne } });
            command.AddOption(new Option(new[] { "--passphrase", "-pp" }, "passphrase to generate key") { Argument = new Argument<string>("key", "path to private key") { Arity = ArgumentArity.ExactlyOne } });
            command.Handler = CommandHandler.Create<string, string, string, bool, string, string>(async (from, to, name, verbose, key, passphrase) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(from))
                        throw new ArgumentNullException(nameof(from));
                    if (string.IsNullOrEmpty(to))
                        throw new ArgumentNullException(nameof(to));

                    IonburstFS fs = new IonburstFS { Verbose = verbose };

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
                return 0;
            });

            return command;
        }

        private static Command MakeDir()
        {
            Command command = new Command("mkdir", "create a folder, prefix remote paths with ion://");
            command.AddArgument(new Argument<string>("folder") { Arity = ArgumentArity.ExactlyOne });
            command.Handler = CommandHandler.Create<string>(async (folder) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(folder))
                        throw new ArgumentNullException(nameof(folder));

                    IonburstFS fs = new IonburstFS();
                    IonFSObject fso = fs.FromRemoteFolder(folder);

                    await fs.MakeDirAsync(fso);
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                    return 1;
                }
                return 0;
            });

            return command;
        }

        private static Command RemoveDir()
        {
            Command command = new Command("rmdir", "remove a folder, prefix remote paths with ion://");
            command.AddArgument(new Argument<string>("folder") { Arity = ArgumentArity.ExactlyOne });
            command.Handler = CommandHandler.Create<string>(async (folder) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(folder))
                        throw new ArgumentNullException(nameof(folder));

                    IonburstFS fs = new IonburstFS();
                    IonFSObject fso = fs.FromRemoteFolder(folder);

                    await fs.DeleteDirAsync(fso);
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine($"*** ERROR: {e.Message}");
                }
                catch (IonFSException e)
                {
                    Console.WriteLine($"*** ERROR: {e.Message}");
                }
                return 0;
            });

            return command;
        }

        private static Command DeleteById()
        {
            Command command = new Command("rm-id") { IsHidden = true };
            command.AddArgument(new Argument<string>("guid") { Arity = ArgumentArity.ZeroOrOne });
            command.Handler = CommandHandler.Create<Guid>(async (guid) =>
            {
                if (guid == null)
                throw new ArgumentNullException(nameof(guid));

                IonburstFS fs = new IonburstFS();
                await fs.RemoveById(guid);
            });

            return command;
        }

        private static Command GetChunkById()
        {
            Command command = new Command("get-id") { IsHidden = true };
            command.AddArgument(new Argument<string>("guid") { Arity = ArgumentArity.ZeroOrOne });
            command.AddOption(new Option(new[] { "--verbose", "-v" }));
            command.Handler = CommandHandler.Create<Guid, bool>(async (guid, verbose) =>
            {
                if (guid == null)
                    throw new ArgumentNullException(nameof(guid));

                IonburstFS fs = new IonburstFS { Verbose = verbose };
                var results = await fs.GetChunk(guid);

                if (!results.All(r => r.Value == 200))
                {
                    Console.WriteLine($"Error receiving data Ionburst!");
                    foreach (var r in results)
                        Console.WriteLine($" {r.Key} {r.Value}");
                }
            });

            return command;
        }

        private static Command DeleteMetadata()
        {
            Command command = new Command("rm-meta") { IsHidden = true };
            command.AddArgument(new Argument<string>("file") { Arity = ArgumentArity.ZeroOrOne });
            command.Handler = CommandHandler.Create<string>(async (file) =>
            {
                if (file == null)
                    throw new ArgumentNullException(nameof(file));

                IonburstFS fs = new IonburstFS();
                IonFSObject fso = fs.FromRemoteFile(file);

                await fs.RemoveMetadata(fso);
            });

            return command;
        }

        private static Command GetMetadata()
        {
            Command command = new Command("meta") { IsHidden = true };
            command.AddArgument(new Argument<string>("file", "prefix remote paths with ion://") { Arity = ArgumentArity.ZeroOrOne });
            command.Handler = CommandHandler.Create<string>(async (file) =>
            {
                if (string.IsNullOrEmpty(file))
                    throw new ArgumentNullException(nameof(file));

                try
                {
                    Console.WriteLine(FiggleFonts.Slant.Render("IonFS"));

                    IonburstFS fs = new IonburstFS();
                    IonFSObject fso = fs.FromRemoteFile(file);
                    IonFSMetadata metadata = await fs.GetMetadata(fso);

                    Console.WriteLine($"Metadata for {fso}\n");
                    Console.WriteLine(metadata.ToString());
                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                    return 1;
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                    return 1;
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine(e.Message);
                    return 1;
                }
                return 0;
            });

            return command;
        }

        private static Command AddMetadata()
        {
            Command command = new Command("add-meta", "") { IsHidden = true };
            command.AddArgument(new Argument<string>("metadata", "metadata file to upload") { Arity = ArgumentArity.ExactlyOne });
            command.AddArgument(new Argument<string>("folder", "destination folder, prefixed with ion://") { Arity = ArgumentArity.ExactlyOne });
            command.AddOption(new Option(new[] { "--verbose", "-v" }));
            command.Handler = CommandHandler.Create<string, string, bool>(async (metadata, folder, verbose) =>
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
                return 0;
            });

            return command;
        }

        private static Command GetClassifications()
        {
            Command command = new Command("policy", "list the current Ionburst Classification Policies") { IsHidden = false };
            command.Handler = CommandHandler.Create(async () =>
            {
                try
                {
                    IonburstFS fs = new IonburstFS();

                    Console.WriteLine(FiggleFonts.Slant.Render("IonFS"));

                    var classifications = await fs.GetClassifications();


                    Console.WriteLine("Available Classifications:\n");
                    foreach (var c in classifications.OrderBy(x => x.Key))
                    {
                        Console.WriteLine($" {c.Key}:{c.Value}");
                    }

                }
                catch (RemoteFSException e)
                {
                    Console.WriteLine(e.Message);
                    return 1;
                }
                return 0;
            });

            return command;
        }

        private static Command GetRepos()
        {
            Command command = new Command("repos", "list the currently registered Repositories") { IsHidden = false };
            command.Handler = CommandHandler.Create(() =>
            {
                try
                {
                    IonburstFS fs = new IonburstFS();

                    Console.WriteLine(FiggleFonts.Slant.Render("IonFS"));
                    Console.WriteLine($"Available Repositories (*default):\n");

                    List<IonFSRepository> repos = fs.Repositories;
                    foreach (var r in repos)
                    {
                        Console.WriteLine($" {(r.IsDefault?"*":" ")} {"ion://"+r.Repository+"/",-32} ({r.Metadata.GetType()})");
                    }
                }
                catch (IonFSException e)
                {
                    Console.WriteLine(e.Message);
                    return 1;
                }
                return 0;
            });

            return command;
        }

        private static Command KeyGen()
        {
            Command command = new Command("keygen") { IsHidden = true };
            command.AddArgument(new Argument<string>("passphrase") { Arity = ArgumentArity.ExactlyOne });
            command.Handler = CommandHandler.Create<string>((passphrase) =>
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
                    return 1;
                }
                return 0;
            });

            return command;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Main Program")]
        public static void Main(string[] args)
        {
            try
            {
                var command = new RootCommand("Securing your data on Ionburst.");
                command.AddOption(new Option(new[] { "--version", "-v" }));
                command.Handler = CommandHandler.Create<IConsole, bool>((console, version) =>
                {
                    string shield = "\U0001f6e1";
                    string scotland = "\U0001F3F4\U000E0067\U000E0062\U000E0073\U000E0063\U000E0074\U000E007F";

                    Console.WriteLine(FiggleFonts.Slant.Render("IonFS"));
                    Console.WriteLine($"We may guard {shield} your data, but we'll never take its freedom {scotland}!");
                    Console.WriteLine("\nUsage: IonFS --help");

                    Console.WriteLine();

                    if (version)
                    {
                        try
                        {
                            IonburstFS ionburst = new IonburstFS();

                            Console.WriteLine(" Ionburst {1} is {0}\n", (ionburst.IonburstStatus) ? "Online" : "Offline", ionburst.IonburstUri);
                            foreach (var v in ionburst.IonburstVersion)
                            {
                                Console.WriteLine($" {v}");
                            }
                            Console.WriteLine($"  Max Upload: {ionburst.MaxUploadSize} bytes");
                            Console.WriteLine($"    Max Size: {ionburst.MaxSize} bytes");
                        }
                        catch (IonFSException e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                });

                #region Register Commands
                command.AddCommand(List());
                command.AddCommand(Get());
                command.AddCommand(Put());
                command.AddCommand(Del());
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
                #endregion

                command.InvokeAsync(args).Wait();
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

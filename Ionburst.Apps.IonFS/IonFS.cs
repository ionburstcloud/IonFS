using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static System.Environment;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Ionburst.SDK;
using ion = Ionburst.SDK.Model;
using Ionburst.Apps.IonFS.Model;
using Ionburst.Apps.IonFS.Exceptions;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace Ionburst.Apps.IonFS
{
    public class IonburstFS : IDisposable
    {
        private IIonburstClient ionburstPrivate;
        private readonly IIonFSMetadata md;
        private readonly IonFSRepository defaultRepo;
        private readonly IConfiguration config;
        private readonly IConfiguration ionFSConfig;

        const long HardMaxSize = 65000000;

        public List<IonFSRepository> Repositories { get; }

        public bool Verbose { get; set; } = false;
        private long _maxSize;

        public long MaxSize
        {
            get { return _maxSize; }
            set { _maxSize = (value > HardMaxSize) ? HardMaxSize : value; }
        }

        public string Classification { get; set; }
        public bool Encrypt { get; set; }
        public string KeyPath { get; set; }

        public IonFSCrypto Crypto { get; set; }

        public Boolean UseManifest { get; set; }

        private struct Burst
        {
            public long start;
            public long end;
            public long size;
            public Guid id;
            public byte[] data;
        }

        public IonburstFS()
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            ionFSConfig = new ConfigurationBuilder()
                .AddJsonFile(
                    $"{GetFolderPath(SpecialFolder.UserProfile, SpecialFolderOption.None)}/.ionfs/appsettings.json",
                    optional: true, reloadOnChange: true)
                .Build();

            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddConfiguration(ionFSConfig)
                .AddEnvironmentVariables()
                .Build();

            _ = bool.TryParse(config["IonFS:Verbose"], out bool verboseConfig);
            Verbose = verboseConfig;

            _ = bool.TryParse(config["IonFS:UseManifest"], out bool useManifestConfig);
            UseManifest = useManifestConfig;

            MaxSize = long.Parse(config["IonFS:MaxSize"], NumberStyles.Integer);
            Classification = config["IonFS:DefaultClassification"];
            Encrypt = false;

            List<IonFSRepositoryConfiguration> configuredRepositories = new List<IonFSRepositoryConfiguration>();
            config.GetSection("IonFS:Repositories").Bind(configuredRepositories);

            // Build the repo collection from config
            Repositories = new List<IonFSRepository>();
            foreach (IonFSRepositoryConfiguration configuredRepository in configuredRepositories)
            {
                IonFSRepository newRepository = new()
                {
                    Repository = configuredRepository.Name,
                    Usage = configuredRepository.Usage,
                    DataStore = configuredRepository.DataStore
                };

                //Type t = Type.GetType(configuredRepository.Class, Assembly.LoadFrom(configuredRepository.Assembly), );

                Assembly assem = Assembly.LoadFrom(configuredRepository.Assembly);

                Type t = Type.GetType(configuredRepository.Class,
                    (name) => Assembly.LoadFrom(configuredRepository.Assembly),
                    (a, b, c) => assem.GetType(configuredRepository.Class, true, false)
                );

                newRepository.Metadata = (IIonFSMetadata) Activator.CreateInstance(t, configuredRepository.DataStore,
                    configuredRepository.Name, configuredRepository.Usage);
                Repositories.Add(newRepository);
            }

            if (Repositories.Count > 0)
            {
                IonFSRepository findDefault = Repositories.Find(r => r.Repository == config["IonFS:DefaultRepository"]);
                if (findDefault != null)
                {
                    findDefault.IsDefault = true;
                }
                else
                {
                    Repositories[0].IsDefault = true;
                }
            }

            // Default
            defaultRepo = Repositories.Find(r => r.IsDefault);
            md = defaultRepo.Metadata;
        }

        public long MaxUploadSize
        {
            get => GetIonburst().GetUploadSizeLimit().Result;
        }

        public List<string> IonburstVersion
        {
            get => GetIonburst().GetVersionDetails().Result;
        }

        public bool IonburstStatus
        {
            get => GetIonburst().CheckIonburstAPI().Result;
        }

        public string IonburstUri
        {
            get => GetIonburst().GetConfiguredUri().Result;
        }

        public string GetCurrentDataStore()
        {
            return md.GetDataStore();
        }

        public string GetCurrentRepositoryName()
        {
            return md.RepositoryName;
        }

        protected IIonburstClient GetIonburst()
        {
            if (ionburstPrivate is null)
                ionburstPrivate = IonburstClientFactory.CreateIonburstClient(ionFSConfig);

            if (!ionburstPrivate.CheckIonburstAPI().Result)
                throw new IonFSException($"** WARNING **: Ionburst Cloud is currently unavailable!");

            return ionburstPrivate;
        }

        public IIonFSMetadata GetMetadataHandler(IonFSObject fso)
        {
            IonFSRepository repo = Repositories.Find(r => r.Repository == fso.Repository);

            // Set Handler for Default repository
            IIonFSMetadata m = md;
            if (repo != null)
            {
                fso.IsSecret = repo.Usage == "Secrets";
                m = repo.Metadata;
            }
            else
            {
                fso.IsSecret = defaultRepo.Usage == "Secrets";
                fso.Repository = m.RepositoryName;
                fso.HasRepository = true;
            }


            return m;
        }


        // Core Methods

        public async Task DeleteDirAsync(IonFSObject folder, bool recursive = false)
        {
            IIonFSMetadata mh = GetMetadataHandler(folder);

            if (!mh.Exists(folder).Result)
                throw new IonFSException($"Folder {folder} doesn't exist.");

            List<IonFSObject> items = await mh.List(folder, recursive);

            if (items.Count == 0 || (items.All(i => i.Path == folder.Path && items.Count == 1)))
            {
                await mh.DelMetadata(folder);
            }
            else if (items.Count != 0 && recursive)
            {
                Parallel.ForEach(items.FindAll(x => !x.IsFolder), (item) => { DelAsync(item).Wait(); });
                Parallel.ForEach(items.FindAll(x => x.IsFolder), (item) => { DeleteDirAsync(item, recursive).Wait(); });
            }
            else
                throw new IonFSException($"Folder '{folder}' must be empty, or use the --recursive option");
        }

        public async Task<HashSet<KeyValuePair<Guid, int>>> DelAsync(IonFSObject file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            HashSet<KeyValuePair<Guid, int>> ids = new();

            try
            {
                IIonFSMetadata mh = GetMetadataHandler(file);
                IonFSMetadata metadata = await mh.GetMetadata(file);

                if (metadata.IsManifest && !(mh.Usage == "Secrets"))
                {
                    ion.DeleteManifestRequest delManifestRequest = new()
                    {
                        Particle = metadata.Id.First().ToString(),
                        TimeoutSpecified = true,
                        RequestTimeout = new TimeSpan(0, 2, 0)
                    };

                    ion.DeleteManifestResult delManifestResult;
                    delManifestResult = await GetIonburst().DeleteAsync(delManifestRequest) as ion.DeleteManifestResult;

                    ids.Add(new KeyValuePair<Guid, int>(metadata.Id.First(), delManifestResult.StatusCode));
                    if (Verbose)
                        Console.WriteLine(
                            $"{metadata.Id.First()} {delManifestResult.StatusCode} {delManifestResult.StatusMessage}");
                }
                else
                {
                    _ = Parallel.ForEach(metadata.Id, async (id) =>
                        {
                            // Ionburst
                            ion.DeleteObjectRequest delRequest = new ion.DeleteObjectRequest
                            {
                                Particle = id.ToString(),
                                TimeoutSpecified = true,
                                RequestTimeout = new TimeSpan(0, 2, 0)
                            };

                            ion.DeleteObjectResult delResult;
                            if (mh.Usage == "Secrets")
                            {
                                delResult = await GetIonburst().SecretsDeleteAsync(delRequest);
                            }
                            else
                            {
                                delResult = await GetIonburst().DeleteAsync(delRequest);
                            }

                            ids.Add(new KeyValuePair<Guid, int>(id, delResult.StatusCode));
                            if (Verbose)
                                Console.WriteLine($"{id} {delResult.StatusCode} {delResult.StatusMessage}");
                        }
                    );
                }

                if (ids.All(id => id.Value == 200))
                {
                    await mh.DelMetadata(file);
                }
            }
            catch (IonFSObjectDoesNotExist e)
            {
                throw new IonFSException($"The FSObject {e.Message} does not exist!", e);
            }

            return ids;
        }

        public async Task MakeDirAsync(IonFSObject folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            IIonFSMetadata mh = GetMetadataHandler(folder);
            await mh.MakeDir(folder);
        }

        public async Task<Dictionary<Guid, int>> PutAsync(IonFSObject source, IonFSObject target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "source object cannot be null");
            if (target == null)
                throw new ArgumentNullException(nameof(target), "target object cannot be null");

            if (source.IsRemote)
                throw new IonFSException("Source cannot be a remote object, yet!");

            IonFSMetadata metadata = new IonFSMetadata();
            metadata.Name = string.IsNullOrEmpty(target.Name) ? source.Name : target.Name;

            Dictionary<Guid, int> ids = new Dictionary<Guid, int>();

            IIonFSMetadata mh = GetMetadataHandler(target);
            bool exists = await mh.Exists(target);
            if (!exists)
            {
                using SHA256 sha = SHA256.Create();

                MemoryStream data;
                byte[] bytes;
                if (source.IsText)
                {
                    bytes = Encoding.ASCII.GetBytes(source.Text);
                }
                else
                {
                    bytes = File.ReadAllBytes(source.FullName);
                }

                // Encrypt
                if (Encrypt)
                {
                    if (Verbose) Console.WriteLine("Encrypting...");

                    using Aes a = Aes.Create();

                    metadata.IV = Convert.ToBase64String(a.IV);
                    a.Mode = CipherMode.CBC;
                    a.Padding = PaddingMode.PKCS7;
                    a.Key = Crypto.Key;

                    var encryptor = a.CreateEncryptor();
                    byte[] hashBytes = sha.ComputeHash(bytes);
                    metadata.Hash = Convert.ToBase64String(hashBytes);

                    var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

                    data = new MemoryStream(encrypted);
                }
                else
                {
                    data = new MemoryStream(bytes);

                    byte[] hashBytes = sha.ComputeHash(data);
                    metadata.Hash = Convert.ToBase64String(hashBytes);
                }

                if (UseManifest && !source.IsText)
                {
                    if (Verbose) Console.WriteLine("Using Manifest...");

                    long size = data.Length;
                    metadata.ChunkCount = 1;
                    metadata.MaxSize = MaxSize;
                    metadata.Size = size;
                    metadata.IsManifest = true;

                    if (Verbose)
                    {
                        Console.WriteLine("File: {0} is {1} bytes", source.FullName, size);
                        Console.WriteLine("SHA256: '{0}'", metadata.Hash);
                    }

                    Guid guid = Guid.NewGuid();
                    ion.PutManifestRequest putManifestRequest = new()
                    {
                        Particle = guid.ToString(),
                        ChunkSize = MaxSize,
                        PolicyClassification = Classification,
                        DataStream = data
                    };
                    metadata.Id.Add(guid);

                    ion.PutManifestResult putManifestResult =
                        await GetIonburst().PutAsync(putManifestRequest) as ion.PutManifestResult;

                    ids.Add(guid, putManifestResult.StatusCode);
                }
                else
                {
                    if (Verbose) Console.WriteLine("Native processing...");

                    // Begin Spliting of large data
                    long size = data.Length;
                    long offset = MaxSize;
                    var bursts = new List<Burst>();

                    long chunks = (size / offset) + (size % offset > 0 ? 1 : 0);
                    metadata.ChunkCount = chunks;
                    metadata.MaxSize = MaxSize;
                    metadata.Size = size;

                    if (Verbose)
                    {
                        Console.WriteLine("File: {0} is {1} bytes", source.FullName, size);
                        Console.WriteLine("SHA256: '{0}'", metadata.Hash);
                        Console.WriteLine("Splitting into {0} chunks: {1}", chunks, offset);
                    }

                    using (BinaryReader binaryReader = new(data))
                    {
                        int i = 0;
                        for (long l = 0; l < size; l += offset)
                        {
                            long boundary = l + offset;
                            if (boundary > size)
                                boundary = size;

                            Guid guid = Guid.NewGuid();
                            if (Verbose)
                                Console.WriteLine("Chunk[{2}]:[{4}:{3}] {0} - {1}", l, boundary - 1, i++, boundary - l,
                                    guid);

                            Burst burst = new Burst {start = l, end = boundary, size = boundary - l, id = guid};

                            var buffer = new byte[burst.size];
                            binaryReader.BaseStream.Seek(burst.start, SeekOrigin.Begin);
                            binaryReader.Read(buffer, 0, (int) burst.size);

                            burst.data = buffer;

                            bursts.Add(burst);
                            metadata.Id.Add(guid);
                        }
                    }

                    ParallelOptions parallelOptions = new() {MaxDegreeOfParallelism = 8};
                    Parallel.ForEach(bursts, parallelOptions, (burst) =>
                        {
                            // Ionburst
                            ion.PutObjectRequest putObjectRequest = new()
                            {
                                PolicyClassification = Classification,
                                Particle = burst.id.ToString()
                            };

                            if (Verbose)
                            {
                                using (SHA256 sha = SHA256.Create())
                                {
                                    byte[] hashBytes = sha.ComputeHash(burst.data);
                                    Console.WriteLine(
                                        $"[{burst.id}:{burst.data.Length}] - {Convert.ToBase64String(hashBytes)}");
                                }
                            }

                            putObjectRequest.DataStream = new MemoryStream(burst.data);

                            ion.PutObjectResult putResult;
                            if (mh.Usage == "Secrets")
                            {
                                var ion = GetIonburst();
                                putResult = ion.SecretsPutAsync(putObjectRequest).Result;
                            }
                            else
                            {
                                putResult = GetIonburst().PutAsync(putObjectRequest).Result;
                            }

                            ids.Add(burst.id, putResult.StatusCode);
                        }
                    );
                }

                await mh.PutMetadata(metadata, target);

                data.Close();
            }
            else
                throw new IonFSException("Target File Exists");

            return ids;
        }

        public async Task<Dictionary<Guid, int>> GetAsync(IonFSObject file, IonFSObject to)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (to == null)
                throw new ArgumentNullException(nameof(to));

            if (to.IsRemote)
                throw new IonFSException("Target must be a local file.");

            IIonFSMetadata mh = GetMetadataHandler(file);
            IonFSMetadata metadata = await mh.GetMetadata(file);

            if (metadata.Id.Count == 0)
                throw new IonFSException("No Objects found in metadata");

            Dictionary<Guid, int> ids = new Dictionary<Guid, int>();

            if (string.IsNullOrEmpty(metadata.IV))
            {
                Stream dataStream;
                if (to.IsText)
                {
                    dataStream = new MemoryStream();
                }
                else
                {
                    dataStream = File.Create(to.FullName);
                }

                await LoadStreamFromIonburst(metadata, ids, dataStream, mh, to.IsText);

                if (to.IsText)
                {
                    dataStream.Position = 0;
                    using var reader = new StreamReader(dataStream);
                    to.Text = reader.ReadToEnd();
                }

                dataStream.Dispose();
            }
            else // Encrypted file
            {
                if (!Encrypt)
                    throw new IonFSException("Please provide a key for decrypting the data (--key/--passphase)");

                using MemoryStream ms = new MemoryStream();
                await LoadStreamFromIonburst(metadata, ids, ms, mh, to.IsText);

                using (Aes a = Aes.Create())
                {
                    a.Key = Crypto.Key;
                    a.IV = Convert.FromBase64String(metadata.IV);

                    a.Mode = CipherMode.CBC;
                    a.Padding = PaddingMode.PKCS7;

                    ICryptoTransform decryptor = a.CreateDecryptor(a.Key, a.IV);
                    var encrypted = ms.ToArray();

                    var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

                    if (to.IsText)
                    {
                        ms.Position = 0;
                        to.Text = Encoding.Default.GetString(decrypted);
                    }
                    else
                    {
                        using FileStream fs = File.Open(to.FullName, FileMode.Create);
                        fs.Write(decrypted, 0, decrypted.Length);
                    }
                }
            }

            if (ids.All(id => id.Value == 200) && !to.IsText)
            {
                using (MemoryStream data = new MemoryStream(File.ReadAllBytes(to.FullName)))
                {
                    using (SHA256 sha = SHA256.Create())
                    {
                        byte[] hashBytes = sha.ComputeHash(data);
                        string hash = Convert.ToBase64String(hashBytes);
                        if (Verbose)
                        {
                            Console.WriteLine($"File: {to.FullName}");
                            Console.WriteLine($"File size: {data.Length}");
                            Console.WriteLine($"SHA256: '{hash}'");
                        }

                        if (hash != metadata.Hash)
                        {
                            throw new IonFSChecksumException(metadata, hash);
                        }
                    }
                }
            }

            return ids;
        }

        public async Task<HashSet<KeyValuePair<Guid, int>>> ManifestDelAsync(IonFSObject file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            HashSet<KeyValuePair<Guid, int>> ids = new HashSet<KeyValuePair<Guid, int>>();

            try
            {
                IIonFSMetadata mh = GetMetadataHandler(file);
                IonFSMetadata metadata = await mh.GetMetadata(file);

                // Ionburst
                ion.DeleteManifestRequest delManifestRequest = new()
                {
                    Particle = metadata.Id.First().ToString(),
                    TimeoutSpecified = true,
                    RequestTimeout = new TimeSpan(0, 2, 0)
                };

                ion.DeleteManifestResult delManifestResult;
                delManifestResult = await GetIonburst().DeleteAsync(delManifestRequest) as ion.DeleteManifestResult;

                ids.Add(new KeyValuePair<Guid, int>(metadata.Id.First(), delManifestResult.StatusCode));
                if (Verbose)
                    Console.WriteLine(
                        $"{metadata.Id.First()} {delManifestResult.StatusCode} {delManifestResult.StatusMessage}");


                if (ids.All(id => id.Value == 200))
                {
                    await mh.DelMetadata(file);
                }
            }
            catch (IonFSObjectDoesNotExist e)
            {
                throw new IonFSException($"The FSObject {e.Message} does not exist!", e);
            }

            return ids;
        }

        public async Task<Dictionary<Guid, int>> ManifestPutAsync(IonFSObject source, IonFSObject target)
        {
            IonFSMetadata metadata = new()
            {
                Name = string.IsNullOrEmpty(target.Name) ? source.Name : target.Name
            };

            Dictionary<Guid, int> ids = new();

            IIonFSMetadata mh = GetMetadataHandler(target);

            byte[] bytes = File.ReadAllBytes(source.FullName);
            MemoryStream data;

            // Encrypt
            if (Encrypt)
            {
                using Aes a = Aes.Create();

                metadata.IV = Convert.ToBase64String(a.IV);
                a.Mode = CipherMode.CBC;
                a.Padding = PaddingMode.PKCS7;
                a.Key = Crypto.Key;

                var encryptor = a.CreateEncryptor();
                var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

                data = new MemoryStream(encrypted);
            }
            else
            {
                data = new MemoryStream(bytes);
            }

            using SHA256 sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(data);

            metadata.Hash = Convert.ToBase64String(hashBytes);
            metadata.ChunkCount = 1;
            metadata.MaxSize = MaxSize;
            metadata.Size = data.Length;

            Guid guid = Guid.NewGuid();
            ion.PutManifestRequest putManifestRequest = new()
            {
                Particle = guid.ToString(),
                ChunkSize = MaxSize,
                PolicyClassification = Classification,
                DataStream = data
            };
            metadata.Id.Add(guid);

            ion.PutManifestResult putManifestResult =
                await GetIonburst().PutAsync(putManifestRequest) as ion.PutManifestResult;

            ids.Add(guid, putManifestResult.StatusCode);

            //if (ids.All(r => r.Value == 200))
            await mh.PutMetadata(metadata, target);

            return ids;
        }

        public async Task<Dictionary<Guid, int>> ManifestGetAsync(IonFSObject file, IonFSObject to)
        {
            IIonFSMetadata mh = GetMetadataHandler(file);
            IonFSMetadata metadata = await mh.GetMetadata(file);

            Dictionary<Guid, int> ids = new();

            ion.GetManifestRequest getManifestRequest = new()
            {
                Particle = metadata.Id.First().ToString(),
            };

            ion.GetManifestResult getManifestResult =
                await GetIonburst().GetAsync(getManifestRequest) as ion.GetManifestResult;

            ids.Add(metadata.Id.First(), getManifestResult.StatusCode);

            if (string.IsNullOrEmpty(metadata.IV))
            {
                Stream dataStream = File.Create(to.FullName);

                getManifestResult.DataStream.Seek(0, SeekOrigin.Begin);
                getManifestResult.DataStream.CopyTo(dataStream);

                dataStream.Dispose();
            }
            else // Encrypted file
            {
                if (!Encrypt)
                    throw new IonFSException("Please provide a key for decrypting the data (--key/--passphase)");

                using MemoryStream ms = new MemoryStream();

                getManifestResult.DataStream.Seek(0, SeekOrigin.Begin);
                getManifestResult.DataStream.CopyTo(ms);

                using (Aes a = Aes.Create())
                {
                    a.Key = Crypto.Key;
                    a.IV = Convert.FromBase64String(metadata.IV);

                    a.Mode = CipherMode.CBC;
                    a.Padding = PaddingMode.PKCS7;

                    ICryptoTransform decryptor = a.CreateDecryptor(a.Key, a.IV);
                    var encrypted = ms.ToArray();

                    var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

                    if (to.IsText)
                    {
                        ms.Position = 0;
                        to.Text = Encoding.Default.GetString(decrypted);
                    }
                    else
                    {
                        using FileStream fs = File.Open(to.FullName, FileMode.Create);
                        fs.Write(decrypted, 0, decrypted.Length);
                    }
                }
            }

            return ids;
        }

        private async Task LoadStreamFromIonburst(IonFSMetadata metadata, Dictionary<Guid, int> ids, Stream stream,
            IIonFSMetadata mh, bool isText)
        {
            if (metadata.IsManifest && !isText)
            {
                // Manifest chunking
                ion.GetManifestRequest getManifestRequest = new()
                {
                    Particle = metadata.Id.First().ToString(),
                };

                ion.GetManifestResult getManifestResult =
                    await GetIonburst().GetAsync(getManifestRequest) as ion.GetManifestResult;
                getManifestResult.DataStream.Seek(0, SeekOrigin.Begin);
                getManifestResult.DataStream.CopyTo(stream);

                ids.Add(metadata.Id.First(), getManifestResult.StatusCode);
            }
            else
            {
                // Native chunking
                foreach (Guid id in metadata.Id)
                {
                    ion.GetObjectRequest getObjectRequest = new()
                    {
                        Particle = id.ToString()
                    };

                    ion.GetObjectResult getObjectResult;

                    if (mh.Usage == "Secrets")
                    {
                        getObjectResult = await GetIonburst().SecretsGetAsync(getObjectRequest);
                    }
                    else
                    {
                        getObjectResult = await GetIonburst().GetAsync(getObjectRequest);
                    }

                    ids.Add(id, getObjectResult.StatusCode);

                    if (getObjectResult.StatusCode == 200)
                    {
                        getObjectResult.DataStream.Seek(0, SeekOrigin.Begin);
                        getObjectResult.DataStream.CopyTo(stream);
                        if (Verbose)
                        {
                            using (SHA256 sha = SHA256.Create())
                            {
                                getObjectResult.DataStream.Seek(0, SeekOrigin.Begin);
                                byte[] hashBytes = sha.ComputeHash(getObjectResult.DataStream);
                                Console.WriteLine(
                                    $"[{id}:{getObjectResult.DataStream.Length}] - {Convert.ToBase64String(hashBytes)}");
                            }
                        }
                    }
                }
            }
        }

        public async Task MoveAsync(IonFSObject source, IonFSObject target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            // Remote to Remote - metadata move
            //if (source.StartsWith(fsPrefix, StringComparison.Ordinal) && target.StartsWith(fsPrefix, StringComparison.Ordinal))
            if (source.IsRemote && target.IsRemote)
            {
                // Not currently supporting repo to repo move
                IIonFSMetadata sourceHandler = GetMetadataHandler(source);
                IIonFSMetadata targetHandler = GetMetadataHandler(target);

                if (source.Repository != target.Repository)
                {
                    IonFSMetadata metadata = await sourceHandler.GetMetadata(source);
                    if (targetHandler.Exists(target).Result)
                    {
                        throw new IonFSException("target exists");
                    }
                    else
                    {
                        await targetHandler.PutMetadata(metadata, target);
                        await sourceHandler.DelMetadata(source);
                    }
                }
                else
                {
                    await sourceHandler.Move(source, target);
                }
            }
            // Remote FS to local FS, GET and DEL
            else if (source.IsRemote)
            {
                // Essentially a GET
                await GetAsync(source, target);
                await DelAsync(source);
            }
            // Local FS to Remote FS, PUT and local DEL
            else if (target.IsRemote)
            {
                // essentially a PUT
                await PutAsync(source, target);
                Console.WriteLine($"*** WARNING ***: Local file [{source}] is not being removed!");
                // DELETE local file
            }
            // defaults to Remote to Remote
            else
            {
                IIonFSMetadata mh = GetMetadataHandler(source);
                await mh.Move(source, target);
            }
        }

        public async Task CopyAsync(IonFSObject source, IonFSObject target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            // local FS to remote FS
            if (!source.IsRemote && target.IsRemote)
            {
                // essentially a PUT
                await PutAsync(source, target);
            }
            // remote FS to local FS
            else if (source.IsRemote && !target.IsRemote)
            {
                // essentially a GET
                await GetAsync(source, target);
            }
            // between two different remote FS
            else if (source.IsRemote && target.IsRemote)
            {
                // Can't (don't want to support) 
                if (source.Repository == target.Repository)
                    throw new IonFSException("We don't current support copying data within the remote FS");

                IIonFSMetadata sourceHandler = GetMetadataHandler(source);
                IIonFSMetadata targetHandler = GetMetadataHandler(target);

                IonFSMetadata metadata = await sourceHandler.GetMetadata(source);
                await targetHandler.PutMetadata(metadata, target);
                //await sourceHandler.DelMetadata(source);
            }
            // local to local
            else
            {
                throw new IonFSException("Please use OS tools when working with the local filesystem");
            }
        }

        public async Task<List<IonFSObject>> ListAsync(IonFSObject folder, bool recursive = false)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            IIonFSMetadata mh = GetMetadataHandler(folder);
            List<IonFSObject> items = await mh.List(folder, recursive);

            items.ForEach(i =>
            {
                i.Repository = mh.RepositoryName;
                i.HasRepository = true;
            });

            items.Sort((s1, s2) => { return s1.FullName.CompareTo(s2.FullName); });

            return items;
        }

        public async Task<IonFSMetadata> GetMetadata(IonFSObject file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (file.IsFolder)
                throw new IonFSException("Metadata does not exist for folders.");

            IIonFSMetadata mh = GetMetadataHandler(file);
            IonFSMetadata metadata = await mh.GetMetadata(file);

            return metadata;
        }

        public async Task<IDictionary<int, string>> GetClassifications()
        {
            ion.GetPolicyClassificationRequest classificationRequest = new ion.GetPolicyClassificationRequest();
            ion.GetPolicyClassificationResult classificationResult =
                await GetIonburst().GetClassificationsAsync(classificationRequest);

            if (classificationResult.StatusCode != 200)
                throw new IonFSException(classificationResult.StatusMessage);

            return classificationResult.ClassificationDictionary;
        }

        public async Task Synchronise(IonFSObject sourceFolder, IonFSObject targetFolder)
        {
            if (sourceFolder == null)
                throw new ArgumentNullException(nameof(sourceFolder));
            if (targetFolder == null)
                throw new ArgumentNullException(nameof(targetFolder));

            if (!(sourceFolder.IsFolder && targetFolder.IsFolder))
                throw new IonFSException("Only Folders can be synchronised");

            throw new IonFSException("Folder Sync has not been implemented.");
        }

        public async Task AddMetadata(IonFSObject source, IonFSObject target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            string data = File.ReadAllText(source.FullName);
            IonFSMetadata metadata = JsonConvert.DeserializeObject<IonFSMetadata>(data);

            IIonFSMetadata mh = GetMetadataHandler(target);
            await mh.PutMetadata(metadata, target);
        }

        // Factory methods for creating IonFSObjects

        public IonFSObject FromRemoteFolder(string? folder, bool autoAddDelimiter = true)
        {
            string fs = "ion://";
            bool isRemote = true;

            if (string.IsNullOrEmpty(folder) || folder is null)
                return new IonFSObject
                {
                    FS = fs, Name = "", Path = "", IsFolder = true, IsRoot = true, IsRemote = isRemote,
                    HasRepository = false
                };

            if (string.IsNullOrEmpty(folder) || folder is null || folder == fs)
                return new IonFSObject
                {
                    FS = fs, Name = "", Path = "", IsFolder = true, IsRoot = true, IsRemote = true,
                    HasRepository = false
                };

            if (!string.IsNullOrEmpty(folder) && folder.StartsWith(fs, StringComparison.Ordinal))
            {
                isRemote = true;
            }

            if (autoAddDelimiter && !folder.EndsWith(@"/", StringComparison.Ordinal))
                folder += @"/";

            if (folder.StartsWith(fs, StringComparison.Ordinal)
                && folder.EndsWith(@"/", StringComparison.Ordinal))
            {
                string path = folder.Replace(fs, "", StringComparison.Ordinal);
                string firstElement = path.Substring(0, path.IndexOf('/', StringComparison.Ordinal));
                string repo = "";
                bool hasRepo = false;
                bool isRoot = false;
                if (Repositories != null && Repositories.Any(r => r.Repository == firstElement))
                {
                    repo = firstElement;
                    path = path.Replace(repo + "/", "", StringComparison.Ordinal);
                    hasRepo = true;
                    if (string.IsNullOrEmpty(path))
                    {
                        isRoot = true;
                        //path = "/";
                    }
                }

                //if (!String.IsNullOrEmpty(path) && !path.StartsWith(@"/")) path = @"/" + path;

                IonFSObject fso = new IonFSObject
                {
                    FS = fs, Repository = repo, Name = "", Path = path, IsFolder = true, IsFile = false,
                    IsRoot = isRoot, IsText = false, IsRemote = true, HasRepository = hasRepo
                };
                return fso;
            }
            else
                throw new RemoteFSException(folder);
        }

        public IonFSObject FromRemoteFile(string fullFSName)
        {
            if (string.IsNullOrEmpty(fullFSName))
                throw new ArgumentNullException(nameof(fullFSName), "fullFSName cannot be Null or Empty.");

            string pathHolder = fullFSName.Substring(0, fullFSName.LastIndexOf(@"/") + 1);
            string path = fullFSName == "ion://" || (fullFSName.Replace("ion://", "").LastIndexOf(@"/") == -1)
                ? ""
                : fullFSName.Substring(0, fullFSName.LastIndexOf(@"/"));
            string filename = string.IsNullOrEmpty(path)
                ? fullFSName.Replace("ion://", "")
                : fullFSName.Replace(pathHolder, "");

            return FromRemoteFile(filename, path);
        }

        public IonFSObject FromRemoteFile(string file, string path)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file), "File cannot be Null.");
            if (path == null)
                throw new ArgumentNullException(nameof(path), "Path cannot be Null or Empty.");

            IonFSObject fso = FromRemoteFolder(path);
            fso.IsText = false;

            if (string.IsNullOrEmpty(file))
            {
                fso.Name = "";
                fso.IsFolder = true;
                fso.IsFile = false;
            }
            else
            {
                fso.Name = file;
                fso.IsFolder = false;
                fso.IsFile = true;
            }

            return fso;
        }

        public IonFSObject FromString(string val)
        {
            if (val.StartsWith(@"ion://", StringComparison.Ordinal))
                return FromRemoteFile(val);
            else
                return IonFSObject.FromLocalFile(val);
        }


        // Internal tools for lower level interaction with ionburst directly
        // - use with care

        public async Task RemoveById(Guid gid)
        {
            // Ionburst
            ion.DeleteObjectRequest delRequest = new ion.DeleteObjectRequest
            {
                Particle = gid.ToString(),
                TimeoutSpecified = true,
                RequestTimeout = new TimeSpan(0, 2, 0)
            };
            ion.DeleteObjectResult delResult = await GetIonburst().DeleteAsync(delRequest);

            if (delResult.StatusCode != 200)
                throw new IonFSException(
                    $"Ionburst Cloud Delete operation returned non-200 StatusCode {delResult.StatusCode}");
        }

        public async Task RemoveMetadata(IonFSObject fso)
        {
            IIonFSMetadata mh = GetMetadataHandler(fso);

            await mh.DelMetadata(fso);
        }

        public async Task<Dictionary<Guid, int>> GetChunk(Guid gid)
        {
            using var stream = File.Create(gid.ToString());

            ion.GetObjectRequest getObjectRequest = new ion.GetObjectRequest
            {
                Particle = gid.ToString()
            };

            Dictionary<Guid, int> ids = new Dictionary<Guid, int>();

            ion.GetObjectResult getObjectResult = await GetIonburst().GetAsync(getObjectRequest);
            ids.Add(gid, getObjectResult.StatusCode);

            if (getObjectResult.StatusCode == 200)
            {
                getObjectResult.DataStream.Seek(0, SeekOrigin.Begin);
                getObjectResult.DataStream.CopyTo(stream);
                if (Verbose)
                {
                    using (SHA256 sha = SHA256.Create())
                    {
                        getObjectResult.DataStream.Seek(0, SeekOrigin.Begin);
                        byte[] hashBytes = sha.ComputeHash(getObjectResult.DataStream);
                        Console.WriteLine(
                            $"[{gid}:{getObjectResult.DataStream.Length}] - {Convert.ToBase64String(hashBytes)}");
                    }
                }
            }

            return ids;
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            return;
        }
    }
}
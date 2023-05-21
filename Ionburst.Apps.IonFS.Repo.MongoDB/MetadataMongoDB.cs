// Copyright Ionburst Limited 2018-2021

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

using Newtonsoft.Json;

using Ionburst.Apps.IonFS.Model;

namespace Ionburst.Apps.IonFS.Repo.Mongo
{
    public class MetadataMongoDB : IIonFSMetadata
    {
        private readonly MongoDBWrapper _mongo;
        private readonly string _databaseName;
        private const string COLLECTION_NAME = "IonFSMetadata";
        public bool Verbose { get; set; } = false;
        public string RepositoryName { get; set; }
        public string Usage { get; set; }

        public MetadataMongoDB(string databaseName, string repoName, string repoUsage)
        {
            _databaseName = databaseName;
            RepositoryName = repoName;
            Usage = repoUsage;
            _mongo = new MongoDBWrapper(databaseName, repoName);
        }

        public string GetDataStore()
        {
            return _databaseName;
        }

        public async Task<IonFSMetadata> GetMetadata(IonFSObject file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            IonFSMetadata data = null;

            try
            {
                IMongoCollection<IMongoDBIonFSMetadata> objectMetadata = _mongo.MongoDB.GetCollection<IMongoDBIonFSMetadata>(COLLECTION_NAME);
                var filter = Builders<IMongoDBIonFSMetadata>.Filter.Eq("Key", file.FullName);
                List<IMongoDBIonFSMetadata> metadataList = await objectMetadata.Find(filter).ToListAsync();
                IMongoDBIonFSMetadata metadataEntry = metadataList[0];

                data = JsonConvert.DeserializeObject<IonFSMetadata>(metadataEntry.Metadata);

                if (Verbose) Console.WriteLine(data);
            }
            catch (MongoException e)
            {
                throw new Exception($"MongoDB error occurred. Exception: {e}");
            }
            catch (Exception e)
            {
                throw new Exception("MetdataMongoDB.GetMetadata exception", e);
            }

            return data;
        }

        public async Task<bool> Exists(IonFSObject fso)
        {
            if (fso == null)
                throw new ArgumentNullException(nameof(fso));

            bool exists = false;

            try
            {
                IMongoCollection<IMongoDBIonFSMetadata> objectMetadata = _mongo.MongoDB.GetCollection<IMongoDBIonFSMetadata>(COLLECTION_NAME);
                var filter = Builders<IMongoDBIonFSMetadata>.Filter.Eq("Key", fso.FullName);
                List<IMongoDBIonFSMetadata> metadataList = await objectMetadata.Find(filter).ToListAsync();

                if (fso.IsRoot && fso.IsFolder)
                {
                    exists = true;
                }
                else
                {
                    if (metadataList.Count > 0)
                    {
                        IMongoDBIonFSMetadata metadataEntry = metadataList[0];
                        if ((metadataEntry.Key == fso.FullName) && !fso.IsFolder)
                            exists = true;
                        else if ((metadataEntry.Key == fso.Path) && fso.IsFolder)
                            exists = true;
                    }
                }
            }
            catch (MongoException e)
            {
                Console.WriteLine($"MongoDB error occurred. Exception: {e}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"MetdataMongoDB.Exists exception: {e}");
            }

            return exists;
        }

        public async Task<bool> IsEmpty(IonFSObject folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            List<IonFSObject> items = await List(folder);
            return (items.Count == 0);
        }

        public async Task<List<IonFSObject>> List(IonFSObject folder, bool recursive = false)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            List<IonFSObject> items = new List<IonFSObject>();

            try
            {
                IMongoCollection<IMongoDBIonFSMetadata> objectMetadata = _mongo.MongoDB.GetCollection<IMongoDBIonFSMetadata>(COLLECTION_NAME);
                List<IMongoDBIonFSMetadata> metadataList = await objectMetadata.Find(_ => true).ToListAsync();
                if (metadataList.Count > 0)
                {
                    foreach (IMongoDBIonFSMetadata metadataobject in metadataList)
                    {
                        //if (metadataobject.Key != folder.Path || recursive)
                        //{
                            IonFSObject fso = IonFSObject.FromLocalFile(metadataobject.Key);
                            fso.FS = "ion://";
                            fso.IsRemote = true;
                            if (metadataobject.Key.EndsWith(@"/", StringComparison.Ordinal))
                                fso.IsFolder = true;
                            else
                                fso.IsFolder = false;
                            fso.LastModified = metadataobject.LastModified;
                            items.Add(fso);
                        //}
                    }
                }
            }
            catch (MongoException e)
            {
                Console.WriteLine($"MongoDB error occurred. Exception: {e}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"MetadataMongoDB exception: {e}");
            }

            return items;
        }

        public async Task MakeDir(IonFSObject folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            try
            {
                if (!(await Exists(folder)))
                {
                    IMongoDBIonFSMetadata obj = new MongoDBIonFSMetadata
                    {
                        _id = ObjectId.GenerateNewId(),
                        Key = folder.Path,
                        LastModified = DateTime.UtcNow
                    };
                    IMongoCollection<IMongoDBIonFSMetadata> objectMetadata = _mongo.MongoDB.GetCollection<IMongoDBIonFSMetadata>(COLLECTION_NAME);
                    await objectMetadata.InsertOneAsync(obj);
                }
            }
            catch (MongoException e)
            {
                Console.WriteLine($"MongoDB error occurred. Exception: {e}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"MetadataMongoDB.MakeDir exception: {e}");
            }
        }

        public async Task Move(IonFSObject source, IonFSObject target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            try
            {
                IMongoCollection<IMongoDBIonFSMetadata> objectMetadata = _mongo.MongoDB.GetCollection<IMongoDBIonFSMetadata>(COLLECTION_NAME);
                var filter = Builders<IMongoDBIonFSMetadata>.Filter.Eq("Key", source.FullName);
                List<IMongoDBIonFSMetadata> metadataList = await objectMetadata.Find(filter).ToListAsync();
                IMongoDBIonFSMetadata metadataEntry = metadataList[0];

                IMongoDBIonFSMetadata newObj = new MongoDBIonFSMetadata
                {
                    Key = target.FullName,
                    Metadata = metadataEntry.Metadata,
                    LastModified = DateTime.UtcNow
                };
                await objectMetadata.InsertOneAsync(newObj);

                filter = Builders<IMongoDBIonFSMetadata>.Filter.Eq("Key", source.FullName);
                await objectMetadata.DeleteOneAsync(filter);
            }
            catch (MongoException e)
            {
                Console.WriteLine($"MongoDB error occurred. Exception: {e}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"MetadataMongoDB.Move exception:  {e}");
            }
        }

        public async Task PutMetadata(IonFSMetadata metadata, IonFSObject folder)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            try
            {
                if (metadata != null)
                {
                    String data = JsonConvert.SerializeObject(metadata, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    IMongoDBIonFSMetadata obj = new MongoDBIonFSMetadata
                    {
                        _id = ObjectId.GenerateNewId(),
                        Key = folder.FullName,
                        Metadata = (data),
                        LastModified = DateTime.UtcNow
                    };

                    IMongoCollection<IMongoDBIonFSMetadata> objectMetadata = _mongo.MongoDB.GetCollection<IMongoDBIonFSMetadata>(COLLECTION_NAME);
                    await objectMetadata.InsertOneAsync(obj);

                    if (Verbose) Console.WriteLine(data);
                }
                else
                    throw new Exception("metadata is null");
            }
            catch (MongoException e)
            {
                Console.WriteLine($"MongoDB error occurred. Exception: {e}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"MetadataMongoDB.PutMetadata exception:  {e}");
            }
        }

        public async Task DelMetadata(IonFSObject fSObject)
        {
            if (fSObject == null)
                throw new ArgumentNullException(nameof(fSObject));

            try
            {
                IMongoCollection<IMongoDBIonFSMetadata> objectMetadata = _mongo.MongoDB.GetCollection<IMongoDBIonFSMetadata>(COLLECTION_NAME);
                var filter = Builders<IMongoDBIonFSMetadata>.Filter.Eq("Key", fSObject.FullName);
                await objectMetadata.DeleteOneAsync(filter);
            }
            catch (MongoException e)
            {
                Console.WriteLine($"MongoDB error occurred. Exception: {e}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"MetadataMongoDB.DelMetadata exception:  {e}");
            }
        }
        
        public async Task<List<IonFSSearchResult>> Search(IonFSObject folder, string tag, string regex, bool recursive)
        {
            throw new NotImplementedException();
        }

    }
}

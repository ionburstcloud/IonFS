// Copyright Ionburst Limited 2018-2021

using System;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Amazon.S3;
using s3 = Amazon.S3.Model;
using System.Collections.Generic;

using Ionburst.Apps.IonFS.Exceptions;
using Ionburst.Apps.IonFS.Model;

namespace Ionburst.Apps.IonFS.Repo.S3
{
    public class MetadataS3 : IIonFSMetadata
    {
        private readonly S3Wrapper s3;

        public bool Verbose { get; set; } = false;
        public string RepositoryName { get; set; }
        public string Usage { get; set; }

        public MetadataS3()
        {
            s3 = new S3Wrapper();
        }

        public MetadataS3(string bucketName, string repoName, string repoUsage)
        {
            RepositoryName = repoName;
            Usage = repoUsage;
            s3 = new S3Wrapper();
            s3.SetBucket(bucketName);
        }

        public string GetDataStore()
        {
            return s3.GetBucket();
        }

        public async Task<IonFSMetadata> GetMetadata(IonFSObject file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            IonFSMetadata data = null;

            if (!Exists(file).Result)
                throw new IonFSObjectDoesNotExist(file);

            try
            {
                s3.GetObjectRequest getRequest = new s3.GetObjectRequest
                {
                    BucketName = s3.GetBucket(),
                    Key = file.FullName
                };

                using s3.GetObjectResponse response = await s3.S3.GetObjectAsync(getRequest);
                using Stream responseStream = response.ResponseStream;
                using StreamReader reader = new StreamReader(responseStream);

                string responseBody = reader.ReadToEnd();
                data = JsonConvert.DeserializeObject<IonFSMetadata>(responseBody);

                if (Verbose) Console.WriteLine(responseBody);
            }
            catch (AmazonS3Exception e)
            {
                throw new IonFSObjectDoesNotExist("S3 Exception", file, e);
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
                s3.ListObjectsV2Request request = new s3.ListObjectsV2Request
                {
                    BucketName = s3.GetBucket(),
                    Prefix = fso.FullName,
                    Delimiter = "/"
                };
                s3.ListObjectsV2Response response;

                var t = s3.S3;
                response = await t.ListObjectsV2Async(request);

                if (fso.IsRoot && fso.IsFolder)
                    exists = true;

                if (response.S3Objects.Exists(x => x.Key == fso.FullName) && !fso.IsFolder)
                    exists = true;
                else if (response.S3Objects.Exists(x => x.Key == fso.Path) && fso.IsFolder)
                    exists = true;
            }
            catch (AmazonS3Exception e)
            {
                throw new IonFSException("S3 Exception", fso, e);
            }
            catch (Exception e)
            {
                throw e;
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
                s3.ListObjectsV2Request request = new s3.ListObjectsV2Request
                {
                    BucketName = s3.GetBucket(),
                    Prefix = folder.IsRoot?"":folder.Path,
                    Delimiter = recursive?"":@"/"
                };
                s3.ListObjectsV2Response response;

                response = await s3.S3.ListObjectsV2Async(request);

                foreach (var common in response.CommonPrefixes)
                {
                    items.Add(new IonFSObject { FS = "ion://", Name = "", Path = common, IsFolder = true, IsRemote = true } );
                }

                // Process the response.
                foreach (s3.S3Object entry in response.S3Objects)
                {
                    if (entry.Key != folder.Path || recursive)
                    {
                        IonFSObject fso = IonFSObject.FromLocalFile(entry.Key);
                        fso.FS = "ion://";
                        fso.IsRemote = true;
                        if (entry.Key.EndsWith(@"/", StringComparison.Ordinal))
                            fso.IsFolder = true;
                        else
                            fso.IsFolder = false;
                        fso.LastModified = entry.LastModified;
                        items.Add(fso);
                    }
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                throw new IonFSException("S3 Exception", folder, amazonS3Exception);
            }

            return items;
        }

        public async Task MakeDir(IonFSObject folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            if (Exists(folder).Result)
                throw new IonFSException("Folder already exists");

            try
            {
                // S3
                s3.PutObjectRequest s3PutRequest = new s3.PutObjectRequest
                {
                    BucketName = s3.GetBucket(),
                    Key = folder.Path
                };

                s3.PutObjectResponse response = await s3.S3.PutObjectAsync(s3PutRequest);
            }
            catch (AmazonS3Exception e)
            {
                throw new IonFSException("S3 Exception", folder, e);
            }
        }

        public async Task Move(IonFSObject source, IonFSObject target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (!Exists(source).Result)
                throw new IonFSException("Source must exist");
            if (Exists(target).Result)
                throw new IonFSException("Target already exists");

            try
            {
                if (string.IsNullOrEmpty(target.Name)) target.Name = source.Name;

                // S3
                s3.CopyObjectRequest copyRequest = new s3.CopyObjectRequest
                {
                    DestinationBucket = s3.GetBucket(),
                    SourceBucket = s3.GetBucket(),
                    DestinationKey = target.FullName,
                    SourceKey = source.FullName
                };

                s3.DeleteObjectRequest delRequest = new s3.DeleteObjectRequest
                {
                    BucketName = s3.GetBucket(),
                    Key = source.FullName
                };

                // Overwrites target if exists!!!
                await s3.S3.CopyObjectAsync(copyRequest);
                await s3.S3.DeleteObjectAsync(delRequest);

            }
            catch (AmazonS3Exception e)
            {
                throw new IonFSException("S3 Exception", e);
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
                    if (string.IsNullOrEmpty(folder.Name)) folder.Name = metadata.Name;

                    String data = JsonConvert.SerializeObject(metadata, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    s3.PutObjectRequest s3PutRequest = new s3.PutObjectRequest
                    {
                        BucketName = s3.GetBucket(),
                        Key = folder.FullName,
                        ContentBody = data
                    };

                    var t = s3.S3;
                    s3.PutObjectResponse response = await s3.S3.PutObjectAsync(s3PutRequest);

                    if (Verbose) Console.WriteLine(data);
                }
                else
                    throw new IonFSException("metadata is null");
            }
            catch (AmazonS3Exception e)
            {
                throw new IonFSException("S3 Exception", e);
            }
        }

        public async Task DelMetadata(IonFSObject fSObject)
        {
            if (fSObject == null)
                throw new ArgumentNullException(nameof(fSObject));

            try
            {
                s3.DeleteObjectRequest s3DelRequest = new s3.DeleteObjectRequest
                {
                    BucketName = s3.GetBucket(),
                    Key = fSObject.FullName
                };

                s3.DeleteObjectResponse response = await s3.S3.DeleteObjectAsync(s3DelRequest);
            }
            catch (AmazonS3Exception e)
            {
                throw new IonFSException("S3 Exception", fSObject, e);
            }
        }
    }
}

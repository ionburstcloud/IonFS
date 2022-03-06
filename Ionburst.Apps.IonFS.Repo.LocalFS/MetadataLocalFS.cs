using System;
using static System.Environment;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Ionburst.Apps.IonFS.Model;
using Ionburst.Apps.IonFS.Exceptions;
using Newtonsoft.Json;
using System.Linq;

namespace Ionburst.Apps.IonFS.Repo.LocalFS
{
    public class MetadataLocalFS : IIonFSMetadata
    {
        DirectoryInfo _dataStoreFolder;

        public bool Verbose { get; set; }
        public string RepositoryName { get; set; }
        public string Usage { get; set; }

        public MetadataLocalFS()
        {
            throw new NotImplementedException();
        }

        public MetadataLocalFS(string folderPath, string repoName, string repoUsage)
        {
            RepositoryName = repoName;
            Usage = repoUsage;

            _dataStoreFolder = new DirectoryInfo(folderPath);
        }

        public async Task DelMetadata(IonFSObject fso)
        {
            if (fso == null)
                throw new ArgumentNullException(nameof(fso));

            try
            {
                if (fso.IsFolder)
                    Directory.Delete(Path.Combine(_dataStoreFolder.FullName, fso.FullName));
                else
                    File.Delete(Path.Combine(_dataStoreFolder.FullName, fso.FullName));
            }
            catch (Exception e)
            {
                throw new IonFSException("LocalFS Exception", fso, e);
            }
        }

        public async Task<bool> Exists(IonFSObject fso)
        {
            bool exists;
            if (fso.IsFolder)
                exists = Directory.Exists(Path.Combine(_dataStoreFolder.FullName,
                    fso.FullName.Replace('/', Path.DirectorySeparatorChar)));
            else
                exists = File.Exists(Path.Combine(_dataStoreFolder.FullName,
                    fso.FullName.Replace('/', Path.DirectorySeparatorChar)));

            return exists;
        }

        public string GetDataStore()
        {
            return _dataStoreFolder.FullName;
        }

        public async Task<IonFSMetadata> GetMetadata(IonFSObject file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            IonFSMetadata data;

            if (!Exists(file).Result)
                throw new IonFSObjectDoesNotExist(file);

            try
            {
                string metadata = File.ReadAllText(Path.Combine(_dataStoreFolder.FullName, file.FullName));
                data = JsonConvert.DeserializeObject<IonFSMetadata>(metadata);

                if (Verbose) Console.WriteLine(metadata);
            }
            catch (Exception e)
            {
                throw new IonFSObjectDoesNotExist("LocalFS Exception", file, e);
            }

            return data;
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
                SearchOption so = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = from file
                        in Directory.EnumerateFileSystemEntries(
                            Path.Combine(_dataStoreFolder.FullName, folder.FullName),
                            "*", so
                        )
                    select new
                    {
                        FileData = new FileInfo(file.Replace('/', Path.DirectorySeparatorChar))
                    };

                foreach (var f in files)
                {
                    IonFSObject fso = IonFSObject.FromLocalFile(
                        f.FileData.FullName.Replace(_dataStoreFolder.FullName + Path.DirectorySeparatorChar, ""));
                    fso.FS = "ion://";
                    fso.IsRemote = true;
                    fso.IsFolder = (f.FileData.Attributes == FileAttributes.Directory);
                    fso.LastModified = f.FileData.LastWriteTime;

                    items.Add(fso);
                }
            }
            catch (Exception e)
            {
                throw new IonFSException("LocalFS Exception", folder, e);
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
                Directory.CreateDirectory(Path.Combine(_dataStoreFolder.FullName, folder.FullName));
            }
            catch (Exception e)
            {
                throw new IonFSException("LocalFS Exception", folder, e);
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
            if (Exists(target).Result && !target.IsFolder)
                throw new IonFSException("Target already exists");

            try
            {
                if (string.IsNullOrEmpty(target.Name)) target.Name = source.Name;

                string path = _dataStoreFolder.FullName;
                string sourceFilename = Path.Combine(path, source.FullName.Replace('/', Path.DirectorySeparatorChar));
                string targetFilename = Path.Combine(path, target.FullName.Replace('/', Path.DirectorySeparatorChar));

                File.Move(sourceFilename, targetFilename);
            }
            catch (Exception e)
            {
                throw new IonFSException("LocalFS Exception", e);
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
                using (StreamWriter streamWriter =
                       File.CreateText(Path.Combine(_dataStoreFolder.FullName, folder.FullName)))
                {
                    String data = JsonConvert.SerializeObject(metadata);

                    streamWriter.WriteLine(data);
                    streamWriter.Dispose();
                }
            }
            catch (Exception e)
            {
                throw new IonFSException("LocalFS Exception", e);
            }
        }
    }
}
// Copyright Ionburst Limited 2018-2021

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ionburst.Apps.IonFS.Model
{
    public interface IIonFSMetadata
    {
        public bool Verbose { get;  set; }
        public string RepositoryName { get; set; }
        public string Usage { get; set; }
        public string GetDataStore();

        public Task MakeDir(IonFSObject folder);
        public Task Move(IonFSObject source, IonFSObject target);
        public Task<List<IonFSObject>> List(IonFSObject folder, bool recursive);
        public Task<bool> IsEmpty(IonFSObject folder);
        public Task<bool> Exists(IonFSObject fso);
        public Task PutMetadata(IonFSMetadata metadata, IonFSObject folder);
        public Task<IonFSMetadata> GetMetadata(IonFSObject file);
        public Task DelMetadata(IonFSObject fsObject);
        public Task<List<IonFSSearchResult>> Search(IonFSObject folder, string tag, string regex, bool recursive);

    }
}

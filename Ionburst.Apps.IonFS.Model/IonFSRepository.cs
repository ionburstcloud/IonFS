// Copyright Ionburst Limited 2018-2021

namespace Ionburst.Apps.IonFS.Model
{
    public class IonFSRepository
    {
        public bool IsDefault { get; set; } = false;
        public string Repository { get; set; }
        public string DataStore { get; set; }
        public string Usage { get; set; }
        public IIonFSMetadata Metadata { get; set; }
    }
}

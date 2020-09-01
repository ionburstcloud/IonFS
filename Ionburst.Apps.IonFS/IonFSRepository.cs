// Copyright Ionburst Limited 2020
using System;
using System.Collections.Generic;
using System.Text;

namespace Ionburst.Apps.IonFS
{
    public class IonFSRepository
    {
        public bool IsDefault { get; set; } = false;
        public string Repository { get; set; }
        public string DataStore { get; set; }
        public IIonFSMetadata Metadata { get; set; }
    }
}

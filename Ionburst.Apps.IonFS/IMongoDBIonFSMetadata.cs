// Copyright Ionburst Limited 2020
using System;
using System.Collections.Generic;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ionburst.Apps.IonFS
{
    public interface IMongoDBIonFSMetadata
    {
        ObjectId _id { get; set; }
        string Key { get; set; }
        string Metadata { get; set; }
        DateTime LastModified { get; set; }
    }
}

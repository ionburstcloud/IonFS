// Copyright Ionburst Limited 2018-2021

using System;
using MongoDB.Bson;

namespace Ionburst.Apps.IonFS.Repo.Mongo
{
    public interface IMongoDBIonFSMetadata
    {
        ObjectId _id { get; set; }
        string Key { get; set; }
        string Metadata { get; set; }
        DateTime LastModified { get; set; }
    }
}
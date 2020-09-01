// Copyright Ionburst Limited 2020
using System;

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ionburst.Apps.IonFS
{
    [BsonIgnoreExtraElements]
    [BsonDiscriminator("MongoDBIonFSMetadata")]
    public class MongoDBIonFSMetadata : IMongoDBIonFSMetadata
    {
        [BsonId]
        public ObjectId _id { get; set; }
        [BsonElement("Key")]
        public string Key { get; set; }
        [BsonElement("Metadata")]
        public string Metadata { get; set; }
        [BsonElement("DateAdded")]
        public DateTime LastModified { get; set; }
    }
}

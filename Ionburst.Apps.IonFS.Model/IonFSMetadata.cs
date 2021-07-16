// Copyright Ionburst Limited 2018-2021

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ionburst.Apps.IonFS.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class IonFSMetadata
    {
        public IonFSMetadata() { Id = new List<Guid>(); }

        [JsonProperty]
        public List<Guid> Id { get; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public long ChunkCount { get; set; }

        [JsonProperty]
        public long MaxSize { get; set; }

        [JsonProperty]
        public long Size { get; set; }

        [JsonProperty]
        public string Hash { get; set; }

        [JsonProperty]
        public string IV { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}

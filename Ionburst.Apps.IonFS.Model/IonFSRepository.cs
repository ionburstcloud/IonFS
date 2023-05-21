// Copyright Ionburst Limited 2018-2021

using Ionburst.Apps.IonFS.Exceptions;
using Ionburst.SDK;
using Microsoft.Extensions.Configuration;

namespace Ionburst.Apps.IonFS.Model
{
    public class IonFSRepository
    {
        private IIonburstClient ionburstPrivate;

        public bool IsDefault { get; set; } = false;
        public string Repository { get; set; }
        public string DataStore { get; set; }
        public string Usage { get; set; }
        public IIonFSMetadata Metadata { get; set; }
        public IIonburstClient GetIonburst(IConfiguration ionFSConfig)
        {
            if (ionburstPrivate is null)
                ionburstPrivate = IonburstClientFactory.CreateIonburstClient(ionFSConfig);

            if (!ionburstPrivate.CheckIonburstAPI().Result)
                throw new IonFSException($"** WARNING **: Ionburst Cloud is currently unavailable!");

            return ionburstPrivate;
        }

    }
}

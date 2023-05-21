// Copyright Ionburst Limited 2018-2021

using Ionburst.Apps.IonFS.Exceptions;
using Ionburst.SDK;
using Microsoft.Extensions.Configuration;

namespace Ionburst.Apps.IonFS.Model
{
    public class IonFSRepository
    {
        private IIonburstClient ionburstPrivate;
        private IonburstConfiguration ionburstConfig;

        public IonFSRepository()
        {
        }

        public bool IsDefault { get; set; } = false;
        public string Repository { get; set; }
        public string DataStore { get; set; }
        public string Usage { get; set; }
        public IIonFSMetadata Metadata { get; set; }

        public IonburstConfiguration IonburstConfig
        {
            get { return ionburstConfig; }
            set { ionburstConfig = value; }
        }

        public IIonburstClient GetIonburst()
        {
            ionburstPrivate ??= new IonburstClient()
                .WithProfile(ionburstConfig.Profile)
                .WithIonburstUri(ionburstConfig.IonburstUri)
                .Build();

            if (!ionburstPrivate.CheckIonburstAPI().Result)
                throw new IonFSException($"** WARNING **: Ionburst Cloud is currently unavailable!");

            return ionburstPrivate;
        }
    }
}
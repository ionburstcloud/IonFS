using System;

using Ionburst.Apps.IonFS.Model;

namespace Ionburst.Apps.IonFS.Exceptions
{
    public class IonFSChecksumException : IonFSException
    {
        public IonFSMetadata metadata;
        public string hash;

        public IonFSChecksumException(string message) : base(message)
        {
        }

        public IonFSChecksumException(IonFSMetadata metadata, string hash)
        {
            this.metadata = metadata;
            this.hash = hash;
        }

        public IonFSChecksumException(string message, IonFSException innerException) : base(message, innerException)
        {
        }

        public IonFSChecksumException()
        {
        }

        public IonFSChecksumException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Ionburst.Apps.IonFS.Exceptions
{
    class IonFSChecksumException : IonFSException
    {
        public IonFSMetadata metadata;

        public IonFSChecksumException(string message) : base(message)
        {
        }

        public IonFSChecksumException(IonFSMetadata metadata)
        {
            this.metadata = metadata;
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

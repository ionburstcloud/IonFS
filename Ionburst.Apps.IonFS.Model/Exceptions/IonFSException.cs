using System;
using System.Runtime.Serialization;

using Ionburst.Apps.IonFS.Model;

namespace Ionburst.Apps.IonFS.Exceptions
{
    public class IonFSException : Exception
    {
        protected string FSPrefix { get; }  = "ion://";

        public IonFSException()
        {
        }

        public IonFSException(string message) : base(message)
        {
        }

        public IonFSException(string message, Exception innerException) : base(message, innerException)
        {
        }
        public IonFSException(string message, IonFSException innerException) : base(message, innerException)
        {
        }
        public IonFSException(IonFSObject fso, Exception innerException) : base(fso.FullName, innerException)
        {
        }

        public IonFSException(string message, IonFSObject fso, Exception innerException) : base(message, innerException)
        {
        }

        protected IonFSException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

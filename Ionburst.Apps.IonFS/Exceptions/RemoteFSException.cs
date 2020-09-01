using System;

namespace Ionburst.Apps.IonFS.Exceptions
{
    public class RemoteFSException : IonFSException
    {
        private const string fsPrefix = "ion://";
        public String FolderName { get; }
        public String Prefix { get; }

        public RemoteFSException()
        {
        }

        public RemoteFSException(string remoteFolder) 
            : base($"The remote folder '{remoteFolder}' must include the prefix {fsPrefix} and be a folder")
        {
            if (remoteFolder != null)
            {
                FolderName = remoteFolder;
                Prefix = remoteFolder.StartsWith(fsPrefix, StringComparison.Ordinal) ? fsPrefix : "";
            }
        }

        public RemoteFSException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}

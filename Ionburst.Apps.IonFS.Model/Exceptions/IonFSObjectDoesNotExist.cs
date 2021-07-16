using System;

using Ionburst.Apps.IonFS.Model;

namespace Ionburst.Apps.IonFS.Exceptions
{
    public class IonFSObjectDoesNotExist : IonFSException
    {
        IonFSObject fsObject;

        public IonFSObjectDoesNotExist(IonFSObject fso)
            : base($"Exception: {fso.FullName} does not exist")
        {
            fsObject = fso;
        }

        public IonFSObjectDoesNotExist(IonFSObject fso, Exception innerException) 
            : base($"Exception: {fso.FullName} does not exist", innerException)
        {
        }

        public IonFSObjectDoesNotExist(string message, IonFSObject fso, Exception innerException)
            : base($"Exception: {message}", innerException)
        {
            fsObject = fso;
        }
    }
}

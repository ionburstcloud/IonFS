// Copyright Ionburst Limited 2020
using System;

using Ionburst.Apps.IonFS.Exceptions;

namespace Ionburst.Apps.IonFS
{
    public class IonFSObject
    {
        public string FS { get; set; }
        public string Repository { get; set; }
        public string Name { get; set; } 
        public string Path { get; set; }
        public DateTime LastModified { get; set; }
        public Boolean IsFolder { get; set; }
        public Boolean IsRoot { get; set; }
        public Boolean IsRemote { get; set; }
        public Boolean HasRepository { get; set; }

        public string FullName { get { return $"{Path}{Name}"; } }
        public string FullFSName 
        { 
            get 
            { 
                if (HasRepository)
                    return $"{FS}{Repository}/{Path}{Name}";
                else
                    return $"{FS}{Path}{Name}"; 
            } 
        }

        public override string ToString()
        {
            return $"{FullFSName}";
        }

        public static IonFSObject FromLocalFile(string fullName)
        {
            if (fullName == null)
                throw new ArgumentNullException(nameof(fullName), "fullName cannot be Null.");

            string path = "";
            string filename = "";
            int indx = fullName.LastIndexOf(@"/");
            if (indx >= 0)
            {
                path = fullName.Substring(0, fullName.LastIndexOf(@"/") + 1);
                filename = fullName.Replace(path, "");
            }
            else
                filename = fullName;

            return new IonFSObject { FS="", Name = filename, Path = path, IsFolder = false, IsRemote = false, IsRoot = false, HasRepository = false };
        }
    }
}

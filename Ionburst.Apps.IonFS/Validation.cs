// Copyright Ionburst Limited 2020
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ionburst.Apps.IonFS
{
    public static class Validation
    {
        public static bool IsTrue(bool expression)
        {
            return expression;
        }

        public static bool IsFolder(string folder)
        {
            FileAttributes attributes = File.GetAttributes(folder);
            return attributes.HasFlag(FileAttributes.Directory);
        }

        public async static Task<bool> IsRemoteFolder(IIonFSMetadata metadata, IonFSObject folder)
        {
            if (metadata == null)
                throw new Exception("Invalid Metadata provider");

            return await metadata.Exists(folder);
        }
    }
}

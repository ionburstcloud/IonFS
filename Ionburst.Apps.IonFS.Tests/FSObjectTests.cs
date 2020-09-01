using Xunit;

namespace Ionburst.Apps.IonFS.Tests
{
    public class FSObjectTests
    {
        private IonburstFS fs;

        public FSObjectTests()
        {
            fs = new IonburstFS();
        }

        [Theory]
        [InlineData("ion://folder1/folder2/file")]
        [InlineData("ion://folder/file")]
        [InlineData("ion://folder/")]
        [InlineData("ion://folder1/folder2/")]
        [InlineData("ion://file")]
        [InlineData("ion://")]
        [InlineData("folder1/folder2/file")]
        [InlineData("folder/file")]
        [InlineData("folder/")]
        [InlineData("file")]
        public void FSObjectFromString(string value)
        {
            IonFSObject fso = fs.FromString(value);
            Assert.NotNull(fso);
        }

        [Fact]
        public void FSObjectRemoteFolderRoot()
        {
            IonFSObject fso = fs.FromRemoteFolder("ion://");
            Assert.True(fso.IsFolder);
            Assert.True(fso.IsRemote);
            Assert.True(fso.IsRoot);
            Assert.Equal("ion://", fso.FS);
            Assert.Equal("", fso.Path);
            Assert.Equal("", fso.Name);
            Assert.Equal("ion://", fso.FullFSName);
            Assert.Equal("", fso.FullName);
        }

        [Fact]
        public void FSObjectRemoteFolderEmpty()
        {
            IonFSObject fso = fs.FromRemoteFolder("");
            Assert.True(fso.IsFolder);
            Assert.True(fso.IsRemote);
            Assert.True(fso.IsRoot);
            Assert.Equal("ion://", fso.FS);
            Assert.Equal("", fso.Path);
            Assert.Equal("", fso.Name);
            Assert.Equal("ion://", fso.FullFSName);
            Assert.Equal("", fso.FullName);
        }

        [Fact]
        public void FSObjectRemoteFolderStringWithSuffix()
        {
            IonFSObject fso = fs.FromRemoteFolder("ion://folder/");
            Assert.True(fso.IsFolder);
            Assert.True(fso.IsRemote);
            Assert.False(fso.IsRoot);
            Assert.Equal("ion://", fso.FS);
            Assert.Equal("folder/", fso.Path);
            Assert.Equal("", fso.Name);
            Assert.Equal("ion://folder/", fso.FullFSName);
            Assert.Equal("folder/", fso.FullName);
        }

        [Fact]
        public void FSObjectRemoteFolderStringWithNoSuffix()
        {
            IonFSObject fso = fs.FromRemoteFolder("ion://folder");
            Assert.True(fso.IsFolder);
            Assert.True(fso.IsRemote);
            Assert.False(fso.IsRoot);
            Assert.Equal("ion://", fso.FS);
            Assert.Equal("folder/", fso.Path);
            Assert.Equal("", fso.Name);
            Assert.Equal("ion://folder/", fso.FullFSName);
            Assert.Equal("folder/", fso.FullName);
        }

        [Fact]
        public void FSObjectRemoteFolderStringWithMultipleFolders()
        {
            IonFSObject fso = fs.FromRemoteFolder("ion://folder1/folder2/");
            Assert.True(fso.IsFolder);
            Assert.True(fso.IsRemote);
            Assert.False(fso.IsRoot);
            Assert.Equal("ion://", fso.FS);
            Assert.Equal("folder1/folder2/", fso.Path);
            Assert.Equal("", fso.Name);
            Assert.Equal("ion://folder1/folder2/", fso.FullFSName);
            Assert.Equal("folder1/folder2/", fso.FullName);
        }

        [Fact]
        public void FSObjectRemoteFileStringWithFolder()
        {
            IonFSObject fso = fs.FromRemoteFile("ion://folder/file");
            Assert.False(fso.IsFolder);
            Assert.True(fso.IsRemote);
            Assert.False(fso.IsRoot);
            Assert.Equal("ion://", fso.FS);
            Assert.Equal("folder/", fso.Path);
            Assert.Equal("file", fso.Name);
            Assert.Equal("ion://folder/file", fso.FullFSName);
            Assert.Equal("folder/file", fso.FullName);
        }

        [Fact]
        public void FSObjectRemoteFileStringWithMultipleFolders()
        {
            IonFSObject fso = fs.FromRemoteFile("ion://folder1/folder2/file");
            Assert.False(fso.IsFolder);
            Assert.True(fso.IsRemote);
            Assert.False(fso.IsRoot);
            Assert.Equal("ion://", fso.FS);
            Assert.Equal("folder1/folder2/", fso.Path);
            Assert.Equal("file", fso.Name);
            Assert.Equal("ion://folder1/folder2/file", fso.FullFSName);
            Assert.Equal("folder1/folder2/file", fso.FullName);
        }

        [Fact]
        public void FSObjectRemoteFileStringInRoot()
        {
            IonFSObject fso = fs.FromRemoteFile("ion://file");
            Assert.False(fso.IsFolder);
            Assert.True(fso.IsRemote);
            Assert.True(fso.IsRoot);
            Assert.Equal("ion://", fso.FS);
            Assert.Equal("", fso.Path);
            Assert.Equal("file", fso.Name);
            Assert.Equal("ion://file", fso.FullFSName);
            Assert.Equal("file", fso.FullName);
        }

        [Fact]
        public void FSObjectLocalFileWithoutFolder()
        {
            IonFSObject fso = IonFSObject.FromLocalFile("file");
            Assert.False(fso.IsFolder);
            Assert.False(fso.IsRemote);
            Assert.False(fso.IsRoot);
            Assert.Equal("", fso.FS);
            Assert.Equal("", fso.Path);
            Assert.Equal("file", fso.Name);
            Assert.Equal("file", fso.FullFSName);
            Assert.Equal("file", fso.FullName);
        }

        [Fact]
        public void FSObjectLocalFileWitFolder()
        {
            IonFSObject fso = IonFSObject.FromLocalFile("folder/file");
            Assert.False(fso.IsFolder);
            Assert.False(fso.IsRemote);
            Assert.False(fso.IsRoot);
            Assert.Equal("", fso.FS);
            Assert.Equal("folder/", fso.Path);
            Assert.Equal("file", fso.Name);
            Assert.Equal("folder/file", fso.FullFSName);
            Assert.Equal("folder/file", fso.FullName);
        }
    }
}

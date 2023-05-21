using System;
using Xunit;
using Xunit.Abstractions;
using Ionburst.Apps.IonFS.Model;
using Ionburst.Apps.IonFS.Repo.S3;
using Ionburst.Apps.IonFS.Repo.Mongo;
using Ionburst.Apps.IonFS.Repo.LocalFS;

namespace Ionburst.Apps.IonFS.Tests
{
    public class FSIonburstTests
    {
        private readonly ITestOutputHelper output;

        public FSIonburstTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void PutGetDelTest_S3()
        {
            IonburstFS ionburstFS = new IonburstFS();
            Assert.NotNull(ionburstFS);

            output.WriteLine("Making remote folder");
            IonFSObject folder = ionburstFS.FromRemoteFolder("ion://first-S3/atestfolder/");
            ionburstFS.MakeDirAsync(folder).Wait();

            IIonFSMetadata metadata = new MetadataS3(ionburstFS.GetCurrentDataStore(),
                ionburstFS.GetCurrentRepositoryName(), "Data");
            Assert.True(metadata.Exists(folder).Result);

            IonFSObject fsoPutFrom = IonFSObject.FromLocalFile("li.jpg");
            IonFSObject fsoPutTo = ionburstFS.FromRemoteFile("ion://first-S3/atestfolder/li.jpg");
            output.WriteLine("PUT");
            ionburstFS.PutAsync(fsoPutFrom, fsoPutTo).Wait();
            Assert.True(metadata.Exists(fsoPutTo).Result);

            IonFSObject fsoGetFrom = ionburstFS.FromRemoteFile("ion://first-S3/atestfolder/li.jpg");
            IonFSObject fsoGetTo = IonFSObject.FromLocalFile("li-g.jpg");
            output.WriteLine("GET");
            ionburstFS.GetAsync(fsoGetFrom, fsoGetTo).Wait();

            IonFSObject fsoDelFrom = ionburstFS.FromRemoteFile("ion://first-S3/atestfolder/li.jpg");
            output.WriteLine("DEL");
            ionburstFS.DelAsync(fsoDelFrom).Wait();
            Assert.False(metadata.Exists(fsoDelFrom).Result);

            output.WriteLine("Removing remote folder");
            ionburstFS.DeleteDirAsync(folder).Wait();
            Assert.False(metadata.Exists(folder).Result);
        }

        [Fact]
        public void GetTest()
        {
            IonburstFS ionburstFS = new IonburstFS();
            Assert.NotNull(ionburstFS);

            IonFSObject fsoGetFrom = ionburstFS.FromRemoteFile("ion://li-2.jpg");
            IonFSObject fsoGetTo = IonFSObject.FromLocalFile("li-g2.jpg");
            output.WriteLine("GET");
            ionburstFS.GetAsync(fsoGetFrom, fsoGetTo).Wait();
        }

        [Fact]
        public void GetTest2()
        {
            IonburstFS ionburstFS = new IonburstFS();
            Assert.NotNull(ionburstFS);

            IonFSObject fsoGetFrom = ionburstFS.FromRemoteFile("ion://atestfolder/li.jpg");
            IonFSObject fsoGetTo = IonFSObject.FromLocalFile("li-g.jpg");
            output.WriteLine("GET");
            ionburstFS.GetAsync(fsoGetFrom, fsoGetTo).Wait();
        }

        [Fact]
        public void PutGetDelTest_mongo()
        {
            IonburstFS ionburstFS = new IonburstFS();
            Assert.NotNull(ionburstFS);

            output.WriteLine("Making remote folder");
            IonFSObject folder = ionburstFS.FromRemoteFolder("ion://iain-mongo/atestfolder/");
            ionburstFS.MakeDirAsync(folder).Wait();

            IIonFSMetadata metadata = new MetadataMongoDB(ionburstFS.GetCurrentDataStore(),
                ionburstFS.GetCurrentRepositoryName(), "Data");
            Assert.True(metadata.Exists(folder).Result);

            IonFSObject fsoPutFrom = IonFSObject.FromLocalFile("li.jpg");
            IonFSObject fsoPutTo = ionburstFS.FromRemoteFile("ion://iain-mongo/atestfolder/li.jpg");
            output.WriteLine("PUT");
            ionburstFS.PutAsync(fsoPutFrom, fsoPutTo).Wait();
            Assert.True(metadata.Exists(fsoPutTo).Result);

            IonFSObject fsoGetFrom = ionburstFS.FromRemoteFile("ion://iain-mongo/atestfolder/li.jpg");
            IonFSObject fsoGetTo = IonFSObject.FromLocalFile("li-g.jpg");
            output.WriteLine("GET");
            ionburstFS.GetAsync(fsoGetFrom, fsoGetTo).Wait();

            IonFSObject fsoDelFrom = ionburstFS.FromRemoteFile("ion://iain-mongo/atestfolder/li.jpg");
            output.WriteLine("DEL");
            ionburstFS.DelAsync(fsoDelFrom).Wait();
            Assert.False(metadata.Exists(fsoDelFrom).Result);

            output.WriteLine("Removing remote folder");
            ionburstFS.DeleteDirAsync(folder).Wait();
            Assert.False(metadata.Exists(folder).Result);
        }

        [Fact]
        public void PutGetDelTest_LocalFS()
        {
            IonburstFS ionburstFS = new IonburstFS();
            Assert.NotNull(ionburstFS);

            output.WriteLine("Making remote folder");
            IonFSObject folder = ionburstFS.FromRemoteFolder("ion://local/atestfolder/");
            ionburstFS.MakeDirAsync(folder).Wait();

            IIonFSMetadata metadata = new MetadataLocalFS(ionburstFS.GetCurrentDataStore(),
                ionburstFS.GetCurrentRepositoryName(), "Data");
            Assert.True(metadata.Exists(folder).Result);

            IonFSObject fsoPutFrom = IonFSObject.FromLocalFile("li.jpg");
            IonFSObject fsoPutTo = ionburstFS.FromRemoteFile("ion://local/atestfolder/li.jpg");
            output.WriteLine("PUT");
            ionburstFS.PutAsync(fsoPutFrom, fsoPutTo).Wait();
            Assert.True(metadata.Exists(fsoPutTo).Result);

            IonFSObject fsoGetFrom = ionburstFS.FromRemoteFile("ion://local/atestfolder/li.jpg");
            IonFSObject fsoGetTo = IonFSObject.FromLocalFile("li-g.jpg");
            output.WriteLine("GET");
            ionburstFS.GetAsync(fsoGetFrom, fsoGetTo).Wait();

            IonFSObject fsoDelFrom = ionburstFS.FromRemoteFile("ion://local/atestfolder/li.jpg");
            output.WriteLine("DEL");
            ionburstFS.DelAsync(fsoDelFrom).Wait();
            Assert.False(metadata.Exists(fsoDelFrom).Result);

            output.WriteLine("Removing remote folder");
            ionburstFS.DeleteDirAsync(folder).Wait();
            Assert.False(metadata.Exists(folder).Result);
        }
    }
}
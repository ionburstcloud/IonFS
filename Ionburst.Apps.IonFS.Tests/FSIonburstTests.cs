using System;
using Xunit;
using Xunit.Abstractions;

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
        public void PutGetDelTest()
        {
            IonburstFS ionburstFS = new IonburstFS();
            Assert.NotNull(ionburstFS);

            output.WriteLine("Making remote folder");
            IonFSObject folder = ionburstFS.FromRemoteFolder("ion://atestfolder/");
            ionburstFS.MakeDirAsync(folder).Wait();

            IIonFSMetadata metadata = new MetadataS3(ionburstFS.GetCurrentDataStore(), ionburstFS.GetCurrentRepositoryName());
            Assert.True(metadata.Exists(folder).Result);

            IonFSObject fsoPutFrom = IonFSObject.FromLocalFile("li.jpg");
            IonFSObject fsoPutTo = ionburstFS.FromRemoteFile("ion://atestfolder/li.jpg");
            output.WriteLine("PUT");
            ionburstFS.PutAsync(fsoPutFrom, fsoPutTo).Wait();
            Assert.True(metadata.Exists(fsoPutTo).Result);

            IonFSObject fsoGetFrom = ionburstFS.FromRemoteFile("ion://atestfolder/li.jpg");
            IonFSObject fsoGetTo = IonFSObject.FromLocalFile("li-g.jpg");
            output.WriteLine("GET");
            ionburstFS.GetAsync(fsoGetFrom, fsoGetTo).Wait();

            IonFSObject fsoDelFrom = ionburstFS.FromRemoteFile("ion://atestfolder/li.jpg");
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

            IIonFSMetadata metadata = new MetadataMongoDB(ionburstFS.GetCurrentDataStore(), ionburstFS.GetCurrentRepositoryName());
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
    }
}

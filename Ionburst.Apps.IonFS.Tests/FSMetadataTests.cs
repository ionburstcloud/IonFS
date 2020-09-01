using Xunit;
using Xunit.Abstractions;

namespace Ionburst.Apps.IonFS.Tests
{
    public class FSMetadataTests
    {
        private readonly ITestOutputHelper output;

        public FSMetadataTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void CreateIonFS()
        {
            IonburstFS ionburstFS = new IonburstFS();
            Assert.NotNull(ionburstFS);
        }

        [Fact]
        public void CreateAndDeleteRemoteFolder()
        {
            IonburstFS ionburstFS = new IonburstFS();
            Assert.NotNull(ionburstFS);

            IonFSObject folder = ionburstFS.FromRemoteFolder("ion://atestfolder/");
            ionburstFS.MakeDirAsync(folder).Wait();

            IIonFSMetadata metadata = new MetadataS3(ionburstFS.GetCurrentDataStore(), ionburstFS.GetCurrentRepositoryName());
            Assert.True(metadata.Exists(folder).Result);

            ionburstFS.DeleteDirAsync(folder).Wait();
            Assert.False(metadata.Exists(folder).Result);
        }

        [Fact]
        public void CreateAndDeleteRemoteFolder_mongo()
        {
            IonburstFS ionburstFS = new IonburstFS();
            Assert.NotNull(ionburstFS);

            IonFSObject folder = ionburstFS.FromRemoteFolder("ion://atestfolder/");
            ionburstFS.MakeDirAsync(folder).Wait();

            IIonFSMetadata metadata = new MetadataMongoDB(ionburstFS.GetCurrentDataStore(), ionburstFS.GetCurrentRepositoryName());
            Assert.True(metadata.Exists(folder).Result);

            ionburstFS.DeleteDirAsync(folder).Wait();
            Assert.False(metadata.Exists(folder).Result);
        }
    }
}

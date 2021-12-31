using bitcoin_data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace BitcoinData.Tests
{
    // alternatively, you can use IClassFixture to avoid init/dispose for every test method. 
    // https://stackoverflow.com/a/16590641/947889

    public class TestEndToEnd : BaseTests
    {
        public TestEndToEnd() :
            base(new List<int> { 120460, 120461, 120462, 120463 })
        { }

        [Theory]
        [InlineData(120460, 120464)]
        public void EndToEnd(int fromHeight, int toHeight)
        {
            // Arrange
            var orchestrator = new Orchestrator(TempExeDir, Client);
            var expOutputCSVCount = 2 * (toHeight - fromHeight) + 1;
            var expDataDir = GetExpOutputDir(fromHeight, toHeight);

            // Act
            orchestrator.RunAsync(fromHeight, toHeight).Wait();

            // Assert
            var dir = new DirectoryInfo(TempExeDir);

            Assert.Equal(expOutputCSVCount, dir.GetFiles("*.csv").Length);
            Assert.Single(dir.GetFiles("*.json"));

            for (int i = fromHeight; i < toHeight; i++)
            {
                var checksums = GetChecksums(expDataDir);
                var blockNodesAndEdges =
                    dir.GetFiles("*.csv").Where(
                        x => x.Name.StartsWith(i.ToString()))
                    .ToList();

                foreach (var csvFile in blockNodesAndEdges)
                {
                    var expectedFileMD5 = checksums[Path.GetFileName(csvFile.FullName)];
                    var producedFileMD5 = Getchecksum(csvFile.FullName);
                    Assert.Equal(expectedFileMD5, producedFileMD5);
                }
            }
        }
    }
}

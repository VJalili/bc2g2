namespace BitcoinData.Tests
{
    public class TestBlock
    {
        public int Height { get; }
        public string Hash { get; }
        public string JsonFilename { get; }

        public TestBlock(int height, string hash, string jsonFilename)
        {
            Height = height;
            Hash = hash;
            JsonFilename = jsonFilename;
        }
    }
}

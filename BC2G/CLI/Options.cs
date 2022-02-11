namespace BC2G.CLI
{
    public class Options
    {
        public int FromInclusive { get; set; }
        public int ToExclusive { get; set; }
        public int LastProcessedBlock
        {
            get { return _lastProcessedBlock == -1 ? FromInclusive - 1 : _lastProcessedBlock; }
            set { _lastProcessedBlock = value; }
        }
        private int _lastProcessedBlock = -1;
        
        public string OutputDir { get; set; } = string.Empty;
        public string AddressIdMappingFilename { set; get; } = string.Empty;
        public bool CreatePerBlockFiles { get; set; }
        public int MaxConcurrentBlocks { get; set; }
    }
}

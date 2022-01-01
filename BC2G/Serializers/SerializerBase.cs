using BC2G.Graph;

namespace BC2G.Serializers
{
    public abstract class SerializerBase
    {
        public AddressToIdMapper Mapper { set; get; }

        public SerializerBase()
        {
            Mapper = new AddressToIdMapper();
        }

        public SerializerBase(string addressToIdMappingsFilename)
        {
            Mapper = new AddressToIdMapper(addressToIdMappingsFilename);
        }

        public abstract void Serialize(BlockGraph g, string baseFilename);

        public abstract BlockGraph Deserialize(string path, int blockHeight);
    }
}

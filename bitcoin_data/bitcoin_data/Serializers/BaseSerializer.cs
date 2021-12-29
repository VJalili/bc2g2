using bitcoin_data.Graph;
using bitcoin_data.Model;

namespace bitcoin_data.Serializers
{
    internal abstract class BaseSerializer
    {
        public AddressToIdMapper Mapper { set; get; }

        public BaseSerializer(string addressToIdMappingsFilename)
        {
            Mapper = new AddressToIdMapper(addressToIdMappingsFilename);
        }

        public abstract void Serialize(BlockGraph g, string baseFilename);
    }
}

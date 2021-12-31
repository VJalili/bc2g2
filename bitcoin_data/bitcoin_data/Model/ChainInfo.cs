using System.Text.Json.Serialization;

namespace bitcoin_data.Model
{
    public class ChainInfo
    {
        [JsonPropertyName("chain")]
        public string Chain { get; set; } = string.Empty;

        [JsonPropertyName("blocks")]
        public int Blocks { get; set; }
    }
}

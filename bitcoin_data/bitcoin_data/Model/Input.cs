using System.Text.Json.Serialization;

namespace bitcoin_data.Model
{
    public class Input
    {
        [JsonPropertyName("coinbase")]
        public string Coinbase { get; set; } = string.Empty;

        [JsonPropertyName("txid")]
        public string? TxId { get; set; }

        [JsonPropertyName("vout")]
        public int? OutputIndex { get; set; }

        [JsonPropertyName("scriptSig")]
        public ScriptSig? ScriptSig { get; set; }

        [JsonPropertyName("txinwitness")]
        public List<string>? TxInputWitness { get; set; }

        [JsonPropertyName("sequence")]
        public long Sequence { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace bitcoin_data.Model
{
    internal class Input
    {
        [JsonPropertyName("coinbase")]
        public string Coinbase { get; set; } = string.Empty;

        [JsonPropertyName("txid")]
        public string? TransactionId { get; set; }

        [JsonPropertyName("vout")]
        public int? OutputIndex { get; set; }

        [JsonPropertyName("scriptSig")]
        public ScriptSig? ScriptSig { get; set; }

        [JsonPropertyName("txinwitness")]
        public List<string>? TransactionInputWitness { get; set; }

        [JsonPropertyName("sequence")]
        public long Sequence { get; set; }
    }
}

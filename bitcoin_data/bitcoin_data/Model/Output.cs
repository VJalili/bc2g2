using System.Text.Json.Serialization;

// In bitcoin instead of "sending money to an address", 
// you lock a value behind a script, who ever can satisfy 
// that script, can redeem that money.
// Locking mechanism is a generic Forth-like, stack-based script
// that is processed from left to right, not Turing-complete with no loops. 
// A transaction is valid if nothing in the combined script triggers
// failure and the top stack item is True (non-zero) when the script exits.
// See: https://en.bitcoin.it/wiki/Script

namespace bitcoin_data.Model
{
    internal class Output
    {
        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("n")]
        public int Index { get; set; }

        public TransactionType PaymentType { get; set; }

        [JsonPropertyName("scriptPubKey")]
        public ScriptPubKey? ScriptPubKey { get; set; }

        public string GetAddress()
        {
            if (ScriptPubKey != null)
                return ScriptPubKey.GetAddress();
            throw new NotImplementedException();
        }

        public ScriptType GetScriptType()
        {
            if (ScriptPubKey != null)
                return ScriptPubKey.ScriptType;
            else
                throw new NotImplementedException();
        }
    }
}

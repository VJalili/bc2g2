﻿using System.Text.Json.Serialization;

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
    public class Output
    {
        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("n")]
        public int Index { get; set; }

        public TransactionType PaymentType { get; set; }

        [JsonPropertyName("scriptPubKey")]
        public ScriptPubKey? ScriptPubKey { get; set; }

        public bool TryGetAddress(out string address)
        {
            if (ScriptPubKey != null)
                address = ScriptPubKey.GetAddress();
            else
                throw new NotImplementedException();

            return !string.IsNullOrEmpty(address);
        }

        public ScriptType GetScriptType()
        {
            if (ScriptPubKey != null)
                return ScriptPubKey.ScriptType;
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// Some outputs in a Bitcoin transaction do not
        /// transfer any bitcoin (i.e., "value": 0.00000000).
        /// This property is False, if this output is of
        /// such type of outputs, otherwise, it is True.
        /// </summary>
        public bool IsValueTransfer
        {
            get
            {
                if (_isValueTransfer == null)
                {
                    /*
                    if (output.GetScriptType() is
                        ScriptType.Unknown or ScriptType.NullData)
                        continue;*/

                    // Ideally the above should be sufficient, but
                    // for dev purposes, we use the following.
                    _isValueTransfer = GetScriptType() switch
                    {
                        ScriptType.NullData => false,
                        ScriptType.Unknown => throw new NotImplementedException(),
                        _ => true, // all other cases.
                    };
                }

                return (bool)_isValueTransfer;
            }
        }
        private bool? _isValueTransfer = null;
    }
}

﻿using System.Text.Json.Serialization;

namespace bitcoin_data.Model
{
    public class ScriptSig
    {
        [JsonPropertyName("asm")]
        public string Asm { get; set; } = string.Empty;

        [JsonPropertyName("hex")]
        public string Hex { get; set; } = string.Empty;
    }
}

﻿using BC2G.Utilities;

namespace BC2G.Blockchains.Bitcoin.Model;

public class Output : IBase64Serializable
{
    [JsonPropertyName("value")]
    public double ValueBTC
    {
        get { return _valueBTC; }
        set
        {
            _valueBTC = value;
            Value = Helpers.BTC2Satoshi(value);
        }
    }
    private double _valueBTC;

    public long Value { get; private set; }

    [JsonPropertyName("n")]
    public int Index { set; get; }

    [JsonPropertyName("scriptPubKey")]
    public ScriptPubKey? ScriptPubKey { set; get; }

    public Output() { }
    public Output(long value, ScriptPubKey? scriptPubKey)
    {
        Value = value;
        ScriptPubKey = scriptPubKey;
    }

    public bool TryGetAddress(out string? address)
    {
        if (ScriptPubKey != null)
            address = ScriptPubKey.GetAddress();
        else
            // TODO: fixme.
            throw new NotImplementedException("Get address when script pub key is not defined is not implemented");

        if (string.IsNullOrEmpty(address))
        {
            address = null;
            //address = $"{AutoGeneratedPrefix}{Utilities.GetSHA256(ToBase64String())}";
            return false;
        }

        return true;
        //return !string.IsNullOrEmpty(address);
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
                if (Value == 0)
                {
                    _isValueTransfer = false;
                }
                else
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
            }

            return (bool)_isValueTransfer;
        }
    }
    private bool? _isValueTransfer = null;

    public string ToBase64String()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(Value);
            writer.Write(Index);
            if (ScriptPubKey != null)
                writer.Write(ScriptPubKey.ToBase64String());
        }
        return Convert.ToBase64String(stream.ToArray());
    }
}

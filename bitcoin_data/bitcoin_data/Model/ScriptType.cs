using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitcoin_data.Model
{
    // DO NOT RENAME THESE, EXCEPT FOR UNKNOW, SINCE 
    // ALL THE KEYS CORRESPOND TO THE `type` ATTRIBUTE
    // IN BITCOIN TRANSACTIONS.

    internal enum ScriptType
    {
        PubKeyHash,

        /// <summary>
        /// Pay-to-script-hash. 
        /// <see cref="https://bitcoin.stackexchange.com/a/9703/129532"/>
        /// </summary>
        ScriptHash,

        /// <summary>
        /// A script with this is often used to 
        /// encode data in the blockchain, where 
        /// a small amount of arbitrary data is 
        /// added to the block chain in exchange 
        /// for paying a transaction fee.
        /// <seealso cref="https://developer.bitcoin.org/devguide/transactions.html#null-data"/>
        /// </summary>
        NullData,

        Unknown
    }
}

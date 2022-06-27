using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    public enum PropName
    {
        Height,
        BlockMedianTime,
        BlockConfirmations,
        BlockDifficulty,
        BlockTxCount,
        BlockSize,
        BlockStrippedSize,
        BlockWeight
    }

    public enum FieldType
    {
        String, Int, Float
    }
}

using BC2G.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal class BlockBulkLoadMapper : ModelMapper<Block>
    {
        public class Neo4jModel : Neo4jModelBase
        {
            public const string label = "Block";
        }

        /// Note that the ordre of the items in this array should 
        /// match those returned from the `ToCsv()` method.. 
        private static readonly PropName[] _properties = new PropName[]
        {
            PropName.Height,
            PropName.BlockMedianTime,
            PropName.BlockConfirmations,
            PropName.BlockDifficulty,
            PropName.BlockTxCount,
            PropName.BlockSize,
            PropName.BlockStrippedSize,
            PropName.BlockWeight
        };

        public BlockBulkLoadMapper(
            string importPrefix,
            string importDirectory,
            string filename = "tmpBulkImportBlocks.csv") :
            base(importPrefix, importDirectory, filename)
        { }

        public override string GetCsvHeader()
        {
            return string.Join(
                csvDelimiter,
                from x in _properties select Properties[x].CsvHeader);
        }

        public override string ToCsv(Block block)
        {
            /// Note that the ordre of the items in this array should 
            /// match those in the `_properties`. 
            return string.Join(csvDelimiter, new string[]
            {
                block.Height.ToString(),
                block.MedianTime.ToString(),
                block.Confirmations.ToString(),
                block.Difficulty.ToString(),
                block.TransactionsCount.ToString(),
                block.Size.ToString(),
                block.StrippedSize.ToString(),
                block.Weight.ToString()
            });
        }

        protected override string ComposeCypherQuery(string filename)
        {
            var builder = new StringBuilder();
            builder.Append(
                $"LOAD CSV WITH HEADERS FROM '{filename}' AS {Property.lineVarName} " +
                $"FIELDTERMINATOR '{csvDelimiter}' " +
                $"MERGE (b: {Neo4jModel.label} {{" +
                $"{Properties[PropName.Height].GetLoadExp(":")}}})" +
                $"ON CREATE SET ");

            string comma = "";
            foreach (var p in _properties) if (p != PropName.Height)
                {
                    builder.Append($"{comma}b.{Properties[p].GetLoadExp()}");
                    comma = ", ";
                }

            builder.Append(
                " ON MATCH SET " +
                $"b.{Properties[PropName.BlockConfirmations].GetLoadExp()}");

            return builder.ToString();
        }
    }
}

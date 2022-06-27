using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal class Property
    {
        public const string lineVarName = "line";
        public string Name { get; }
        public string CsvHeader { get; }
        public string CsvToModelSnippet { get; }

        public Property(string name, string csvHeader, FieldType type = FieldType.String)
        {
            Name = name;
            CsvHeader = csvHeader;

            CsvToModelSnippet = type switch
            {
                FieldType.Int => $"{name}: toInteger({lineVarName}.{csvHeader})",
                FieldType.Float => $"{name}: toFloat({lineVarName}.{csvHeader})",
                _ => $"{name}: {lineVarName}.{csvHeader}",
            };
        }
    }
}

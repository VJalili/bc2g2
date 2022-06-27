﻿using BC2G.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal abstract class ModelMapper<T>
    {
        public const string csvDelimiter = "\t";
        public const string labelsDelimiter = ":";
        public const string lineVarName = "line";

        public static Dictionary<PropName, Property> Properties = new()
        {
            {PropName.Height, new Property("Height", "Height", FieldType.Int) }
        };


        public string Filename { get; }
        public string CypherQuery { get; }

        public class Neo4jModelBase
        {
            //public const string height = "Height";
        }

        public ModelMapper(
            string cypherImportPrefix,
            string importDirectory,
            string filename)
        {
            Filename = Path.Combine(importDirectory, filename);
            CypherQuery = ComposeCypherQuery(cypherImportPrefix + filename);
        }

        public abstract string GetCsvHeader();
        public abstract string ToCsv(T obj);
        protected abstract string ComposeCypherQuery(string filename);
    }
}

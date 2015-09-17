using System;
using System.Collections.Generic;

namespace NestedMapper
{
    public class Mapping
    {
        public List<string> NestedPath { get; set; }
        public string FlatProperty { get; set; }
        public Type PropertyType { get; set; }



        public Mapping(List<string> nestedPath, string flatProperty, Type propertyType)
        {
            NestedPath = nestedPath;
            FlatProperty = flatProperty;
            PropertyType = propertyType;

        }

        public override string ToString()
        {
            return string.Join(".", NestedPath) + " =(" + PropertyType + ") " + FlatProperty;
        }
    }
}
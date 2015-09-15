using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NestedMapper
{
    class MappingsGetter
    {
        public class Mapping
        {
            public List<string> TargetPath { get; set; }
            public string SourceProperty { get; set; }
            public Type PropertyType { get; set; }



            public Mapping(List<string> targetPath, string sourceProperty, Type propertyType)
            {
                TargetPath = targetPath;
                SourceProperty = sourceProperty;
                PropertyType = propertyType;

            }

            public override string ToString()
            {
                return string.Join(".", TargetPath) + " =(" + PropertyType + ") " + SourceProperty;
            }
        }

        public static List<Mapping> GetMappings<T>(object sampleSourceObject, MapperFactory.NamesMismatch namesMismatch,
           List<Type> assumeNullWontBeMappedToThoseTypes) where T : new()
        {
            var tree = MapperFactory.GetMappingsTree<T>(sampleSourceObject, namesMismatch, assumeNullWontBeMappedToThoseTypes);

            var mappings = new List<Mapping>();

            GetMappingsFromNode(tree, mappings, new List<string>());

            return mappings;


        }

        static void GetMappingsFromNode(Node node, List<Mapping> mappings, List<string> path)
        {
            if (node.Nodes != null)
            {
                foreach (var innerNode in node.Nodes)
                {
                    var currentPath = !string.IsNullOrEmpty(node.TargetName)
                        ? new List<string>(path) { node.TargetName }
                        : path;

                    GetMappingsFromNode(innerNode, mappings, currentPath);
                }
            }
            else
            {
                var currentPath = new List<string>(path) { node.TargetName };
                mappings.Add(new Mapping(currentPath, node.SourcePropertyName, node.TargetType));
            }
        }


    }
}
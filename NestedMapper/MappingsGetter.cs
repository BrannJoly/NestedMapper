using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NestedMapperTests")]

namespace NestedMapper
{
    internal class MappingsGetter
    {
        public static List<Mapping> GetMappings(Node tree)
        {

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
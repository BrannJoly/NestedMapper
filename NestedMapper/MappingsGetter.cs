using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NestedMapper
{
    public class MappingsGetter
    {
        private class PropertyBasicInfo
        {
            public string Name { get; set; }
            public Type Type { get; set; }

            public PropertyBasicInfo(string name, Type type)
            {
                Name = name;
                Type = type;
            }
        }



        class TreeMapperController : PropertyBasicInfo
        {
            public bool StopMapping { get; set; }

            public TreeMapperController(PropertyBasicInfo x) : base(x.Name, x.Type)
            {

            }
        }

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
        }

        private static List<PropertyBasicInfo> GetPropertyBasicInfos(object sampleSourceObject)
        {
            List<PropertyBasicInfo> props;

            if (sampleSourceObject is IDictionary<string, object>) // ie is dynamic
            {
                var byName = (IDictionary<string, object>)sampleSourceObject;

                props = byName.Select(x => new PropertyBasicInfo(x.Key, x.Value?.GetType())).ToList();

            }
            else
            {
                props =
                    sampleSourceObject.GetType()
                        .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                        .Select(prop => new PropertyBasicInfo(prop.Name, prop.PropertyType))
                        .ToList();
            }
            return props;
        }

        public static List<Mapping> GetMappings<T>(MapperFactory.NamesMismatch namesMismatch, object sampleSourceObject) where T : new()
        {
            var props = GetPropertyBasicInfos(sampleSourceObject);

            var treeMapperController = new TreeMapperController(props[0]);

            var x = TraverseObject(typeof (T), new Stack<string>(), treeMapperController, namesMismatch, true).GetEnumerator();

            var mappings = new List<Mapping>();

            foreach (var prop in props)
            {
                treeMapperController.Name = prop.Name;
                treeMapperController.Type = prop.Type;

                if (!x.MoveNext())
                {
                    throw new InvalidOperationException("too many fields in the flat object");
                }

                var foundPropertyPath = x.Current;

                mappings.Add(new Mapping(foundPropertyPath, prop.Name, prop.Type));
            }

            treeMapperController.StopMapping = true;
            x.MoveNext(); // will throw if we still have remaining fields now that StopMapping is true
            return mappings;
        }

        private static IEnumerable<List<string>> TraverseObject(Type t, Stack<string> path, TreeMapperController seekedProperty, MapperFactory.NamesMismatch namesMismatch, bool topLevel)
        {
            var props = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            if (props.Length == 0)
            {
                throw new InvalidOperationException("Type mismatch for property " + seekedProperty.Name);
            }

            foreach (var prop in props)
            {
                if (prop.GetSetMethod() == null)
                {
                    continue;
                }

                if (seekedProperty.StopMapping)
                {
                    throw new InvalidOperationException("Not enough fields in the flat object");
                }

                if (seekedProperty.Type == null || prop.PropertyType == seekedProperty.Type )
                {
                    if (prop.Name == seekedProperty.Name
                        || namesMismatch == MapperFactory.NamesMismatch.AlwaysAllow
                        || (namesMismatch == MapperFactory.NamesMismatch.AllowInNestedTypesOnly && !topLevel)
                        )
                    {
                        var currentPath = new List<string>(path) { prop.Name };
                        yield return currentPath;
                    }
                    else
                    {
                        throw new InvalidOperationException("Name mismatch for property " + prop.Name);

                    }

                }
                else
                {
                    path.Push(prop.Name);

                    foreach (var r in TraverseObject(prop.PropertyType, path, seekedProperty, namesMismatch, false))
                    {
                        yield return r;
                    }
                    path.Pop();

                }
            }
        }
    }
}
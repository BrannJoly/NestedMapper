using System;
using System.Collections.Generic;
using System.Dynamic;
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

        public class Mapping
        {
            public List<string> TargetPath { get; set; }
            public string SourceProperty { get; set; }

            public Mapping(List<string> targetPath, string sourceProperty)
            {
                TargetPath = targetPath;
                SourceProperty = sourceProperty;
            }
        }

        private static List<PropertyBasicInfo> GetPropertyBasicInfos(object sampleSourceObject)
        {
            List<PropertyBasicInfo> props;

            if (sampleSourceObject is ExpandoObject)
            {
                var byName = (IDictionary<string, object>)sampleSourceObject;

                props =
                    Dynamitey.Dynamic.GetMemberNames(sampleSourceObject)
                        .Select((propName => new PropertyBasicInfo(propName, byName[propName].GetType())))
                        .ToList();
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

            var seekedProperty = props[0];

            var x = TraverseObject(typeof (T), new Stack<string>(), seekedProperty, namesMismatch, true).GetEnumerator();

            var mappings = new List<Mapping>();

            foreach (var prop in props)
            {
                seekedProperty.Name = prop.Name;
                seekedProperty.Type = prop.Type;

                if (!x.MoveNext())
                {
                    throw new InvalidOperationException("too many fields in the flat object");
                }

                var foundPropertyPath = x.Current;

                mappings.Add(new Mapping(foundPropertyPath, prop.Name));
            }

            seekedProperty.Type = null;
            x.MoveNext(); // will throw if we still have remaining fields now that seekedProperty.Type is null
            return mappings;
        }

        private static IEnumerable<List<string>> TraverseObject(Type t, Stack<string> path, PropertyBasicInfo propertyBasicInfo, MapperFactory.NamesMismatch namesMismatch, bool topLevel)
        {
            var props = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            if (props.Length == 0)
            {
                throw new InvalidOperationException("Type mismatch for property " + propertyBasicInfo.Name);
            }

            foreach (var prop in props)
            {
                if (prop.GetSetMethod() == null)
                {
                    continue;
                }

                if (propertyBasicInfo.Type == null)
                {
                    throw new InvalidOperationException("Not enough fields in the flat object");
                }

                if (prop.PropertyType == propertyBasicInfo.Type)
                {
                    if (prop.Name == propertyBasicInfo.Name
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

                    foreach (var r in TraverseObject(prop.PropertyType, path, propertyBasicInfo, namesMismatch, false))
                    {
                        yield return r;
                    }
                    path.Pop();

                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

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



        class TreeMapperController : MappingsGetter.PropertyBasicInfo
        {
            public bool StopMapping { get; set; }

            public List<Mapping> mappings { get; } =  new List<Mapping>();

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

            public override string ToString()
            {
                return string.Join(".", TargetPath) + " =(" + PropertyType + ") " + SourceProperty;
            }
        }

        private static List<MappingsGetter.PropertyBasicInfo> GetPropertyBasicInfos(object sampleSourceObject)
        {
            List<MappingsGetter.PropertyBasicInfo> props;

            if (sampleSourceObject is IDictionary<string, object>) // ie is dynamic
            {
                var byName = (IDictionary<string, object>)sampleSourceObject;

                props = byName.Select(x => new MappingsGetter.PropertyBasicInfo(x.Key, x.Value?.GetType())).ToList();

            }
            else
            {
                props =
                    sampleSourceObject.GetType()
                        .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                        .Select(prop => new MappingsGetter.PropertyBasicInfo(prop.Name, prop.PropertyType))
                        .ToList();
            }
            return props;
        }

        static InvalidOperationException GetException(string message, List<MappingsGetter.Mapping> mappings)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(message);
            sb.Append(Environment.NewLine);

            if (mappings.Count > 0)
            {
                sb.Append("current mappings so far : ");
                sb.Append(Environment.NewLine);
            }

            foreach (var m in mappings)
            {
                sb.Append(m);
                sb.Append(Environment.NewLine);
            }

            return new InvalidOperationException(sb.ToString());
        }

        public static List<MappingsGetter.Mapping> GetMappings<T>(MapperFactory.NamesMismatch namesMismatch, object sampleSourceObject) where T : new()
        {
            var props = GetPropertyBasicInfos(sampleSourceObject);

            var treeMapperController = new MappingsGetter.TreeMapperController(props[0]);

            var x = TraverseObject(typeof (T), new Stack<string>(), treeMapperController, namesMismatch, true).GetEnumerator();

          

            foreach (var prop in props)
            {
                treeMapperController.Name = prop.Name;
                treeMapperController.Type = prop.Type;

                if (!x.MoveNext())
                {
                    throw GetException("Too many fields in the flat object",treeMapperController.mappings);
                }

                var foundPropertyPath = x.Current;

                treeMapperController.mappings.Add(new MappingsGetter.Mapping(foundPropertyPath, prop.Name, prop.Type));
            }

            treeMapperController.StopMapping = true;
            x.MoveNext(); // will throw if we still have remaining fields now that StopMapping is true
            return treeMapperController.mappings;
        }


        private static IEnumerable<List<string>> TraverseObject(Type t, Stack<string> path, MappingsGetter.TreeMapperController treeMapperController, MapperFactory.NamesMismatch namesMismatch, bool topLevel)
        {
            var props = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            if (props.Length == 0)
            {
                throw GetException("Type mismatch for property", treeMapperController.mappings);
             }

            foreach (var prop in props)
            {
                if (prop.GetSetMethod() == null)
                {
                    continue;
                }

                if (treeMapperController.StopMapping)
                {
                    throw GetException("Not enough fields in the flat object", treeMapperController.mappings);
                }

                if (treeMapperController.Type == null 
                    || prop.PropertyType == treeMapperController.Type 
                    || AvailableCastChecker.CanCast(treeMapperController.Type, prop.PropertyType)
                    )
                {
                    if (prop.Name == treeMapperController.Name
                        || namesMismatch == MapperFactory.NamesMismatch.AlwaysAllow
                        || (namesMismatch == MapperFactory.NamesMismatch.AllowInNestedTypesOnly && !topLevel)
                        )
                    {
                        var currentPath = new List<string>(path) { prop.Name };
                        yield return currentPath;
                    }
                    else
                    {
                        throw GetException("Name mismatch for property " + prop.Name, treeMapperController.mappings);

                    }

                }
                else
                {
                    path.Push(prop.Name);

                    foreach (var r in MappingsGetter.TraverseObject(prop.PropertyType, path, treeMapperController, namesMismatch, false))
                    {
                        yield return r;
                    }
                    path.Pop();

                }
            }
        }
    }
}
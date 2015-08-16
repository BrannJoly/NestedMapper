using System;
using System.Collections.Generic;
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



        class TreeMapperController : PropertyBasicInfo
        {
            public bool StopMapping { get; set; }

            public List<Mapping> Mappings { get; } =  new List<Mapping>();

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

        static InvalidOperationException GetException(string message, List<Mapping> mappings)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(message);

            if (mappings.Count > 0)
            {
                sb.AppendLine("current mappings so far : ");
            }

            foreach (var m in mappings)
            {
                sb.AppendLine(m.ToString());
            }

            return new InvalidOperationException(sb.ToString());
        }

        public static List<Mapping> GetMappings<T>(object sampleSourceObject, MapperFactory.NamesMismatch namesMismatch, IEnumerable<Type> assumeNullWontBeMappedToThoseTypes) where T : new()
        {
            var props = GetPropertyBasicInfos(sampleSourceObject);

            var treeMapperController = new TreeMapperController(props[0]);

            var x = TraverseObject(typeof (T), new Stack<string>(), treeMapperController, namesMismatch, assumeNullWontBeMappedToThoseTypes, true).GetEnumerator();

          

            foreach (var prop in props)
            {
                treeMapperController.Name = prop.Name;
                treeMapperController.Type = prop.Type;

                if (!x.MoveNext())
                {
                    throw GetException("Too many fields in the flat object",treeMapperController.Mappings);
                }

                var foundPropertyPath = x.Current;

                treeMapperController.Mappings.Add(new Mapping(foundPropertyPath, prop.Name, prop.Type));
            }

            treeMapperController.StopMapping = true;
            x.MoveNext(); // will throw if we still have remaining fields now that StopMapping is true
             return treeMapperController.Mappings;
        }

        private static bool ListContainsType(IEnumerable<Type> list, Type type)
        {
            if (list.Contains(type))
                return true;

            if (type.IsGenericType)
                return list.Contains(type.GetGenericTypeDefinition());

            return false;
        }


        private static IEnumerable<List<string>> TraverseObject(Type t, Stack<string> path, TreeMapperController treeMapperController, MapperFactory.NamesMismatch namesMismatch, IEnumerable<Type> assumeNullWontBeMappedToThoseTypes, bool topLevel)
        {
            var props = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            if (props.Length == 0)
            {
                throw GetException("Type mismatch for property", treeMapperController.Mappings);
             }

            foreach (var prop in props)
            {
                if (prop.GetSetMethod() == null)
                {
                    continue;
                }

                if (treeMapperController.StopMapping)
                {
                    throw GetException("Not enough fields in the flat object", treeMapperController.Mappings);
                }

                if ( (treeMapperController.Type == null  && !ListContainsType(assumeNullWontBeMappedToThoseTypes, prop.PropertyType) )
                    || (treeMapperController.Type != null && AvailableCastChecker.CanCast(treeMapperController.Type, prop.PropertyType))
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
                        throw GetException("Name mismatch for property " + prop.Name, treeMapperController.Mappings);

                    }

                }
                else
                {
                    path.Push(prop.Name);

                    foreach (var r in TraverseObject(prop.PropertyType, path, treeMapperController, namesMismatch, assumeNullWontBeMappedToThoseTypes, false))
                    {
                        yield return r;
                    }
                    path.Pop();

                }
            }
        }
    }
}
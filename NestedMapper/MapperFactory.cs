using System;
using System.Collections.Generic;
using System.Reflection;
using System.Dynamic;
using System.Linq;

namespace NestedMapper
{

    public class MapperFactory
    {
        class PropertyBasicInfo
        {
            public string Name { get; set; }
            public Type Type { get; set; }

            public PropertyBasicInfo(string name, Type type)
            {
                Name = name;
                Type = type;
            }
        }


        public static IMapper<T> GetMapper<T>(PropertyNameEnforcement propertyNameEnforcement, object sampleSourceObject) where T:new()
        {
            var mappingActions = new List<Action<T, dynamic>>();

           

            List<PropertyBasicInfo> props;

            if (sampleSourceObject is ExpandoObject)
            {

                var byName = (IDictionary<string, object>) sampleSourceObject;

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

            var seekedProperty = props[0];

            var x = TraverseObject(typeof(T), new Stack<string>(), seekedProperty, propertyNameEnforcement,true).GetEnumerator();

            foreach (var prop in props)
            {
                seekedProperty.Name = prop.Name;
                seekedProperty.Type = prop.Type;

                if (!x.MoveNext())
                {
                    throw new InvalidOperationException("too many fields in the flat object");
                }

                var foundPropertyPath = x.Current;

                var exp = ExpressionTreeUtils.CreateNestedSetFromDynamicProperty<T>(foundPropertyPath, prop.Name);
                mappingActions.Add(exp.Compile());
            }

            if (x.MoveNext())
            {
                throw new InvalidOperationException("Not enough fields in the flat object");
            }
            return new Mapper<T>(mappingActions);
        }


        static IEnumerable<List<string>> TraverseObject(Type t, Stack<string> path, PropertyBasicInfo propertyBasicInfo, PropertyNameEnforcement propertyNameEnforcement, bool topLevel )
        {
            var props = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            if (props.Length == 0)
            {
                throw new InvalidOperationException("Type mismatch for property " + propertyBasicInfo.Name);
            }

            foreach (var prop in props)
            {
                if (prop.PropertyType == propertyBasicInfo.Type)
                {
                    if (prop.Name == propertyBasicInfo.Name
                        || propertyNameEnforcement == PropertyNameEnforcement.Never
                        || (propertyNameEnforcement == PropertyNameEnforcement.InNestedTypesOnly && !topLevel)
                        )
                    {
                        var currentPath = new List<string>(path) {prop.Name};
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
                    foreach (
                        var r in TraverseObject(prop.PropertyType, path, propertyBasicInfo, propertyNameEnforcement, false)
                        )
                    {
                        yield return r;
                    }
                    path.Pop();

                }
            }
        }



        public enum PropertyNameEnforcement
        {
            Never,
            InNestedTypesOnly,
            Always
        }




    }
}

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace NestedMapper
{

    public class MapperFactory
    {
        class SeekedProperty
        {
            public string Name { get; set; }
            public Type Type { get; set; }
        }


        public static IMapper<T> GetMapper<T>(PropertyNameEnforcement propertyNameEnforcement, object sampleSourceObject) where T:new()
        {
            var mappingActions = new List<Action<T, dynamic>>();

            var seekedProperty = new SeekedProperty();

            var x = TraverseObject(typeof(T), new Stack<string>(), seekedProperty).GetEnumerator();


            foreach (var prop in sampleSourceObject.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                seekedProperty.Name = prop.Name;
                seekedProperty.Type = prop.PropertyType;

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


        static IEnumerable<List<string>> TraverseObject(Type t, Stack<string> path, SeekedProperty seekedProperty )
        {
            var props = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            if (props.Length == 0)
            {
                throw new InvalidOperationException("Type mismatch for property " + seekedProperty.Name);
            }

            foreach (var prop in props)
            {
                if (prop.PropertyType == seekedProperty.Type)
                {
                    if (prop.Name != seekedProperty.Name)
                        throw new InvalidOperationException("Name mismatch for property " + prop.Name);
                    var currentPath = new List<string>(path) {prop.Name};
                    yield return currentPath;
                }
                else
                {
                    path.Push(prop.Name);
                    foreach (var r in TraverseObject(prop.PropertyType, path, seekedProperty))
                    {
                        yield return r;
                    }

                }
            }
        }



        public enum PropertyNameEnforcement
        {
            None,
            InNestedTypesOnly,
            Always
        }




    }
}

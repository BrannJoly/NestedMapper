using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace NestedMapper
{
    public class ObjectMapper<T> where T:new()
    {
        public PropertyInfo CurrentTargetProperty;

        IEnumerable<List<string>> TraverseObject(Type t, Stack<string> path )
        {
            var props = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            if (props.Length == 0)
            {
                throw new InvalidOperationException("Name mismatch for property " + CurrentTargetProperty.Name);
            }

            foreach (var prop in props)
            {
                if (prop.Name == CurrentTargetProperty.Name)
                {
                    if (prop.PropertyType != CurrentTargetProperty.PropertyType)
                        throw new InvalidOperationException("Type mismatch for property " + prop.Name);
                    var currentPath = new List<string>(path) {prop.Name};
                    yield return currentPath;
                }
                else
                {
                    path.Push(prop.Name);
                    foreach (var r in TraverseObject(prop.PropertyType, path))
                    {
                        yield return r;
                    }

                }
            }
        }

        private readonly object _mappingActionsLock=new object();
        private List<Action<T, dynamic>> _mappingActions;

        private void InitializeMapper(dynamic flatObject)
        {
            lock (_mappingActionsLock)
            {
                if (_mappingActions != null)
                    return;

                _mappingActions = new List<Action<T, dynamic>>();

                var x = TraverseObject(typeof(T), new Stack<string>()).GetEnumerator();


                foreach (var prop in flatObject.GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
                {
                    CurrentTargetProperty = prop;

                    if (!x.MoveNext())
                    {
                        throw new InvalidOperationException("too many fields in the flat object");
                    }

                    var foundPropertyPath = x.Current;

                    var exp = ExpressionTreeUtils.CreateNestedSetFromDynamicProperty<T>(foundPropertyPath, prop.Name);
                    _mappingActions.Add(exp.Compile());
                }

                if (x.MoveNext())
                {
                    throw new InvalidOperationException("Not enough fields in the flat object");
                }
            }
        }


        public T Map(dynamic flatObject)
        {
            if (flatObject == null) throw new ArgumentNullException(nameof(flatObject));

            InitializeMapper(flatObject);

            var r = new T();

            foreach (var action in _mappingActions)
            {
                action(r, flatObject);
            }

            return r;
        }

    }
}

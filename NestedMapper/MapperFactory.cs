using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

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


        private static List<PropertyBasicInfo> GetPropertyBasicInfos<T>(object sampleSourceObject) where T : new()
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


        public static IMapper<T> GetMapper<T>(NamesMismatch namesMismatch, object sampleSourceObject) where T:new()
        {
            var props = GetPropertyBasicInfos<T>(sampleSourceObject);

            var seekedProperty = props[0];

            var x = TraverseObject(typeof(T), new Stack<string>(), seekedProperty, namesMismatch,true).GetEnumerator();

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


            var constructorActions = GetConstructorActions<T>(mappings);

            var mappingActions = GetMappingLambda<T>(mappings);

            return new Mapper<T>(mappingActions, constructorActions);
        }

        private static List<Expression<Action<T>>> GetConstructorActions<T>(List<Mapping> mappings) where T : new()
        {
            var constructorActions =
                mappings.Select(m => m.TargetPath.Take(m.TargetPath.Count - 1))
                    .Distinct()
                    .Where(p => p.Count() != 0) //get all the distinct paths we assign to
                    .Select(ExpressionTreeUtils.CreateNestedSetConstructorLambda<T>)
                    .Where(a => a != null)
                    // if there's no default constructor, we'll ignore this action and hope the object is initialized properly by its parent. If it's not, we'll get a runtime error.
                    .ToList();
            return constructorActions;
        }

        private static List<Expression<Action<T, dynamic>>> GetMappingLambda<T>(List<Mapping> mappings) where T : new()
        {
            // target
            var targetParameterExpression = Expression.Parameter(typeof(T), "target");

            // source
            var sourceParameterExpression = Expression.Parameter(typeof(object), "source");

            var expressions = new List<Expression>();
            expressions.Add(sourceParameterExpression);
            expressions.Add(targetParameterExpression);

            var mappingActions = GetMappingExpressions<T>(mappings, targetParameterExpression, sourceParameterExpression);

            expressions.AddRange(mappingActions);

            var block = Expression.Block(expressions);

            // (target, value) => target.nested.targetPath = (type) source.sourceProperty;
            var assign = Expression.Lambda<Action<T, dynamic>>(block, targetParameterExpression,
                sourceParameterExpression);

            return new List<Expression<Action<T, dynamic>>> {assign};

        }

        private static List<BinaryExpression> GetMappingExpressions<T>(List<Mapping> mappings, ParameterExpression targetParameterExpression,
            ParameterExpression sourceParameterExpression) where T : new()
        {
            var mappingActions =
                mappings.Select(
                    mapping =>
                        ExpressionTreeUtils.CreateNestedSetFromDynamicProperty<T>(mapping.TargetPath,
                            mapping.SourceProperty, targetParameterExpression, sourceParameterExpression)).ToList();
            return mappingActions;
        }


        static IEnumerable<List<string>> TraverseObject(Type t, Stack<string> path, PropertyBasicInfo propertyBasicInfo, NamesMismatch namesMismatch, bool topLevel )
        {
            var props = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance );

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
                        || namesMismatch == NamesMismatch.AlwaysAllow
                        || (namesMismatch == NamesMismatch.AllowInNestedTypesOnly && !topLevel)
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

                    foreach (var r in TraverseObject(prop.PropertyType, path, propertyBasicInfo, namesMismatch, false))
                    {
                        yield return r;
                    }
                    path.Pop();

                }
            }
        }



        public enum NamesMismatch
        {
            AlwaysAllow,
            AllowInNestedTypesOnly,
            NeverAllow
        }




    }
}

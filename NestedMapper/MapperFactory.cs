using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NestedMapper
{

    public class MapperFactory
    {
        public enum NamesMismatch
        {
            AlwaysAllow,
            AllowInNestedTypesOnly,
            NeverAllow
        }

        public static IMapper<T> GetMapper<T>(NamesMismatch namesMismatch, object sampleSourceObject) where T : new()
        {
            var mappings = MappingsGetter.GetMappings<T>(namesMismatch, sampleSourceObject);

            var constructorActions = GetConstructorActions<T>(mappings);

            return new Mapper<T>(GetMappingLambda<T>(mappings).Compile(), constructorActions);
        }


        private static List<Expression<Action<T>>> GetConstructorActions<T>(List<MappingsGetter.Mapping> mappings) where T : new()
        {
            var constructorActions =
                mappings.Select(m => m.TargetPath.Take(m.TargetPath.Count - 1))
                    .Select(path=>string.Join(".", path))
                    .Distinct() //get all the distinct paths we assign to
                    .Where(path => !string.IsNullOrEmpty(path)) //ignore top level constructors
                    .Select(path=> path.Split('.'))
                    .Select(ExpressionTreeUtils.CreateNestedSetConstructorLambda<T>)
                    .Where(a => a != null)
                    // if there's no default constructor, we'll ignore this action and hope the object is initialized properly by its parent. If it's not, we'll get a runtime error.
                    .ToList();
            return constructorActions;
        }

        private static Expression<Action<T, dynamic>> GetMappingLambda<T>(List<MappingsGetter.Mapping> mappings) where T : new()
        {
            // target
            var targetParameterExpression = Expression.Parameter(typeof(T), "target");

            // source
            var sourceParameterExpression = Expression.Parameter(typeof(object), "source");

            var expressions = new List<Expression> {sourceParameterExpression, targetParameterExpression};

            var mappingActions = GetMappingExpressions(mappings, targetParameterExpression, sourceParameterExpression);
            expressions.AddRange(mappingActions);

            var block = Expression.Block(expressions);

            // (target, value) => target.nested.targetPath = (type) source.sourceProperty;
            var assign = Expression.Lambda<Action<T, dynamic>>(block, targetParameterExpression,
                sourceParameterExpression);

            return assign;

        }

        private static List<BinaryExpression> GetMappingExpressions(List<MappingsGetter.Mapping> mappings, ParameterExpression targetParameterExpression,
            ParameterExpression sourceParameterExpression)
        {
            var mappingActions =
                mappings.Select(
                    mapping =>
                        ExpressionTreeUtils.CreateNestedSetFromDynamicProperty(mapping.TargetPath,
                            mapping.SourceProperty, targetParameterExpression, sourceParameterExpression)).ToList();
            return mappingActions;
        }

    }
}

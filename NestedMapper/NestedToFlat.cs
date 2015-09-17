using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace NestedMapper
{
    class NestedToFlatBuilder
    {
        internal static Action<T, IDictionary<string,object>> UpdateDictionary<T>(Node tree, List<Mapping> mappings )
        {

            // nestedObject
            var nestedObjectParameterExpression = Expression.Parameter(typeof(T), "nested");

            // dic
            var dicParameterExpression = Expression.Parameter(typeof(IDictionary<string,object>), "flat");

            var exps =
                mappings.Where(mapping=>mapping.FlatProperty!=null).Select<Mapping, Expression>(mapping => GetDictionarySetterFromMapping(dicParameterExpression, mapping, nestedObjectParameterExpression));

            var block = Expression.Block(typeof(void), exps);

            var lambda = Expression.Lambda<Action<T, IDictionary<string, object>>>(block, nestedObjectParameterExpression, dicParameterExpression);

            return lambda.Compile();

        }

        private static Expression GetDictionarySetterFromMapping(ParameterExpression dicParameterExpression, Mapping mapping, ParameterExpression nestedObjectParameterExpression)
        {
            return SetValueInDictionaryExpression(dicParameterExpression, mapping.FlatProperty,
                Expression.Convert(GetNestedPropertyExpression(mapping.NestedPath, nestedObjectParameterExpression), typeof(object)));
        }

        internal static Expression GetNestedPropertyExpression(IEnumerable<string> nestedPath, ParameterExpression nestedObjectParameterExpression)
        {
            return nestedPath.Aggregate<string, Expression>(nestedObjectParameterExpression, (c, m) => Expression.Property(c, m));
        }

        internal static BinaryExpression SetValueInDictionaryExpression(ParameterExpression dicParameterExpression, string propertyName, Expression value)
        {
            return Expression.Assign(
                Expression.Property(dicParameterExpression, dicParameterExpression.Type.GetProperty("Item"),Expression.Constant(propertyName)),value);
        }
    }
}
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

        public static Func<dynamic,T> GetMapper<T>(object sampleSourceObject,
            NamesMismatch namesMismatch = NamesMismatch.AllowInNestedTypesOnly,
            IEnumerable<Type> assumeNullWontBeMappedToThoseTypes = null) where T : new()
        {
            var lambda = GetMapperExpression<T>(sampleSourceObject, namesMismatch, assumeNullWontBeMappedToThoseTypes);

            var compiled = lambda.Compile();
            return compiled;

        }

        public static Expression<Func<dynamic,T>> GetMapperExpression<T>(object sampleSourceObject, NamesMismatch namesMismatch,
            IEnumerable<Type> assumeNullWontBeMappedToThoseTypes) where T : new()
        {
            if (assumeNullWontBeMappedToThoseTypes == null)
                assumeNullWontBeMappedToThoseTypes = new List<Type>();

            var tree = MappingsGetter.GetMappingsTree<T>(sampleSourceObject, namesMismatch,
                assumeNullWontBeMappedToThoseTypes.ToList());

            // source
            var sourceParameterExpression = Expression.Parameter(typeof (object), "source");


            var lambda = Expression.Lambda<Func<dynamic, T>>(tree.GetExpression(sourceParameterExpression, namesMismatch),
                sourceParameterExpression);
            return lambda;
        }
    }
}

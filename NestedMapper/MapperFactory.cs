using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

        public class BidirectionalMapper<T>
        {
            public BidirectionalMapper(Func<dynamic, T> toNested, Func<T, object> toFlat)
            {
                ToNested = toNested;
                ToFlat = toFlat;
            }

            public Func<dynamic, T> ToNested { get; }

            public Func<T, object> ToFlat { get; }
        }


        public static Func<dynamic,T> GetMapper<T>(object sampleSourceObject,
            NamesMismatch namesMismatch = NamesMismatch.AllowInNestedTypesOnly,
            IEnumerable<Type> assumeNullWontBeMappedToThoseTypes = null) where T : new()
        {
            var bidirectionalMapper = GetBidirectionalMapper<T>(sampleSourceObject, namesMismatch, assumeNullWontBeMappedToThoseTypes);

            return bidirectionalMapper.ToNested;

        }

        public static BidirectionalMapper<T> GetBidirectionalMapper<T>(object sampleSourceObject, NamesMismatch namesMismatch,
            IEnumerable<Type> assumeNullWontBeMappedToThoseTypes) where T : new()
        {
            if (assumeNullWontBeMappedToThoseTypes == null)
                assumeNullWontBeMappedToThoseTypes = new List<Type>();

            var tree = GetMappingsTree<T>(sampleSourceObject, namesMismatch, assumeNullWontBeMappedToThoseTypes.ToList());

            var flatToNested = GetFlatToNestedLambda<T>(namesMismatch, tree);

            var nestedToFlat = GetNestedToFlatLambda<T>(tree);

            return new BidirectionalMapper<T>(flatToNested, nestedToFlat);

        }

        private static Func<T, object> GetNestedToFlatLambda<T>(Node tree)
        {
            
            return null;
        }

        private static Func<dynamic, T> GetFlatToNestedLambda<T>(NamesMismatch namesMismatch, Node tree) where T : new()
        {
            //source
            var sourceParameterExpression = Expression.Parameter(typeof (object), "source");

            var lambda = Expression.Lambda<Func<dynamic, T>>(tree.GetExpression(sourceParameterExpression, namesMismatch),
                sourceParameterExpression);
            return lambda.Compile();
        }


        static List<PropertyBasicInfo> GetPropertyBasicInfos(object sampleSourceObject)
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



        internal static Node GetMappingsTree<T>(object sampleSourceObject, MapperFactory.NamesMismatch namesMismatch,
              List<Type> assumeNullWontBeMappedToThoseTypes) where T : new()
        {
            var props = new Queue<PropertyBasicInfo>(GetPropertyBasicInfos(sampleSourceObject));

            var tree = MappingTreeBuilder.BuildTree(typeof(T), string.Empty, props, namesMismatch, assumeNullWontBeMappedToThoseTypes);

            if (props.Count != 0)
            {
                throw new InvalidOperationException("Too many fields in the flat object, don't know what to do with " + props.Peek().Name);
            }

            return tree;

        }
    }
}

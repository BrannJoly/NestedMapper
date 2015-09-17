using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NestedMapperTests")]

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
            IEnumerable<Type> assumeNullWontBeMappedToThoseTypes = null, IEnumerable<string> ignoredFields=null ) where T : new()
        {
            var bidirectionalMapper = GetBidirectionalMapper<T>(sampleSourceObject, namesMismatch, assumeNullWontBeMappedToThoseTypes, ignoredFields);

            return bidirectionalMapper.ToNested;

        }

        public static BidirectionalMapper<T> GetBidirectionalMapper<T>(object sampleSourceObject, NamesMismatch namesMismatch = NamesMismatch.AllowInNestedTypesOnly,
            IEnumerable<Type> assumeNullWontBeMappedToThoseTypes=null, IEnumerable<string> ignoredFields = null) where T : new()
        {
            if (assumeNullWontBeMappedToThoseTypes == null)
                assumeNullWontBeMappedToThoseTypes = new List<Type>();

            if (ignoredFields == null)
                ignoredFields = new List<string>();


            var tree = GetMappingsTree<T>(sampleSourceObject, namesMismatch, assumeNullWontBeMappedToThoseTypes.ToList(), ignoredFields.ToList());

            var flatToNested = GetFlatToNestedLambda<T>(namesMismatch, tree);

            var mappings = MappingsGetter.GetMappings(tree);

            var nestedToFlat = NestedToFlatBuilder.UpdateDictionary<T>(tree, mappings);

            return new BidirectionalMapper<T>(flatToNested, nestedToFlat, mappings);
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



        internal static Node GetMappingsTree<T>(object sampleSourceObject, NamesMismatch namesMismatch,
              List<Type> assumeNullWontBeMappedToThoseTypes, List<string> ignoredFields) where T : new()
        {
            var props = new Queue<PropertyBasicInfo>(GetPropertyBasicInfos(sampleSourceObject).Where(x=> !ignoredFields.Contains(x.Name)));

            var tree = MappingTreeBuilder.BuildTree(typeof(T), string.Empty, props, namesMismatch, assumeNullWontBeMappedToThoseTypes, ignoredFields);

            if (props.Count!=0)
            {
                throw new InvalidOperationException("Too many fields in the flat object, don't know what to do with " + props.Peek().Name);
            }

            return tree;

        }
    }
}

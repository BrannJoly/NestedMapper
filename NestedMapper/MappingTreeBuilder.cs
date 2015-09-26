using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NestedMapper
{
    class MappingTreeBuilder
    {
        private static bool ListContainsType(ICollection<Type> list, Type type)
        {
            if (list.Contains(type))
                return true;

            if (type.IsGenericType)
                return list.Contains(type.GetGenericTypeDefinition());

            return false;
        }

        public static Node BuildTree(Type t, string name, Queue<PropertyBasicInfo> sourceObjectProperties, MapperFactory.NamesMismatch namesMismatch,
            List<Type> assumeNullWontBeMappedToThoseTypes, List<string> ignoredFields )
        {
            var props =
                t.GetProperties( BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.GetSetMethod() != null);


            var nodes = new List<Node>();
            foreach (var prop in props)
            {
                if (ignoredFields.Contains(prop.Name))
                {
                    nodes.Add(new Node(prop.PropertyType, prop.Name));
                    continue;
                }

                if (sourceObjectProperties.Count == 0)
                {
                    throw new InvalidOperationException("Not enough fields in the flat object, don't know how to set " + prop.Name);
                }

                var propToMap = sourceObjectProperties.Peek();


                if ((propToMap.Type == null && !ListContainsType(assumeNullWontBeMappedToThoseTypes, prop.PropertyType))
                    || (propToMap.Type != null && AvailableCastChecker.CanCast(propToMap.Type, prop.PropertyType))
                    )
                {
                    if (namesMismatch == MapperFactory.NamesMismatch.AlwaysAllow || prop.Name == propToMap.Name)
                    {

                        nodes.Add(new Node(prop.PropertyType, prop.Name, propToMap.Name));
                        sourceObjectProperties.Dequeue();
                    }
                    else
                    {
                        throw new InvalidOperationException("Name mismatch for property " + prop.Name);

                    }
                }
                else if (prop.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).All(x => x.GetSetMethod() == null))
                {
                    var path = name == null ? null : name + ".";
                    // no sense recursing on this
                    throw new InvalidOperationException(
                        $"Type mismatch when mapping {propToMap.Name} ({propToMap.Type}) with {path}{prop.Name} ({prop.PropertyType})");
                }
                else
                {
                    nodes.Add(BuildTree(prop.PropertyType,prop.Name, sourceObjectProperties, namesMismatch== MapperFactory.NamesMismatch.AllowInNestedTypesOnly? MapperFactory.NamesMismatch.AlwaysAllow:namesMismatch, assumeNullWontBeMappedToThoseTypes, ignoredFields));
                }
            }


            return new Node(t,name,nodes);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace NestedMapper
{
    class MappingsGetter
    {
        class PropertyBasicInfo
        {
            public string Name { get;}
            public Type Type { get; }

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
            public Type PropertyType { get; set; }



            public Mapping(List<string> targetPath, string sourceProperty, Type propertyType)
            {
                TargetPath = targetPath;
                SourceProperty = sourceProperty;
                PropertyType = propertyType;

            }

            public override string ToString()
            {
                return string.Join(".", TargetPath) + " =(" + PropertyType + ") " + SourceProperty;
            }
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


        static bool ListContainsType(ICollection<Type> list, Type type)
        {
            if (list.Contains(type))
                return true;

            if (type.IsGenericType)
                return list.Contains(type.GetGenericTypeDefinition());

            return false;
        }


        public static Node GetMappingsTree<T>(object sampleSourceObject, MapperFactory.NamesMismatch namesMismatch,
              List<Type> assumeNullWontBeMappedToThoseTypes) where T : new()
        {
            var props = new Queue<PropertyBasicInfo>(GetPropertyBasicInfos(sampleSourceObject));

            var tree = BuildTree(typeof(T), String.Empty, props, namesMismatch, assumeNullWontBeMappedToThoseTypes);

            if (props.Count != 0)
            {
                throw new InvalidOperationException("Too many fields in the flat object, don't know what to do with " + props.Peek().Name);
            }

            return tree;


        }



        public static List<Mapping> GetMappings<T>(object sampleSourceObject, MapperFactory.NamesMismatch namesMismatch,
            List<Type> assumeNullWontBeMappedToThoseTypes) where T : new()
        {
            var tree = GetMappingsTree<T>(sampleSourceObject, namesMismatch, assumeNullWontBeMappedToThoseTypes);

            var mappings = new List<Mapping>();

            GetMappingsFromNode(tree,mappings,new List<string>());

            return mappings;


        }

        static void GetMappingsFromNode(Node node, List<Mapping> mappings, List<string> path )
        {
            if (node.Nodes != null)
            {
                foreach (var innerNode in node.Nodes)
                {
                    var currentPath = !string.IsNullOrEmpty(node.TargetName)
                        ? new List<string>(path) {node.TargetName}
                        : path;

                    GetMappingsFromNode(innerNode, mappings, currentPath);
                }
            }
            else
            {
                var currentPath = new List<string>(path) { node.TargetName };
                mappings.Add(new Mapping(currentPath, node.SourcePropertyName, node.TargetType));
            }
        }


        static Node BuildTree(Type t, string name, Queue<PropertyBasicInfo> sourceObjectProperties, MapperFactory.NamesMismatch namesMismatch, List<Type> assumeNullWontBeMappedToThoseTypes)
        {
            var props =
                t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.GetSetMethod() != null);


            var nodes = new List<Node>();
            foreach (var prop in props)
            {
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
                else if (prop.PropertyType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance).All(x => x.GetSetMethod() == null))
                {
                    // no sense recursing on this
                    throw new InvalidOperationException(
                        $"Type mismatch for property {prop.Name}, was excepting a {prop.PropertyType} and got an {propToMap.Type}");
                }
                else
                {
                    nodes.Add(BuildTree(prop.PropertyType,prop.Name, sourceObjectProperties, namesMismatch== MapperFactory.NamesMismatch.AllowInNestedTypesOnly? MapperFactory.NamesMismatch.AlwaysAllow:namesMismatch, assumeNullWontBeMappedToThoseTypes));
                }
            }


            return new Node(t,name,nodes);
        }

        internal class Node
        {
            public Type TargetType { get; }

            public string TargetName { get; }

            public string SourcePropertyName { get; }
            public List<Node> Nodes { get; }

            public Node(Type tartgetType, string targetName, string sourcePropertyName)
            {
                TargetType = tartgetType;
                TargetName = targetName;
                SourcePropertyName = sourcePropertyName;
            }

            public Node(Type tartgetType, string targetName, List<Node> nodes)
            {
                TargetType = tartgetType;
                TargetName = targetName;
                Nodes = nodes;
            }

            public Expression GetExpression(ParameterExpression sourceParameter, MapperFactory.NamesMismatch namesMismatch)
            {
                var namesMismatchSubtypes = namesMismatch == MapperFactory.NamesMismatch.AllowInNestedTypesOnly
                    ? MapperFactory.NamesMismatch.AlwaysAllow
                    : namesMismatch;
                if (SourcePropertyName != null)
                {
                    return GetCastedValue(sourceParameter);
                }
                // does a default constructor exist?
                var defaultConstructor = TargetType.GetConstructor(Type.EmptyTypes);

                if (defaultConstructor != null)
                {
                    // new object();
                    var newObjectExpression = Expression.New(defaultConstructor);


                    var objectInitializerBindings =
                        Nodes.Select(
                            childNode =>
                                Expression.Bind(TargetType.GetProperty(childNode.TargetName),
                                    childNode.GetExpression(sourceParameter, namesMismatchSubtypes))).Cast<MemberBinding>().ToList();


                    var creationExpression = Expression.MemberInit(newObjectExpression, objectInitializerBindings);

                    // (type)new object();
                    return Expression.Convert(creationExpression, TargetType);
                }
                else // let's see if there's a suitable non-default constructor
                {
                    var constructor = FindConstructor(TargetType,Nodes.Select(x => new PropertyBasicInfo(x.TargetName, x.TargetType)).ToList(),true);

                    if (constructor == null)
                    {
                        throw new InvalidOperationException("counldn't find a proper constructor to initialize " + TargetType);
                    }
                    var creationExpression = Expression.New(constructor, Nodes.Select(x => x.GetExpression(sourceParameter, namesMismatchSubtypes)));
                    return Expression.Convert(creationExpression, TargetType);

                }
            }

            static ConstructorInfo FindConstructor(Type type, List<PropertyBasicInfo>  parameters, bool allowNamesMismatch)
            {
                var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (ConstructorInfo ctor in constructors.OrderBy(c => c.IsPublic ? 0 : (c.IsPrivate ? 2 : 1)).ThenBy(c => c.GetParameters().Length))
                {
                    var ctorParameters = ctor.GetParameters();

                    if (ctorParameters.Length != parameters.Count)
                        continue;

                    var i = 0;
                    for (; i < ctorParameters.Length; i++)
                    {
                        if (!allowNamesMismatch && !string.Equals(ctorParameters[i].Name, parameters[i].Name, StringComparison.OrdinalIgnoreCase))
                            break;

                        if (!AvailableCastChecker.CanCast(parameters[i].Type, ctorParameters[i].ParameterType))
                            break;
                    }

                    if (i == ctorParameters.Length)
                        return ctor;
                }

                return null;
            }

            private Expression GetCastedValue(ParameterExpression sourceParameter)
            {
                // source.sourceProperty
                var binder = Binder.GetMember(CSharpBinderFlags.None, SourcePropertyName,
                    typeof (MappingsGetter),
                    new[] {CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
                var sourcePropertyExpression = Expression.Dynamic(binder, typeof (object), sourceParameter);

                // (type) source.sourceProperty;
                var convertBinder = Binder.Convert(CSharpBinderFlags.ConvertExplicit, TargetType, typeof (MappingsGetter));
                var castedValueExpression = Expression.Dynamic(convertBinder, TargetType, sourcePropertyExpression);

                return castedValueExpression;
            }
        }

    }
}
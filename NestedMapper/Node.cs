using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace NestedMapper
{
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

        /// <summary>
        /// gets an expression tree which recursively call the constructors of this node subNodes to build an object
        /// </summary>
        /// <param name="sourceParameter">the parameter containing the flat object which will be usde as source</param>
        /// <param name="namesMismatch">whether to allow names mismatches or not when searching for an appropriate default constructor</param>
        /// <returns>san expression tree which recursively call the constructors of this node subNodes to build an object</returns>
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

        static ConstructorInfo FindConstructor(Type type, IReadOnlyList<PropertyBasicInfo> parameters, bool allowNamesMismatch)
        {
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var constructorInfo in constructors.OrderBy(c => c.IsPublic ? 0 : (c.IsPrivate ? 2 : 1)).ThenBy(c => c.GetParameters().Length))
            {
                var ctorParameters = constructorInfo.GetParameters();

                if (ctorParameters.Length != parameters.Count)
                    continue;

                var i = 0;
                for (; i < ctorParameters.Length; i++)
                {
                    if (!allowNamesMismatch && !String.Equals(ctorParameters[i].Name, parameters[i].Name, StringComparison.OrdinalIgnoreCase))
                        break;

                    if (!AvailableCastChecker.CanCast(parameters[i].Type, ctorParameters[i].ParameterType))
                        break;
                }

                if (i == ctorParameters.Length)
                    return constructorInfo;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace NestedMapper
{
    public static class ExpressionTreeUtils
    {
        /// <summary>
        /// Generates an Expression that can assign a dynamic object property into a nested property
        /// </summary>
        /// <param name="targetPath">a path to a nested property </param>
        /// <param name="sourcePropertyName">the source property </param>
        /// <returns>the compiled set expression</returns>
        public static Expression<Action<T, dynamic>> CreateNestedSetFromDynamicProperty<T>(List<string> targetPath,
            string sourcePropertyName)
        {
            // target
            var targetParameterExpression = Expression.Parameter(typeof (T), "target");

            // target.nested.targetPath
            var propertyExpression = targetPath.Aggregate<string, Expression>(targetParameterExpression,
                Expression.Property);

            // source
            var sourceParameterExpression = Expression.Parameter(typeof (object), "source");

            var binder = Binder.GetMember(CSharpBinderFlags.None, sourcePropertyName, typeof (ExpressionTreeUtils),
                new[] {CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});

            // source.sourceProperty
            var sourcePropertyExpression = Expression.Dynamic(binder, typeof (object), sourceParameterExpression);

            // (type) source.sourceProperty;
            var castedValueExpression = Expression.Convert(sourcePropertyExpression, propertyExpression.Type);

            //target.nested.targetPath = (type) source.sourceProperty;
            var assignExpression = Expression.Assign(propertyExpression, castedValueExpression);

            // (target, value) => target.nested.targetPath = (type) source.sourceProperty;
            var assign = Expression.Lambda<Action<T, dynamic>>(assignExpression, targetParameterExpression,
                sourceParameterExpression);

            return assign;

        }

        public static Expression<Action<T>> CreateNestedSetConstructor<T>(IEnumerable<string> targetPath)
        {

            // target
            var targetParameterExpression = Expression.Parameter(typeof (T), "target");

            // target.nested.targetPath
            var propertyExpression = targetPath.Aggregate<string, Expression>(targetParameterExpression,
                Expression.Property);

            // does a default constructor exist?
            var defaultConstructor = propertyExpression.Type.GetConstructor(Type.EmptyTypes);

            if (defaultConstructor == null)
            {
                return null;
            }

            // new object();
            var newObjectExpression = Expression.New(defaultConstructor);

            // (type)new object();
            var castedValueExpression = Expression.Convert(newObjectExpression, propertyExpression.Type);

            //target.nested.targetPath = (type) new object();
            var assignExpression = Expression.Assign(propertyExpression, castedValueExpression);

            // (target) => target.nested.targetPath = (type) new object();
            var assign = Expression.Lambda<Action<T>>(assignExpression, targetParameterExpression);

            return assign;

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;

namespace NestedMapper
{
    public static class ExpressionTreeUtils
    {
        /// <summary>
        /// Generates an Expression that can assign a dynamic object property into a nested property
        /// </summary>
        /// <param name="targetPath">a path to a nested property </param>
        /// <param name="sourceProperty">the source property </param>
        /// <returns>the compiled set expression</returns>
        public static Expression<Action<T, dynamic>> CreateNestedSetFromDynamicProperty<T>(List<string> targetPath, string sourceProperty)
        {
            // target
            var targetParameterExpression = Expression.Parameter(typeof(T), "target");

            // target.nested.targetPath
            var propertyExpression = targetPath.Aggregate<string, Expression>(targetParameterExpression, (c, m) => Expression.Property(c, m));

            // source
            var sourceParameterExpression = Expression.Parameter(typeof(object), "source");

            var binder = Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, sourceProperty, typeof(ExpressionTreeUtils),
             new CSharpArgumentInfo[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });

            // source.sourceProperty
            var sourcePropertyExpression = Expression.Dynamic(binder, typeof(object), sourceParameterExpression);

            // (type) source.sourceProperty;
            var castedValueExpression = Expression.Convert(sourcePropertyExpression, propertyExpression.Type);

            //target.nested.targetPath = (type) source.sourceProperty;
            var assignExpression = Expression.Assign(propertyExpression, castedValueExpression);

            // (target, value) => target.nested.targetPath = (type) source.sourceProperty;
            var assign = Expression.Lambda<Action<T, dynamic>>(assignExpression, targetParameterExpression, sourceParameterExpression);

            return assign;

        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NestedMapper
{

    public interface IMapper<out T> where T : new()
    {
        T Map(object source);
    }

    public class Mapper<T> : IMapper<T> where T : new()
    {
        private readonly List<Action<T>> _constructorActions = new List<Action<T>>();
        private readonly List<Action<T, dynamic>> _mappingActions= new List<Action<T, dynamic>>();

        public Mapper(List<Expression<Action<T, dynamic>>> mappingExpressions, List<Expression<Action<T>>> constructorExpressions)
        {

            foreach (var action in constructorExpressions)
            {
                _constructorActions.Add(action.Compile());
            }

            foreach (var action in mappingExpressions)
            {
                _mappingActions.Add(action.Compile());
            }

        }

        public T Map(object source)
        {
            var r = new T();

            foreach (var action in _constructorActions)
            {
                action(r);
            }

            foreach (var action in _mappingActions)
            {
                action(r, source);
            }

            return r;
        }
    }
}

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
        private readonly Action<T, dynamic> _mappingAction;

        public Mapper(Action<T, dynamic> mappingAction, List<Expression<Action<T>>> constructorExpressions)
        {

            foreach (var action in constructorExpressions)
            {
                _constructorActions.Add(action.Compile());
            }

            _mappingAction = mappingAction;

        }

        public T Map(object source)
        {
            var r = new T();

            foreach (var action in _constructorActions)
            {
                action(r);
            }

            _mappingAction(r, source);

            return r;
        }
    }
}

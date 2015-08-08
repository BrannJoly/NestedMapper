using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<Action<T, dynamic>> _mappingActions;

        public Mapper(List<Action<T, dynamic>> mappingActions)
        {
            _mappingActions = mappingActions;
        }

        public T Map(object source)
        {
            var r = new T();

            foreach (var action in _mappingActions)
            {
                action(r, source);
            }

            return r;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Dynamic;

namespace NestedMapper
{
    public class BidirectionalMapper<T>
    {
        public BidirectionalMapper(Func<object, T> toNested,  Action<T, IDictionary<string, object>> updateDictionary, List<Mapping> mappings)
        {
            ToNested = toNested;
            UpdateDictionary = updateDictionary;
            Mappings = mappings;
        }

        public Func<object, T> ToNested { get; }

        public Action<T, IDictionary<string,object>> UpdateDictionary { get; }

        public Func<T, object> ToDynamic
        {
            get
            {
                return delegate(T nested)
                {
                    dynamic r = new ExpandoObject();
                    UpdateDictionary(nested, (IDictionary<string, object>) r);
                    return r;
                };
            }
        }

        public List<Mapping> Mappings { get; }


    }
}
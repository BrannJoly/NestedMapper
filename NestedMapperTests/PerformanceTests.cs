using System;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NestedMapper;
using NFluent;
using System.Diagnostics;

namespace NestedMapperTests
{
    [TestClass]
    public class PerformanceTests
    {
        private int PerformanceMapTest(int iterations, IMapper<Foo> mapper, dynamic source)
        {
            var dontinline = 0;
            for (var i = 0; i < iterations; i++)
            {
                var foo = mapper.Map(source);
                dontinline += foo.I;
            }
            return dontinline;
        }

        [TestMethod]
        public void NestedMapperHasTheSameOrderOfMagnitudeThanNativeMapper()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.A = DateTime.Today;
            flatfoo.B = "N1B";

            const int iterations = 100000;


            var sw = new Stopwatch();
            sw.Start();

            for (var i = 0; i < iterations; i++)
            {
                var foo = new Foo
                {
                    I = flatfoo.I,
                    N =
                    {
                        A = flatfoo.A,
                        B = flatfoo.B
                    }
                };
                GC.KeepAlive(foo);
            }

            sw.Stop();

            var mapper = MapperFactory.GetMapper<Foo>(flatfoo, MapperFactory.NamesMismatch.NeverAllow);

            Check.ThatCode(() => PerformanceMapTest(iterations, mapper, flatfoo))
                .LastsLessThan(5*sw.ElapsedMilliseconds, TimeUnit.Milliseconds);
        }
    }
}

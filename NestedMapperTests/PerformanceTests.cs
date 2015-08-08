using System;
using System.Drawing;
using System.Dynamic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NestedMapper;
using NFluent;
using System.Diagnostics;

namespace NestedMapperTests
{
    [TestClass]
    public class PerformanceTests
    {


        public class NestedType
        {
            public DateTime A { get; set; }
            public string B { get; set; }
        }

        public class Foo
        {
            public int I { get; set; }

            public NestedType N { get; set; }

            public Foo()
            {
                N = new NestedType();
            }
        }


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
        public void NestedMapperIsntSlowerThanNativeMapper()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.A = DateTime.Today;
            flatfoo.B = "N1B";

            const int iterations = 100000;
            

            var sw = new Stopwatch();
            sw.Start();

            var dontinline = 0;
            for (var i = 0; i < iterations; i++)
            {
                var foo = new Foo();
                foo.I = flatfoo.I;
                foo.N.A = flatfoo.A;
                foo.N.B = flatfoo.B;
                dontinline += foo.I;
            }

           sw.Stop();

           var mapper = MapperFactory.GetMapper<Foo>(MapperFactory.PropertyNameEnforcement.Always, flatfoo);

           

            Check.ThatCode(() => PerformanceMapTest(1000, mapper, flatfoo)).LastsLessThan(sw.ElapsedMilliseconds*1.1, TimeUnit.Milliseconds);
        }
    }
}

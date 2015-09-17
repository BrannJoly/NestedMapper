using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NestedMapper;
using NFluent;

namespace NestedMapperTests
{
    [TestClass]
    public class IgnoredFieldsTests
    {

        public class FooEnd
        {
            public int I { get; set; }

       

            public NestedType N { get; set; }

            public List<string> List { get; set; }


            public FooEnd()
            {
                N = new NestedType();
            }
        }


        [TestMethod]
        public void IgnoredFields_at_the_end_are_handled_properly()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.A = DateTime.Today;
            flatfoo.B = "N1B";

            var mapper = MapperFactory.GetBidirectionalMapper<FooEnd> (flatfoo, MapperFactory.NamesMismatch.AllowInNestedTypesOnly, null,  new[] {"List"});

            FooEnd foo = mapper.ToNested(flatfoo);

            Check.That(foo.I).IsEqualTo(1);
            Check.That(foo.N.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N.B).IsEqualTo("N1B");
        }


        public class FooMiddle
        {
            public int I { get; set; }

            public List<string> List { get; set; }

            public NestedType N { get; set; }


            public FooMiddle()
            {
                N = new NestedType();
            }
        }



        [TestMethod]
        public void IgnoredFields_in_the_middle_are_handled_properly()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.A = DateTime.Today;
            flatfoo.B = "N1B";


            var mapper = MapperFactory.GetBidirectionalMapper<FooMiddle>(flatfoo, MapperFactory.NamesMismatch.AllowInNestedTypesOnly, null, new[] { "List" });

            FooMiddle foo = mapper.ToNested(flatfoo);

            Check.That(foo.I).IsEqualTo(1);
            Check.That(foo.N.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N.B).IsEqualTo("N1B");
        }


    }
}

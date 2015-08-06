using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NestedMapper;
using NFluent;

namespace NestedMapperTests
{
    [TestClass]
    public class MappingTest
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

        public class FlatFoo
        {
            public int I { get; set; }
            public DateTime A { get; set; }
            public string B { get; set; }
        }


        [TestMethod]
        public void TestSimpleMappingScenario()
        {
            var flatfoo = new FlatFoo()
            {
                I = 1,
                A = DateTime.Today,
                B = "B"
            };

            var foo = new ObjectMapper<Foo>().Map(flatfoo);

            Check.That(foo.I).Equals(1);
            Check.That(foo.N.A).Equals(DateTime.Today);
            Check.That(foo.N.B).Equals("B");

        }


        public class FlatFooWrongName
        {
            public int WrongName { get; set; }
            public DateTime A { get; set; }
            public string B { get; set; }
        }


        [TestMethod]
        public void DontMapIfMembersNameDontMatch()
        {
            var flatfoo = new FlatFooWrongName()
            {
                WrongName = 1,
                A = DateTime.Today,
                B = "B"
            };

            Check.ThatCode(() => new ObjectMapper<Foo>().Map(flatfoo)).Throws<InvalidOperationException>();

        }


        public class FlatFooTooManyFields
        {
            public int I { get; set; }
            public DateTime A { get; set; }
            public string B { get; set; }

            public int UnusedField { get; set; }
        }


        [TestMethod]
        public void DontMapIfTooManyfields()
        {
            var flatfoo = new FlatFooTooManyFields()
            {
                I = 1,
                A = DateTime.Today,
                B = "B",
                UnusedField =0
            };

            Check.ThatCode(() => new ObjectMapper<Foo>().Map(flatfoo)).Throws<InvalidOperationException>();

        }


        public class FlatFooNotEnoughFields
        {
            public int I { get; set; }
            public DateTime A { get; set; }

        }

        [TestMethod]
        public void DontMapIfNotEnoughfields()
        {
            var flatfoo = new FlatFooNotEnoughFields()
            {
                I = 1,
                A = DateTime.Today,
            };

            Check.ThatCode(() => new ObjectMapper<Foo>().Map(flatfoo)).Throws<InvalidOperationException>();

        }


        public class FlatFooWrongType
        {
            public string I { get; set; }
            public int A { get; set; }
            public string B { get; set; }
        }


        [TestMethod]
        public void DontMapIfTypesDontMatch()
        {
            var flatfoo = new FlatFooWrongType()
            {
                I = "WrongType",
                A = 2,
                B = "B"
            };

            Check.ThatCode(() => new ObjectMapper<Foo>().Map(flatfoo)).Throws<InvalidOperationException>();

        }


    }
}

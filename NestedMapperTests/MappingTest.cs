using System;
using System.Drawing;
using System.Dynamic;
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

            Foo foo = MapperFactory.GetMapper<Foo>(MapperFactory.NamesMismatch.NeverAllow, flatfoo ). Map(flatfoo);

            Check.That(foo.I).IsEqualTo(1);
            Check.That(foo.N.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N.B).IsEqualTo("B");

        }


        [TestMethod]
        public void TestSimpleMappingScenarioWithExpandoObject()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.A = DateTime.Today;
            flatfoo.B = "N1B";


            Foo foo = MapperFactory.GetMapper<Foo>(MapperFactory.NamesMismatch.NeverAllow, flatfoo).Map(flatfoo);

            Check.That(foo.I).IsEqualTo(1);
            Check.That(foo.N.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N.B).IsEqualTo("N1B");

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

            Check.ThatCode(() => MapperFactory.GetMapper<Foo>(MapperFactory.NamesMismatch.NeverAllow, flatfoo).Map(flatfoo)).Throws<InvalidOperationException>().WithMessage("too many fields in the flat object");

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

            Check.ThatCode(() => MapperFactory.GetMapper<Foo>(MapperFactory.NamesMismatch.NeverAllow, flatfoo).Map(flatfoo)).Throws<InvalidOperationException>().WithMessage("Not enough fields in the flat object");

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

            Check.ThatCode(() => MapperFactory.GetMapper<Foo>(MapperFactory.NamesMismatch.NeverAllow, flatfoo).Map(flatfoo)).Throws<InvalidOperationException>().WithMessage("Type mismatch for property I") ;

        }

        public class FooPosition
        {
            public string Name { get; set; }

            public Point Position { get; set; }

            public FooPosition()
            {
                Position= new Point(0,0);
            }
        }


        [TestMethod]
        public void MappingToANestedStructureWorks()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.Name = "Foo";
            flatfoo.x = 45;
            flatfoo.y = 200;

            FooPosition fooPosition = MapperFactory.GetMapper<FooPosition>(MapperFactory.NamesMismatch.AllowInNestedTypesOnly, flatfoo).Map(flatfoo);

            Check.That(fooPosition.Name).IsEqualTo("Foo");
            Check.That(fooPosition.Position.X).IsEqualTo(45);
            Check.That(fooPosition.Position.Y).IsEqualTo(200);

        }


        [TestMethod]
        public void TestSimpleMappingScenarioWithExpandoObjectAndUnknownType()
        {
            dynamic flatFooSource = new ExpandoObject();
            flatFooSource.I = 1;
            flatFooSource.A = DateTime.Today;
            flatFooSource.B = null;

            var mapper = MapperFactory.GetMapper<Foo>(MapperFactory.NamesMismatch.NeverAllow, flatFooSource);



            dynamic flatFoo = new ExpandoObject();
            flatFoo.I = 1;
            flatFoo.A = DateTime.Today;
            flatFoo.B = "N1B";

            Foo foo =mapper.Map(flatFoo);

            Check.That(foo.I).IsEqualTo(1);
            Check.That(foo.N.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N.B).IsEqualTo("N1B");

        }





    }
}

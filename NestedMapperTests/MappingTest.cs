using System;
using System.Collections.Generic;
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

        public class FlatFoo
        {
            public int I { get; set; }
            public DateTime A { get; set; }
            public string B { get; set; }
        }



        [TestMethod]
        public void TestSimpleMappingScenarioWithSampleSourceObject()
        {
            var flatfoo = new FlatFoo
            {
                I = 1,
                A = DateTime.Today,
                B = "B"
            };

            Foo foo = MapperFactory.GetBidirectionalMapper<Foo>(flatfoo).ToNested(flatfoo);

            Check.That(foo.I).IsEqualTo(1);
            Check.That(foo.N.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N.B).IsEqualTo("B");

        }


        [TestMethod]
        public void TestSimpleMappingScenarioWithProvidedProperties()
        {
            var props = new List<PropertyBasicInfo>
            {
                new PropertyBasicInfo("I", typeof (int)),
                new PropertyBasicInfo("A", typeof (DateTime)),
                new PropertyBasicInfo("B", typeof (string))
            };

            var flatfoo = new FlatFoo
            {
                I = 1,
                A = DateTime.Today,
                B = "B"
            };
            
            Foo foo = MapperFactory.GetBidirectionalMapper<Foo>(props).ToNested(flatfoo);

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


            Foo foo = MapperFactory.GetBidirectionalMapper<Foo>(flatfoo, MapperFactory.NamesMismatch.NeverAllow).ToNested(flatfoo);

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
            var flatfoo = new FlatFooTooManyFields
            {
                I = 1,
                A = DateTime.Today,
                B = "B",
                UnusedField = 0
            };

            Check.ThatCode(
                () => MapperFactory.GetBidirectionalMapper<Foo>(flatfoo).ToNested(flatfoo))
                .Throws<InvalidOperationException>()
                .WithMessage("Too many fields in the flat object, don't know what to do with UnusedField");

        }


        public class FlatFooNotEnoughFields
        {
            public int I { get; set; }
            public DateTime A { get; set; }

        }

        [TestMethod]
        public void DontMapIfNotEnoughfields()
        {
            var flatfoo = new FlatFooNotEnoughFields
            {
                I = 1,
                A = DateTime.Today
            };

            Check.ThatCode(
                () => MapperFactory.GetBidirectionalMapper<Foo>(flatfoo).ToNested(flatfoo))
                .Throws<InvalidOperationException>()
                .WithMessage(@"Not enough fields in the flat object, don't know how to set B");


        }


        public class FlatFooWrongType
        {
            public int I{ get; set; }
            public string FlatA { get; set; }
            public string FlatB { get; set; }
        }


        [TestMethod]
        public void DontMapIfTypesDontMatch()
        {
            var flatfoo = new FlatFooWrongType
            {
                I = 0,
                FlatA = "WrongType",
                FlatB = "B"
            };

            Check.ThatCode(
                () => MapperFactory.GetBidirectionalMapper<Foo>(flatfoo).ToNested(flatfoo))
                .Throws<InvalidOperationException>()
                .WithMessage("Type mismatch when mapping FlatA (System.String) with N.A (System.DateTime)");

        }

        public class FooPosition
        {
            public string Name { get; set; }

            public Point Position { get; set; }

            public FooPosition()
            {
                Position = new Point(0, 0);
            }
        }


        [TestMethod]
        public void MappingToANestedStructureWorks()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.Name = "Foo";
            flatfoo.x = 45;
            flatfoo.y = 200;

            FooPosition fooPosition =
                MapperFactory.GetBidirectionalMapper<FooPosition>(flatfoo, MapperFactory.NamesMismatch.AllowInNestedTypesOnly).ToNested(flatfoo);

            Check.That(fooPosition.Name).IsEqualTo("Foo");
            Check.That(fooPosition.Position.X).IsEqualTo(45);
            Check.That(fooPosition.Position.Y).IsEqualTo(200);

        }


        [TestMethod]
        public void TestSimpleMappingScenarioWithExpandoObjectAndUnknownType()
        {
            dynamic flatFooSource = new ExpandoObject();
            flatFooSource.I = 1;
            flatFooSource.A = null;
            flatFooSource.B = "N1B";

            var mapper = MapperFactory.GetBidirectionalMapper<Foo>(flatFooSource, MapperFactory.NamesMismatch.NeverAllow,
                new List<Type> {typeof (NestedType)});

            dynamic flatFoo = new ExpandoObject();
            flatFoo.I = 1;
            flatFoo.A = DateTime.Today;
            flatFoo.B = "N1B";

            Foo foo = mapper.ToNested(flatFoo);

            Check.That(foo.I).IsEqualTo(1);
            Check.That(foo.N.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N.B).IsEqualTo("N1B");

        }

        public class ImplicitCastTestTarget
        {
            public decimal TargetDecimalProp { get; set; }
        }


        [TestMethod]
        public void TestSimpleMappingWithImplicitCast()
        {
            dynamic flatFoo = new ExpandoObject();
            flatFoo.IntProp = 1;

            var mapper = MapperFactory.GetBidirectionalMapper<ImplicitCastTestTarget>(flatFoo,
                MapperFactory.NamesMismatch.AlwaysAllow);

            ImplicitCastTestTarget foo = mapper.ToNested(flatFoo);


            Check.That(foo.TargetDecimalProp).IsEqualTo(1m);

        }

        public class NullableFoo
        {
            public int? I { get; set; }

        }

        [TestMethod]
        public void Test_Mapping_null_To_Nullable_Types_Works()
        {
            dynamic flatFoo = new ExpandoObject();
            flatFoo.IntProp = null;

            var mapper = MapperFactory.GetBidirectionalMapper<NullableFoo>(flatFoo, MapperFactory.NamesMismatch.AlwaysAllow);

            NullableFoo foo = mapper.ToNested(flatFoo);


            Check.That(foo.I).IsNull();

        }

        [TestMethod]
        public void Test_Mapping_To_Nullable_Types_Works()
        {
            dynamic flatFoo = new ExpandoObject();
            flatFoo.IntProp = 1;

            var mapper = MapperFactory.GetBidirectionalMapper<NullableFoo>(flatFoo, MapperFactory.NamesMismatch.AlwaysAllow);

            NullableFoo foo = mapper.ToNested(flatFoo);


            Check.That(foo.I).IsEqualTo(1);

        }


    }
}

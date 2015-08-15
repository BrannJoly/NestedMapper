﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.Remoting.Messaging;
using Microsoft.CSharp.RuntimeBinder;
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
        public void TestSimpleMappingScenario()
        {
            var flatfoo = new FlatFoo()
            {
                I = 1,
                A = DateTime.Today,
                B = "B"
            };

            Foo foo = MapperFactory.GetMapper<Foo>(flatfoo, MapperFactory.NamesMismatch.NeverAllow).Map(flatfoo);

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


            Foo foo = MapperFactory.GetMapper<Foo>(flatfoo, MapperFactory.NamesMismatch.NeverAllow).Map(flatfoo);

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
                UnusedField = 0
            };

            Check.ThatCode(
                () => MapperFactory.GetMapper<Foo>(flatfoo, MapperFactory.NamesMismatch.NeverAllow).Map(flatfoo))
                .Throws<InvalidOperationException>();
            //.WithMessage("too many fields in the flat object");

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

            Check.ThatCode(
                () => MapperFactory.GetMapper<Foo>(flatfoo, MapperFactory.NamesMismatch.NeverAllow).Map(flatfoo))
                .Throws<InvalidOperationException>();
//                .WithMessage(@"Not enough fields in the flat object
//current mappings so far : 
//I =(System.Int32) I
//N.A =(System.DateTime) A
//");

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

            Check.ThatCode(
                () => MapperFactory.GetMapper<Foo>(flatfoo, MapperFactory.NamesMismatch.NeverAllow).Map(flatfoo))
                .Throws<InvalidOperationException>();
                //.WithMessage("Type mismatch for property I");

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
                MapperFactory.GetMapper<FooPosition>(flatfoo, MapperFactory.NamesMismatch.AllowInNestedTypesOnly)
                    .Map(flatfoo);

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

            var mapper = MapperFactory.GetMapper<Foo>(flatFooSource, MapperFactory.NamesMismatch.NeverAllow, new List<Type> {typeof(NestedType) });



            dynamic flatFoo = new ExpandoObject();
            flatFoo.I = 1;
            flatFoo.A = DateTime.Today;
            flatFoo.B = "N1B";

            Foo foo = mapper.Map(flatFoo);

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

            var mapper = MapperFactory.GetMapper<ImplicitCastTestTarget>(flatFoo, MapperFactory.NamesMismatch.AlwaysAllow);

            ImplicitCastTestTarget foo = mapper.Map(flatFoo);


            Check.That(foo.TargetDecimalProp).IsEqualTo(1m);

        }


    }
}

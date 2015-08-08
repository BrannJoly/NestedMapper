using System;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NestedMapper;
using NFluent;

namespace NestedMapperTests
{
    [TestClass]
    public class PropertyNameEnforcementTests
    {


        public class FooMultipleNested
        {
            public int I { get; set; }

            public MappingTest.NestedType N1 { get; set; }

            public MappingTest.NestedType N2 { get; set; }

            public FooMultipleNested()
            {
                N1 = new MappingTest.NestedType();
                N2 = new MappingTest.NestedType();
            }
        }



        [TestMethod]
        public void DontMapIf_PropertyNameEnforcementIsAlways_AndNamesMismatch()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.N1A = DateTime.Today;
            flatfoo.N1B = "N1B";
            flatfoo.N2A = DateTime.Today;
            flatfoo.N2B = "N1B";

            Check.ThatCode(() => MapperFactory.GetMapper<MappingTest.Foo>(MapperFactory.PropertyNameEnforcement.Always, flatfoo).Map(flatfoo)).Throws<InvalidOperationException>();

        }

        [TestMethod]
        public void DontMapIf_PropertyNameEnforcementIsInNestedTypesOnly_AndNamesMismatchOnTop()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.Mismatch = 1;
            flatfoo.N1A = DateTime.Today;
            flatfoo.N1B = "N1B";
            flatfoo.N2A = DateTime.Today;
            flatfoo.N2B = "N1B";

            Check.ThatCode(() => MapperFactory.GetMapper<MappingTest.Foo>(MapperFactory.PropertyNameEnforcement.InNestedTypesOnly, flatfoo).Map(flatfoo)).Throws<InvalidOperationException>();

        }

        [TestMethod]
        public void DoMapIf_PropertyNameEnforcementIsNever_AndNamesMismatch()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.Mismatch = 1;
            flatfoo.N1A = DateTime.Today;
            flatfoo.N1B = "N1B";
            flatfoo.N2A = DateTime.Today;
            flatfoo.N2B = "N2B";
            var foo = MapperFactory.GetMapper<FooMultipleNested>(MapperFactory.PropertyNameEnforcement.Never, flatfoo).Map(flatfoo);

            Check.That(foo.I).Equals(1);
            Check.That(foo.N1.A).Equals(DateTime.Today);
            Check.That(foo.N1.B).Equals("N1B");
            Check.That(foo.N2.A).Equals(DateTime.Today);
            Check.That(foo.N2.B).Equals("N2B");
        }


        [TestMethod]
        public void DoMapIf_PropertyNameEnforcementIsInNestedTypesOnly_AndNamesMismatchOnlyInNestedTypes()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.N1A = DateTime.Today;
            flatfoo.N1B = "N1B";
            flatfoo.N2A = DateTime.Today;
            flatfoo.N2B = "N2B";
            var foo = MapperFactory.GetMapper<FooMultipleNested>(MapperFactory.PropertyNameEnforcement.InNestedTypesOnly, flatfoo).Map(flatfoo);

            Check.That(foo.I).Equals(1);
            Check.That(foo.N1.A).Equals(DateTime.Today);
            Check.That(foo.N1.B).Equals("N1B");
            Check.That(foo.N2.A).Equals(DateTime.Today);
            Check.That(foo.N2.B).Equals("N2B");
        }


    }
}

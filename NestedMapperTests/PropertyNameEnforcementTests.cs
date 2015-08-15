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

            public NestedType N1 { get; set; }

            public NestedType N2 { get; set; }
        }



        [TestMethod]
        public void DontMapIf_PropertyNameEnforcementIs_NeverAllow_AndNamesMismatch()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.N1A = DateTime.Today;
            flatfoo.N1B = "N1B";
            flatfoo.N2A = DateTime.Today;
            flatfoo.N2B = "N1B";

            Check.ThatCode(
                () => MapperFactory.GetMapper<Foo>(MapperFactory.NamesMismatch.NeverAllow, flatfoo).Map(flatfoo))
                .Throws<InvalidOperationException>();
            //.WithMessage("Name mismatch for property A");

        }

        [TestMethod]
        public void DontMapIf_PropertyNameEnforcementIsIn_AllowInNestedTypesOnlyy_AndNamesMismatchOnTop()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.Mismatch = 1;
            flatfoo.N1A = DateTime.Today;
            flatfoo.N1B = "N1B";
            flatfoo.N2A = DateTime.Today;
            flatfoo.N2B = "N1B";

            Check.ThatCode(
                () =>
                    MapperFactory.GetMapper<Foo>(MapperFactory.NamesMismatch.AllowInNestedTypesOnly, flatfoo)
                        .Map(flatfoo))
                .Throws<InvalidOperationException>();
            //.WithMessage("Name mismatch for property I");

        }

        [TestMethod]
        public void DoMapIf_PropertyNameEnforcementIs_AlwaysAllow_AndNamesMismatch()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.Mismatch = 1;
            flatfoo.N1A = DateTime.Today;
            flatfoo.N1B = "N1B";
            flatfoo.N2A = DateTime.Today;
            flatfoo.N2B = "N2B";
            FooMultipleNested foo = MapperFactory.GetMapper<FooMultipleNested>(MapperFactory.NamesMismatch.AlwaysAllow, flatfoo).Map(flatfoo);

            Check.That(foo.I).IsEqualTo(1);
            Check.That(foo.N1.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N1.B).IsEqualTo("N1B");
            Check.That(foo.N2.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N2.B).IsEqualTo("N2B");
        }


        [TestMethod]
        public void DoMapIf_PropertyNameEnforcement_AllowInNestedTypesOnly_AndNamesMismatchOnlyInNestedTypes()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.N1A = DateTime.Today;
            flatfoo.N1B = "N1B";
            flatfoo.N2A = DateTime.Today;
            flatfoo.N2B = "N2B";
            FooMultipleNested foo = MapperFactory.GetMapper<FooMultipleNested>(MapperFactory.NamesMismatch.AllowInNestedTypesOnly, flatfoo).Map(flatfoo);

            Check.That(foo.I).IsEqualTo(1);
            Check.That(foo.N1.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N1.B).IsEqualTo("N1B");
            Check.That(foo.N2.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N2.B).IsEqualTo("N2B");
        }



    }
}

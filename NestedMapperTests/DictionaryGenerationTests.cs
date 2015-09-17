using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NestedMapper;
using NFluent;

namespace NestedMapperTests
{
    [TestClass]
    public class DictionaryGenerationTests
    {
        [TestMethod]
        public void Test_That_We_Can_Update_An_Expando()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.A = DateTime.Today;
            flatfoo.B = "N1B";

            var mapper = MapperFactory.GetBidirectionalMapper<Foo>(flatfoo, MapperFactory.NamesMismatch.NeverAllow);

            var nested = mapper.ToNested(flatfoo);

            dynamic expando = mapper.ToDynamic(nested);

            var dic = (IDictionary<string, object>) expando;

            Check.That(dic["I"]).IsEqualTo(1);
            Check.That(dic["A"]).IsEqualTo(DateTime.Today);
            Check.That(dic["B"]).IsEqualTo("N1B");
        }

        [TestMethod]
        public void TestUpdateDictionary()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.A = DateTime.Today;
            flatfoo.B = "N1B";

            var mapper = MapperFactory.GetBidirectionalMapper<Foo>(flatfoo, MapperFactory.NamesMismatch.NeverAllow);

            var nested = mapper.ToNested(flatfoo);

            var dic = new Dictionary<string, object>();

            mapper.UpdateDictionary(nested, dic);

            Check.That(dic["I"]).IsEqualTo(1);
            Check.That(dic["A"]).IsEqualTo(DateTime.Today);
            Check.That(dic["B"]).IsEqualTo("N1B");

        }


        [TestMethod]
        public void Test_SetValueInDictionaryExpression()
        {
            var dic = new Dictionary<string, object>();

            // dic
            var dicParameterExpression = Expression.Parameter(typeof(Dictionary<string, object>), "flat");

            var exp = NestedToFlatBuilder.SetValueInDictionaryExpression(dicParameterExpression, "I",Expression.Convert(Expression.Constant(1),typeof(object)));
            var lambda = Expression.Lambda<Action<Dictionary<string, object>>>(exp, dicParameterExpression).Compile();

            lambda(dic);

            Check.That(dic["I"]).IsEqualTo(1);

        }

        [TestMethod]
        public void Test_GetNestedPropertyExpression()
        {
            var foo = new Foo {N = new NestedType {B = "test"}};

            // nestedObject
            var nestedObjectParameterExpression = Expression.Parameter(typeof(Foo), "nested");

            var exp = NestedToFlatBuilder.GetNestedPropertyExpression(new[] {"N", "B"}, nestedObjectParameterExpression);
            var lambda = Expression.Lambda<Func<Foo,object>>(exp, nestedObjectParameterExpression).Compile();

            var r = lambda(foo);

            Check.That(r).IsEqualTo("test");

        }

    }
}

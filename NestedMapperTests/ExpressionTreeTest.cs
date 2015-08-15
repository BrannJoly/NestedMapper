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
    public class ExpressionTreeTest
    {
        public class Foo
        {
            public Bar MyBar { get; set; }
        }
        public class Bar
        {
            public string Test { get; set; }
        }

        public class FlatClass
        {
            public string Input { get; set; }
        }

        [TestMethod]
        public void CheckThatNestedAssignmentWorks()
        {

            var f = ExpressionTreeUtils.CreateNestedSetFromDynamicPropertyLambda<Foo>(new List<string>(new[] {"myBar", "Test"}),"Input").Compile();

            var foo = new Foo { MyBar = new Bar() };
            dynamic myDynamic = new ExpandoObject();
            myDynamic.Input = "test";

            f(foo, myDynamic);

            Check.That(foo.MyBar.Test).IsEqualTo(("test"));
        }

        [TestMethod]
        public void CheckThatNestedContructorCallWorks()
        {

            var f = ExpressionTreeUtils.CreateNestedSetConstructorLambda<Foo>(new List<string>(new[] { "myBar"})).Compile();

            var foo = new Foo();

            f(foo);

            Check.That(foo.MyBar).IsNotEqualTo(null);
        }

 
    }
}

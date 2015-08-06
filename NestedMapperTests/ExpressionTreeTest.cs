using System.Collections.Generic;
using System.Dynamic;
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

            var f = ExpressionTreeUtils.CreateNestedSetFromDynamicProperty<Foo>(new List<string>(new[] {"myBar", "Test"}),"Input");

            var foo = new Foo { MyBar = new Bar() };
            dynamic myDynamic = new ExpandoObject();
            myDynamic.Input = "test";

            f.Compile()(foo, myDynamic);

            Check.That(foo.MyBar.Test).Equals(("test"));
        }

    }
}

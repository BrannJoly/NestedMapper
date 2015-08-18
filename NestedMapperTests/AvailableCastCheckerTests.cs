using Microsoft.VisualStudio.TestTools.UnitTesting;
using NestedMapper;
using NFluent;

namespace NestedMapperTests
{
    [TestClass]
    public class AvailableCastCheckerTests
    {

        [TestMethod]
        public void HasImplicitConversion_Works_On_Builtin_Types()
        {
            Check.That(AvailableCastChecker.CanCast(typeof (int), typeof (decimal))).IsTrue();
        }

        public enum Test
        {
           A
        }

        [TestMethod]
        public void HasImplicitConversion_Works_On_Enums()
        {
            Check.That(AvailableCastChecker.CanCast(typeof(int), typeof(Test))).IsTrue();
        }


        [TestMethod]
        public void HasImplicitConversion_Works_On_NullableEnums()
        {
            Check.That(AvailableCastChecker.CanCast(typeof(int), typeof(Test?))).IsTrue();
        }


    }
}

using System;
using System.Dynamic;
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


    }
}

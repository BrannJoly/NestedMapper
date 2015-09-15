using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NestedMapper;
using NFluent;

namespace NestedMapperTests
{
    [TestClass]
    public class PerformanceTests
    {
        private static void TestNestedMapperGeneratedCode(int iterations, Func<dynamic,Foo> mapper, dynamic source)
        {
            for (var i = 0; i < iterations; i++)
            {
                var foo = PerformNestedMapperMapping(mapper, source);
                GC.KeepAlive(foo);
            }
        }

        private static dynamic PerformNestedMapperMapping(Func<dynamic, Foo> mapper, dynamic source)
        {
            var foo = mapper(source);
            return foo;
        }


        private static void SaveLambda(LambdaExpression lambda)
        {
            var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("test"),AssemblyBuilderAccess.Save);
            var type = asm.DefineDynamicModule("myModule", "test.dll").DefineType("myType");
            var builder = type.DefineMethod("Foo", MethodAttributes.Public | MethodAttributes.Static);
            lambda.CompileToMethod(builder);
            type.CreateType();
            asm.Save("test.dll");
        }

        private int _acceptedSlownessFactor = 2;
        private int _iterations= 100000;
        [TestMethod]
        public void NestedMapper_Isnt_Significantly_Slower_Than_Native_Code()
        {
            dynamic flatfoo = new ExpandoObject();
            flatfoo.I = 1;
            flatfoo.A = DateTime.Today;
            flatfoo.B = "N1B";

            var sw = new Stopwatch();
            sw.Start();

            TestNativeCode(_iterations, flatfoo);

            sw.Stop();

            var mapper = MapperFactory.GetBidirectionalMapper<Foo>(flatfoo, MapperFactory.NamesMismatch.NeverAllow, new List<Type>());

            //SaveLambda(lambda);

            Check.ThatCode(() => TestNestedMapperGeneratedCode(_iterations, mapper, flatfoo))
                .LastsLessThan(_acceptedSlownessFactor * sw.ElapsedMilliseconds, TimeUnit.Milliseconds);
        }

        private static void TestNativeCode(int iterations, dynamic flatfoo)
        {
            for (var i = 0; i < iterations; i++)
            {
                var foo = PerformNativeMapping(flatfoo);
                GC.KeepAlive(foo);
            }
        }

        private static Foo PerformNativeMapping(dynamic flatfoo)
        {
            var foo = new Foo
            {
                I = flatfoo.I,
                N =
                {
                    A = flatfoo.A,
                    B = flatfoo.B
                }
            };
            return foo;
        }
    }
}

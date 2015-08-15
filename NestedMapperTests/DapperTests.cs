using System;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NestedMapper;
using NFluent;
using System.Linq;
using Dapper;

namespace NestedMapperTests
{
    [TestClass]
    public class DapperTests
    {

        public SqlConnection GetDatabaseConnection()
        {

            var c = new SqlConnection("Data Source=.;Initial Catalog=tempdb;Integrated Security=True");
            c.Open();
            return c;

        }


        [TestMethod]
        public void SimpleDapperMapppingWorks()
        {
            var connection = GetDatabaseConnection();

            var flatfoo = connection.Query("select 1 as I, cast ('" + DateTime.Today.ToString("yyyyMMdd") + "' as date) A, 'N1B' as B").Single();

            Foo foo = MapperFactory.GetMapper<Foo>(MapperFactory.NamesMismatch.NeverAllow, flatfoo).Map(flatfoo);


            Check.That(foo.I).IsEqualTo(1);
            Check.That(foo.N.A).IsEqualTo(DateTime.Today);
            Check.That(foo.N.B).IsEqualTo("N1B");

        }
    }
}

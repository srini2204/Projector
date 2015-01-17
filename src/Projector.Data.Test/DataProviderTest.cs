using NUnit.Framework;
using System.Linq;

namespace Projector.Data.Test
{
    [TestFixture]
    public class DataProviderTest
    {
        [Test]
        public void CreateTableTest()
        {
            var table = DataProvider.CreateTable<Person>();
            
            Assert.AreEqual(2, table.Schema.Columns.Count);
            
            var field1 = table.Schema.Columns.Single(x => x.Name == "Name");
            Assert.AreEqual(typeof(string), field1.DataType);
            
            var field2 = table.Schema.Columns.Single(x => x.Name == "Age");
            Assert.AreEqual(typeof(int), field2.DataType);
        }

        [Test]
        public void CreateFilterTest()
        {
            var table = DataProvider.CreateTable<Person>();
            var filteredData = table.Filter(x => x.Age > 5);
        }

        [Test]
        public void CreateProjectionTest()
        {
            var table = DataProvider.CreateTable<Person>();
            var projectionData = table.Projection(x => new { x.Name, ProjectedAge = x.Age * 5 });
            
        }

        private class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

    }
}

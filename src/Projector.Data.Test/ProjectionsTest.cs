using NUnit.Framework;
using Projector.Data.Tables;
using System.Linq;

namespace Projector.Data.Test
{
    [TestFixture]
    public class ProjectionsTest
    {
        [Test]
        public void CreateTableTest()
        {
            var table = TableExtensions.CreateTable<Person>();

            Assert.AreEqual(2, table.Schema.Columns.Count);

            var field1 = table.Schema.Columns.Single(x => x.Name == "Name");
            Assert.AreEqual(typeof(string), field1.DataType);

            var field2 = table.Schema.Columns.Single(x => x.Name == "Age");
            Assert.AreEqual(typeof(int), field2.DataType);
        }

        [Test]
        public void CreateFilterTest()
        {
            var table = TableExtensions.CreateTable<Person>();
            var filteredData = table.Where(x => x.Age > 5);
        }

        [Test]
        public void CreateProjectionTest()
        {
            var table = TableExtensions.CreateTable<Person>();
            var projectionData = table.Projection(x => new { x.Name, ProjectedAge = x.Age * 5 });

        }

        [Test]
        public void CreateJoinTest()
        {
            var leftTable = TableExtensions.CreateTable<Person>();
            var rightTable = TableExtensions.CreateTable<Person>();

            var joinedResult = leftTable.InnerJoin(rightTable, left => left.Name, right => right.Name, (left, right) => new { left.Name, left.Age, RightAge = right.Age });

        }

        [Test]
        public void CreateLeftJoinTest()
        {
            var leftTable = TableExtensions.CreateTable<Person>();
            var rightTable = TableExtensions.CreateTable<Person>();

            var joinedResult = leftTable.LeftJoin(rightTable, left => left.Name, right => right.Name, (left, right) => new { left.Name, left.Age, RightAge = right.Age });

        }

        [Test]
        public void CreateRightJoinTest()
        {
            var leftTable = TableExtensions.CreateTable<Person>();
            var rightTable = TableExtensions.CreateTable<Person>();

            var joinedResult = leftTable.RightJoin(rightTable, left => left.Name, right => right.Name, (left, right) => new { left.Name, left.Age, RightAge = right.Age });

        }

        [Test]
        public void ProjectionChainingTest()
        {
            var sourceTable = TableExtensions.CreateTable<Person>();

            var result = sourceTable
                .Where(p => p.Age > 25)
                .Projection(p => new { p.Age, Name1 = p.Name, NameAge = p.Name + p.Age });
        }

        private class Person
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

    }
}

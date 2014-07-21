using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projector.Data.Test
{
    [TestFixture]
    public class TableTest
    {
        private Mock<IDataConsumer> _dataConsumer;
        private class Client
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }

        [SetUp]
        public void InitContext()
        {
            _dataConsumer = new Mock<IDataConsumer>();

        }

        [Test]
        public void TestSubscribeWhenZeroRowsInside()
        {
            var table = new Table<Client>(item => item.Id.ToString());
            table.Subscribe(_dataConsumer.Object);

            _dataConsumer.Verify(x => x.OnSchema(It.IsAny<ISchema>()), Times.Once);
            _dataConsumer.Verify(x => x.OnAdd(It.IsAny<long[]>(), It.IsAny<long>()), Times.Never);
            _dataConsumer.Verify(x => x.OnUpdate(It.IsAny<long[]>(), It.IsAny<long>()), Times.Never);
            _dataConsumer.Verify(x => x.OnDelete(It.IsAny<long[]>(), It.IsAny<long>()), Times.Never);
            _dataConsumer.Verify(x => x.OnSyncPoint(), Times.Once);
        }

        [Test]
        public void TestSubscribeWhenSeveralRowsInside()
        {
            var table = new Table<Client>(item => item.Id.ToString());
            table.Add(new Client { Id = 123, Name = "John Doe" });
            table.Add(new Client { Id = 124, Name = "John Doe" });
            table.Add(new Client { Id = 125, Name = "John Doe" });
            table.FireChanges();
            table.Subscribe(_dataConsumer.Object);

            _dataConsumer.Verify(x => x.OnSchema(It.IsAny<ISchema>()), Times.Once);
            _dataConsumer.Verify(x => x.OnAdd(
                                            It.Is<long[]>(ids => ids[0] == 0 
                                                                && ids[1] == 1
                                                                && ids[2] == 2),
                                            It.Is<long>(count => count == 3)),
                                            Times.Once);
            _dataConsumer.Verify(x => x.OnUpdate(It.IsAny<long[]>(), It.IsAny<long>()), Times.Never);
            _dataConsumer.Verify(x => x.OnDelete(It.IsAny<long[]>(), It.IsAny<long>()), Times.Never);
            _dataConsumer.Verify(x => x.OnSyncPoint(), Times.Once);
        }

        [Test]
        public void AddTest()
        {
            var table = new Table<Client>(item => item.Id.ToString());
            var id = table.Add(new Client { Id = 123, Name = "John Doe" });
        }

        [Test]
        public void DeleteTest()
        {
            var table = new Table<Client>(item => item.Id.ToString());
            var id = table.Add(new Client { Id = 123, Name = "John Doe" });
        }

        [Test]
        public void FireChangesTest()
        {
            var table = new Table<Client>(item => item.Id.ToString());
            var id = table.Add(new Client { Id = 123, Name = "John Doe" });
        }
    }
}

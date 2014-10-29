//using Moq;
//using NUnit.Framework;
//using Projector.Data.Filters;
//using Projector.Data.Test.Helpers;

//namespace Projector.Data.Test
//{
//    [TestFixture]
//    class FilterTest
//    {
//        private Filter _filter;
//        private Table<Client> _source;
//        private Mock<IDataConsumer> _dataConsumer;

//        class FilterCreteria : IFilterCriteria
//        {
//            public bool Check(ISchema schema, long id)
//            {
//                return id >= 3;
//            }
//        }

//        [SetUp]
//        public void InitContext()
//        {
//            _dataConsumer = new Mock<IDataConsumer>();

//            _filter = new Filter(new FilterCreteria());

//            _source = new Table<Client>(item => item.Id.ToString());
//            _source.Add(new Client { Id = 123, Name = "John Doe" });
//            _source.Add(new Client { Id = 124, Name = "John Doe" });
//            _source.Add(new Client { Id = 125, Name = "John Doe" });
//            _source.Add(new Client { Id = 126, Name = "John Doe" });
//            _source.Add(new Client { Id = 127, Name = "John Doe" });
//            _source.Add(new Client { Id = 128, Name = "John Doe" });
//            _source.FireChanges();
//        }

//        [Test]
//        public void TestFilteringOnSubscription()
//        {
//            _source.Subscribe(_filter);

//            _filter.Subscribe(_dataConsumer.Object);
            

//            _dataConsumer.Verify(x => x.OnSchema(It.IsAny<ISchema>()), Times.Once);
//            _dataConsumer.Verify(x => x.OnAdd(
//                                            It.Is<long[]>(ids => ids[0] == 3
//                                                                && ids[1] == 4
//                                                                && ids[2] == 5),
//                                            It.Is<long>(count => count == 3)),
//                                            Times.Once);
//            _dataConsumer.Verify(x => x.OnUpdate(It.IsAny<long[]>(), It.IsAny<long>()), Times.Never);
//            _dataConsumer.Verify(x => x.OnDelete(It.IsAny<long[]>(), It.IsAny<long>()), Times.Never);
//            _dataConsumer.Verify(x => x.OnSyncPoint(), Times.Once);
//        }

//    }
//}

using Moq;
using NUnit.Framework;
using Projector.Data.Filters;
using Projector.Data.Test.Helpers;

namespace Projector.Data.Test
{
    [TestFixture]
    class FilterTest
    {
        private Filter _filter;
        private Table<Client> _source;
        private Mock<IDataConsumer> _dataConsumer;

        class FilterCreteria:IFilterCriteria
        {
            public bool Check(ISchema schema, long id)
            {
                return id > 3;
            }
        }

        [SetUp]
        public void InitContext()
        {
            _dataConsumer = new Mock<IDataConsumer>();

            _filter = new Filter(new FilterCreteria());
            _source = new Table<Client>(item => item.Id.ToString());
            _source.Add(new Client { Id = 123, Name = "John Doe" });
            _source.Add(new Client { Id = 124, Name = "John Doe" });
            _source.Add(new Client { Id = 125, Name = "John Doe" });
            _source.FireChanges();

            _filter.Subscribe(_dataConsumer.Object);
            _source.Subscribe(_filter);
        }

        [Test]
        public void TestSubscribeWhenZeroRowsInside()
        {

        }
    }
}

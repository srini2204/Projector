using Moq;
using NUnit.Framework;

namespace Projector.Data.Test
{
    [TestFixture]
    public class SchemaTest
    {
        [SetUp]
        public void InitContext()
        {
            var _dataConsumer = new Mock<IDataConsumer>();

        }

        [Test]
        public void TestSubscribeWhenZeroRowsInside()
        {

        }
    }
}

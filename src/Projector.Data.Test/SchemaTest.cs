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

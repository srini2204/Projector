using System.Net.Sockets;


namespace Projector.IO.Server
{
    class Mediator
    {

        private IncomingDataPreparer theIncomingDataPreparer;
        private OutgoingDataPreparer theOutgoingDataPreparer;
        private DataHolder theDataHolder;
        private SocketAsyncEventArgs saeaObject;
        private DataHoldingUserToken receiveSendToken;

        public Mediator(SocketAsyncEventArgs e)
        {

            this.saeaObject = e;
            this.theIncomingDataPreparer = new IncomingDataPreparer(saeaObject);
            this.theOutgoingDataPreparer = new OutgoingDataPreparer();
        }


        internal void HandleData(DataHolder incomingDataHolder)
        {
            theDataHolder = theIncomingDataPreparer.HandleReceivedData(incomingDataHolder, this.saeaObject);
        }

        internal void PrepareOutgoingData()
        {
            theOutgoingDataPreparer.PrepareOutgoingData(saeaObject, theDataHolder);
        }


        internal SocketAsyncEventArgs GiveBack()
        {
            return saeaObject;
        }
    }
}

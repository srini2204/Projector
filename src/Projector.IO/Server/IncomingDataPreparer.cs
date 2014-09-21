using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketAsyncServer
{
    class IncomingDataPreparer
    {
        //object that will be used to lock the listOfDataHolders
        private object _lockerForList = new object();
        private DataHolder _theDataHolder;
        private SocketAsyncEventArgs _theSaeaObject;
        private int _mainTransMissionId = 1000;

        public IncomingDataPreparer(SocketAsyncEventArgs e)
        {
            _theSaeaObject = e;
        }

        private int ReceivedTransMissionIdGetter()
        {
            var receivedTransMissionId = Interlocked.Increment(ref _mainTransMissionId);
            return receivedTransMissionId;
        }

        private EndPoint GetRemoteEndpoint()
        {
            return _theSaeaObject.AcceptSocket.RemoteEndPoint;
        }

        internal DataHolder HandleReceivedData(DataHolder incomingDataHolder, SocketAsyncEventArgs theSaeaObject)
        {
            var receiveToken = (DataHoldingUserToken)theSaeaObject.UserToken;

            _theDataHolder = incomingDataHolder;
            _theDataHolder.sessionId = receiveToken.SessionId;
            _theDataHolder.receivedTransMissionId = ReceivedTransMissionIdGetter();
            _theDataHolder.remoteEndpoint = GetRemoteEndpoint();

            return _theDataHolder;
        }
    }
}

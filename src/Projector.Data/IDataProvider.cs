﻿
namespace Projector.Data
{
    public interface IDataProvider
    {
        IDisconnectable AddConsumer(IDataConsumer consumer);
        void RemoveConsumer(IDataConsumer consumer);
    }
}

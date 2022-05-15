
using System;

namespace SecyrityMail.Data
{
    public interface IMailEventProxy
    {
        event EventHandler<EventActionArgs> EventCb;

        void SubscribeProxyEvent(EventHandler<EventActionArgs> handler);
        void UnSubscribeProxyEvent(EventHandler<EventActionArgs> handler);
    }
}
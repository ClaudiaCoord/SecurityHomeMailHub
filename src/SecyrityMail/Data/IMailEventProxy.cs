/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


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
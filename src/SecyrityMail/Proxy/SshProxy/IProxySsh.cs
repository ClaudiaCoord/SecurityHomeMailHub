/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System.Net;

namespace SecyrityMail.Proxy.SshProxy
{
    public interface IProxySsh
    {
        bool IsConnected { get; }
        IPEndPoint LocalEndPoint { get; set; }

        void Dispose();
    }
}
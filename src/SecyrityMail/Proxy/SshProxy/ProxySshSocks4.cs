/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using MailKit.Net.Proxy;

namespace SecyrityMail.Proxy.SshProxy
{
    internal class ProxySshSocks4 : Socks4Client, IProxySsh, IDisposable
    {
        public const string SshProxyHost = "127.0.0.1";
        public const int SshProxyPort = 33114;
        private ProxySshClient Client = default;

        public ProxySshSocks4(SshAccount acc) : base(SshProxyHost, SshProxyPort) => Client = new ProxySshClient(SshProxyHost, SshProxyPort, acc);
        ~ProxySshSocks4() => Dispose();

        public bool IsConnected => (Client != default) && Client.IsConnected;

        public void Dispose() {

            ProxySshClient c = Client;
            Client = default;
            if (c != null)
                c.Dispose();
        }
    }
}

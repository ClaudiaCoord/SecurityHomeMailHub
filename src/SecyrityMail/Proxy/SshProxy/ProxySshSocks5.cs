/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using MailKit.Net.Proxy;

namespace SecyrityMail.Proxy.SshProxy
{
    internal class ProxySshSocks5 : Socks5Client, IProxySsh, IDisposable
    {
        public const string SshProxyHost = "127.0.0.1";
        public const int SshProxyPort = 33115;
        private ProxySshClient Client = default;

        public ProxySshSocks5(SshAccount acc) : base(SshProxyHost, SshProxyPort) => Client = new ProxySshClient(SshProxyHost, SshProxyPort, acc);
        ~ProxySshSocks5() => Dispose();

        public bool IsConnected => (Client != default) && Client.IsConnected;

        public void Dispose()
        {
            ProxySshClient c = Client;
            Client = default;
            if (c != null)
                c.Dispose();
        }
    }
}

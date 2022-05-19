/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Proxy;
using MailKit.Security;

namespace SecyrityMail.Clients
{
    public interface IMailClient
    {
        int Timeout { get; set; }
        string LocalDomain { get; set; }
        IPEndPoint LocalEndPoint { get; set; }
        IProxyClient ProxyClient { get; set; }
        void Dispose();
        void Disconnect(bool quit, CancellationToken token = default);
        Task AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default);
        Task ConnectAsync(string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default);
    }
}
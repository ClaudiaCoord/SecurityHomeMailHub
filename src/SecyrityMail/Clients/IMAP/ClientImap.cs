/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Proxy;
using MailKit.Security;

namespace SecyrityMail.Clients.IMAP
{
    internal class ClientImap : ImapClient, IMailClient
    {
        public string LocalDomain { get => string.Empty; set { }}
        public new int Timeout { get => base.Timeout; set => base.Timeout = value; }
        public new IPEndPoint LocalEndPoint { get => base.LocalEndPoint; set => base.LocalEndPoint = value; }
        public new IProxyClient ProxyClient { get => base.ProxyClient; set => base.ProxyClient = value; }
        public new async Task ConnectAsync(string host, int port = 0, SecureSocketOptions options = SecureSocketOptions.Auto, CancellationToken cancellationToken = default(CancellationToken)) =>
            await base.ConnectAsync(host, port, options, cancellationToken);
        public new async Task AuthenticateAsync(string userName, string password, CancellationToken cancellationToken = default(CancellationToken)) =>
            await base.AuthenticateAsync(userName, password, cancellationToken);

        public static IMailClient Create(IProtocolLogger logger) => new ClientImap(logger);
        public new void Dispose() => base.Dispose(true);
        public override void Disconnect(bool quit, CancellationToken token = default) => base.Disconnect(quit, token);
        public override async Task DisconnectAsync(bool quit, CancellationToken token = default) => await base.DisconnectAsync(quit, token);

        private ClientImap(IProtocolLogger logger) : base(logger) {
            base.CheckCertificateRevocation = false;
            base.ServerCertificateValidationCallback = (s, c, h, e) => true;
        }
    }
}

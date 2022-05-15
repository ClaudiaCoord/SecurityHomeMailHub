
using System;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace SecyrityMail.Proxy.SshProxy
{
    internal class ProxySshClient : IDisposable
    {
        private SshClient Client = default;
        private ForwardedPortDynamic PortFw = default;

        public ProxySshClient(string host, int port, SshAccount acc) => Init(host, (uint)port, acc);
        ~ProxySshClient() => Dispose();

        public bool IsConnected => (Client != default) && Client.IsConnected;

        private void Init(string host, uint port, SshAccount acc) {

            if (acc.IsEmpty || acc.IsExpired)
                throw new ArgumentException(nameof(SshAccount));

            Client = new SshClient(acc.Host, acc.Port, acc.Login, acc.Pass);
            PortFw = new ForwardedPortDynamic(host, port);

            Client.HostKeyReceived += Client_HostKeyReceived;
            Client.Connect();

            PortFw.Exception += PortFw_Exception;
            PortFw.RequestReceived += PortFw_RequestReceived;
            Client.AddForwardedPort(PortFw);
            PortFw.Start();
        }

        private void PortFw_Exception(object sender, ExceptionEventArgs e) =>
            Global.Instance.Log.Add(nameof(ProxySshSocks5), e.Exception);

        private void PortFw_RequestReceived(object sender, PortForwardEventArgs e) =>
            Global.Instance.Log.Add(nameof(ProxySshSocks5), $"call: {Client.ConnectionInfo.Username} -> {e.OriginatorHost}:{e.OriginatorPort}");

        private void Client_HostKeyReceived(object sender, HostKeyEventArgs e) => e.CanTrust = true;

        public void Dispose()
        {
            SshClient c = Client;
            Client = default;
            if (c != null)
            {
                c.HostKeyReceived -= Client_HostKeyReceived;
                if (c.IsConnected)
                    c.Disconnect();
                c.Dispose();
            }
            ForwardedPortDynamic p = PortFw;
            PortFw = default;
            if (p != null)
            {
                p.RequestReceived -= PortFw_RequestReceived;
                p.Exception -= PortFw_Exception;
                if (p.IsStarted)
                    p.Stop();
                p.Dispose();
            }
        }
    }
}

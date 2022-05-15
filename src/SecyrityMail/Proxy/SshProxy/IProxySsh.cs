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
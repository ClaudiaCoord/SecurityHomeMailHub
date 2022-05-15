
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SecyrityMail.Servers
{
    internal class StreamSession : Stream, IDisposable
    {
        private static readonly string CertificateFile = "MailSecurity.cer";
        private NetworkStream nstream_ = default(NetworkStream);
        private SslStream sstream_ = default(SslStream);
        private static X509Certificate cert_ = default(X509Certificate);

        public bool DataAvailable => IsDataAvailable;
        public bool IsDataAvailable => IsEnable && ((nstream_ != default) ? nstream_.DataAvailable : false);
        public bool IsEnable => (Client != null) && Client.Connected && (Stream != null);
        public bool IsSecure { get; private set; } = false;
        public EndPoint IpEndPoint => Client.Client.RemoteEndPoint;
        public TcpClient Client { get; private set; }
        public Stream Stream { get; private set; }

        public StreamSession(TcpClient client, bool issecure)
        {
            IsSecure = issecure;
            Client = client;
            Init();
        }
        ~StreamSession() => Dispose();

        private async void Init()
        {
            Stream = nstream_ = Client.GetStream();
            Stream.ReadTimeout = Stream.WriteTimeout = 5000;
            if (IsSecure)
                await StartTls().ConfigureAwait(false);
        }

        public async Task StartTls()
        {
            await Task.Run(() => {
                try {
                    if ((sstream_ != null) || (nstream_ == null))
                        return;
                    
                    IsSecure = !IsSecure ? true : IsSecure;
                    if (cert_ == null)
                        cert_ = CertLoad();
                    if (cert_ == null) {
                        Dispose();
                        return;
                    }
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls;
                    Stream = sstream_ = new SslStream(nstream_, false, ValidateCertificate);
                    sstream_.AuthenticateAsServer(cert_, false, false);
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(StreamSession), ex); }
            });
        }

        public new void Dispose()
        {
            Stream s = sstream_;
            sstream_ = null;
            if (s != null)
            {
                s.Close();
                try { s.Dispose(); } catch { }
            }
            s = nstream_;
            nstream_ = null;
            if (s != null)
            {
                s.Close();
                try { s.Dispose(); } catch { }
            }
            TcpClient c = Client;
            Client = null;
            if (c != null)
            {
                if (c.Connected)
                    c.Close();
                try { c.Dispose(); } catch { }
            }
            X509Certificate t = cert_;
            cert_ = null;
            if (t != null)
                try { t.Dispose(); } catch { }
            base.Dispose();
        }

        public override bool CanRead => Stream.CanRead;
        public override bool CanSeek => Stream.CanSeek;
        public override bool CanWrite => Stream.CanWrite;
        public override long Length => Stream.Length;
        public override long Position { get => Stream.Position; set => Stream.Position = value; }

        public override void Flush() { if (IsEnable) Stream.Flush(); }
        public override int  Read(byte[] buffer, int offset, int count) { if (IsEnable) return Stream.Read(buffer, offset, count); return 0; }
        public override long Seek(long offset, SeekOrigin origin) { if (IsEnable) return Stream.Seek(offset, origin); return 0L; }
        public override void SetLength(long value) { if (IsEnable) Stream.SetLength(value); }
        public override void Write(byte[] buffer, int offset, int count) { if (IsEnable) Stream.Write(buffer, offset, count); }

        private static bool ValidateCertificate(object s, X509Certificate c, X509Chain h, SslPolicyErrors p) => true;
        private X509Certificate2 CertLoad()
        {
            try {
                FileInfo f = new(Global.GetRootFile(Global.DirectoryPlace.Root, CertificateFile));
                if ((f != null) && f.Exists) {
#                   if NET5_0_OR_GREATER
                    return new X509Certificate2(f.FullName);
#                   else
                    X509Certificate2 cert = new X509Certificate2();
                    cert.Import(f.FullName);
                    return cert;
#                   endif
                }
            }
            catch (Exception ex) { Global.Instance.Log.Add($"{nameof(CertLoad)}-File", ex); }
            try {
                CertificateRequest request;
#               if ECDSA_KEY
                ECDsa ecdsa = ECDsa.Create();
                request = new CertificateRequest(
                    $"cn={nameof(SecyrityMail)}", ecdsa, HashAlgorithmName.SHA256);
#               else
                RSA rsa = RSA.Create();
                request = new CertificateRequest(
                    $"cn={nameof(SecyrityMail)}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#               endif
                byte[] b = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1)).Export(X509ContentType.Pkcs12);
                if (b != null)
                    return new X509Certificate2(b);
            }
            catch (Exception ex) { Global.Instance.Log.Add($"{nameof(CertLoad)}-Create", ex); }
            try {
                using Stream cs = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{nameof(SecyrityMail)}.{CertificateFile}");
                if (cs == null)
                    return default;

                byte[] raw = new byte[cs.Length];
                for (int i = 0; i < cs.Length; ++i)
                    raw[i] = (byte)cs.ReadByte();

#               if NET5_0_OR_GREATER
                return new X509Certificate2(raw);
#               else
                X509Certificate2 cert = new X509Certificate2();
                cert.Import(raw);
                return cert;
#               endif
            }
            catch (Exception ex) { Global.Instance.Log.Add($"{nameof(CertLoad)}-Resource", ex); }
            return default;
        }
    }
}

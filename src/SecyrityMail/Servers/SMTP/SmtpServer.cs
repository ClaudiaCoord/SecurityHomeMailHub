/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SecyrityMail.Data;
using SecyrityMail.Utils;

namespace SecyrityMail.Servers.SMTP
{
    public class SmtpServer : ServerBase, IMailEventProxy
    {
        public SmtpServer(int port, CancellationTokenSafe token) : base(port, IPAddress.Any, token, 465) => InitOptions();
        public SmtpServer(int port, IPAddress ip, CancellationTokenSafe token) : base(port, ip, token, 465) => InitOptions();
        ~SmtpServer() => Dispose();

        protected override void InitOptions() {
            IsLog = Global.Instance.Config.IsSmtpLog;
            IsSecure = Global.Instance.Config.IsSmtpSecure;
        }

        public override void Start()
        {
            MainThread = new Thread(async () => {
                try
                {
                    Listener.Start();
                    IsServiceRun = true;

                    while (true)
                    {
                        SafeToken.TokenSafe.ThrowIfCancellationRequested();
                        SmtpSession client = new SmtpSession(
                            Listener.AcceptTcpClient(),
                            (a) => a.UnSubscribeProxyEvent(eventClient),
                            (e) => AddSpamFilter(e),
                            SafeToken.TokenSafe, IsSecure, IsLog, LogLocation);

                        client.ClientIdle = Global.Instance.Config.SmtpClientIdle;
                        client.IsDeliveryLocal = Global.Instance.Config.IsSmtpDeliveryLocal;
                        client.SubscribeProxyEvent(eventClient);
                        if (!CheckSpamFilter(client.IpEndPoint)) {
                            byte[] buffer = Encoding.UTF8.GetBytes(SmtpResponseId.IpAccessDenied.SmtpResponse());
                            await client.Stream.WriteAsync(buffer, 0, buffer.Length)
                                               .ConfigureAwait(false);
                            client.Dispose();
                            continue;
                        }
                        Thread ClientThread = new Thread(new ThreadStart(client.NewSession));
                        ClientThread.Name = $"{nameof(SmtpServer)} - {client.IpEndPoint}";
                        ClientThread.IsBackground = true;
                        ClientThread.Start();
                    }
                }
                catch (SocketException ex) {
                    if (ex.SocketErrorCode == SocketError.Interrupted)
                        OnCallEvent(MailEventId.Cancelled, nameof(SmtpServer));
                    else
                        Global.Instance.Log.Add(nameof(SmtpServer), ex);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(SmtpServer), ex); }
                finally { Dispose(); IsServiceRun = false; }
            });
            MainThread.Name = $"{nameof(SmtpServer)} - {IpEndPoint}";
            MainThread.IsBackground = true;
            MainThread.Start();
            OnCallEvent(MailEventId.Started, nameof(SmtpServer));
        }
    }
}

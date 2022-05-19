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

namespace SecyrityMail.Servers.POP3
{
    public class Pop3Server : ServerBase, IMailEventProxy
    {
        public Pop3Server(int port, CancellationTokenSafe token) : base(port, IPAddress.Any, token, 995) { }
        public Pop3Server(int port, IPAddress ip, CancellationTokenSafe token) : base(port, ip, token, 995) { }
        ~Pop3Server() => base.Dispose();

        protected override void InitOptions() {
            IsLog = Global.Instance.Config.IsPop3Log;
            IsSecure = Global.Instance.Config.IsPop3Secure;
        }

        public override void Start()
        {
            MainThread = new Thread(async () => {
                try
                {
                    Listener.Start();
                    IsServiceRun = true;

                    while (true) {

                        SafeToken.TokenSafe.ThrowIfCancellationRequested();
                        Pop3Session client = new Pop3Session(
                            Listener.AcceptTcpClient(),
                            (a) => a.UnSubscribeProxyEvent(eventClient),
                            (e) => AddSpamFilter(e),
                            SafeToken.TokenSafe, IsSecure, IsLog, LogLocation);

                        client.ClientIdle = Global.Instance.Config.Pop3ClientIdle;
                        client.IsDeleteAllMessages = Global.Instance.Config.IsPop3DeleteAllMessages;
                        client.SubscribeProxyEvent(eventClient);
                        if (!CheckSpamFilter(client.IpEndPoint)) {
                            byte[] buffer = Encoding.UTF8.GetBytes(Pop3ResponseId.IpAccessDenied.Pop3Response());
                            await client.Stream.WriteAsync(buffer, 0, buffer.Length)
                                               .ConfigureAwait(false);
                            client.Dispose();
                            continue;
                        }
                        Thread ClientThread = new Thread(new ThreadStart(client.NewSession));
                        ClientThread.Name = $"{nameof(Pop3Server)} - {client.IpEndPoint}";
                        ClientThread.IsBackground = true;
                        ClientThread.Start();
                    }
                }
                catch (SocketException ex) {
                    if (ex.SocketErrorCode == SocketError.Interrupted)
                        OnCallEvent(MailEventId.Cancelled, nameof(Pop3Server));
                    else
                        Global.Instance.Log.Add(nameof(Pop3Server), ex);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Pop3Server), ex); }
                finally { Dispose(); IsServiceRun = false; }
            });
            MainThread.Name = $"{nameof(Pop3Server)} - {IpEndPoint}";
            MainThread.IsBackground = true;
            MainThread.Start();
            OnCallEvent(MailEventId.Started, nameof(Pop3Server));
        }
    }
}

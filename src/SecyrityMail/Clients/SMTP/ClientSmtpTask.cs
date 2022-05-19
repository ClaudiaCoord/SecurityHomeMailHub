/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using SecyrityMail.Clients.IMAP;
using SecyrityMail.Data;
using SecyrityMail.MailAccounts;
using SecyrityMail.Utils;

namespace SecyrityMail.Clients.SMTP
{
    internal class ClientSmtpTask : ClientEvent
    {
        #region constant
        public static readonly string IpSourceTag = "X-SourceIP";
        public static readonly string IpOriginatingTag = "X-Originating-IP";
        public static readonly string[] Xmailers = new string[]
        {
            "Mozilla 3.0b5 (X11; U; IRIX 5.3 IP22)",
            "Mozilla 1.1 (X11; U; Linux 1.1.47 i586)",
            "Mozilla 3.0b6Gold (X11; U; SunOS 5.5 sun4u)",
            "Mozilla 1.1N (Macintosh; I; 68K)",
            "Mozilla/5.0 (X11; Linux x86_64; rv:45.0) Gecko/20100101 Thunderbird/45.0",
            "Microsoft Outlook 15.0",
            "Microsoft Outlook 16.0",
            "Microsoft Office Outlook, Build 15.0.4569.1506",
            "Microsoft Office Outlook, Build 16.0.4229.1003",
            "Microsoft Office Outlook, Build 16.0.10336.20039",
            "Microsoft 365 16.0",
        };
        #endregion

        string user = string.Empty;
        readonly InitClientSession session;
        readonly EventHandler<EventActionArgs> evRoot;

        public ClientSmtpTask(InitClientSession s, EventHandler<EventActionArgs> a) {
            LogTag = nameof(ClientSmtp);
            session = s; evRoot = a;
            this.SubscribeProxyEvent(evRoot);
        }
        ~ClientSmtpTask() {
            Global.Instance.Log.Add(nameof(ClientSmtpTask), $"Smtp client for {user} end");
            this.UnSubscribeProxyEvent(evRoot);
        }

        #region Smtp Send
        public async Task<bool> Send(UserAccount account, string rootpath, TokenSafe token) =>
            await Task.Run(async () => {
                try {
                    if (account.IsEmptySend)
                        throw new Exception($"Mail Account not complette to Smtp {nameof(Send)}");
                    account.CurrentAction = AccountUsing.Smtp;
                    user = account.Email;

                    using SmtpClient client = (SmtpClient)await session.InitClient(
                        (a) => ClientSmtp.Create(a), account, rootpath, token);
                    if (client == default)
                        throw new Exception($"error initialize Smtp client from proxy list {Global.Instance.ProxyList.ProxyType}");

                    List<string> list = Directory.GetFiles(
                        Global.AppendPartDirectory(
                            rootpath, Global.DirectoryPlace.Out)).ToList();
                    if (list.Count == 0)
                        return false;

                    try {
                        client.Connected += Client_Connected;
                        client.MessageSent += Client_MessageSent;
                        client.Disconnected += Client_Disconnected;
                        client.Authenticated += Client_Authenticated;
                    } catch { }

                    int x = new Random().Next(0, Xmailers.Length);
                    Global.Instance.SmtpClientStat.SmtpLastMessageTotal = list.Count;

                    foreach (string s in list) {
                        try {
                            if (string.IsNullOrWhiteSpace(s))
                                continue;

                            FileInfo file = new(s);
                            if ((file == null) || !file.Exists || (file.Length == 0))
                                continue;

                            MimeMessage mmsg = await MimeMessage.LoadAsync(file.FullName)
                                                                .ConfigureAwait(false);
                            if (mmsg == null)
                                continue;

                            if (!string.IsNullOrWhiteSpace(account.ReplayTo) && (mmsg.ReplyTo.Count == 0))
                                mmsg.ReplyTo.Add(new MailboxAddress(account.Name, account.ReplayTo));
                            if (!string.IsNullOrWhiteSpace(account.Email) && (mmsg.From.Count == 0))
                                mmsg.From.Add(new MailboxAddress(account.Name, account.ReplayTo));
                            if (mmsg.Headers.Contains(HeaderId.XMailer))
                                mmsg.Headers.Replace(HeaderId.XMailer, Xmailers[x]);
                            else
                                mmsg.Headers.Add(HeaderId.XMailer, Xmailers[x]);

                            if (Global.Instance.Config.IsSmtpClientFakeIp) {

                                Tuple<string, int> ip = Global.Instance.ProxyList.Selected;
                                if (ip != null) {
                                    if (mmsg.Headers.Contains(IpSourceTag)) {
                                        if (x > 4)
                                            mmsg.Headers.Replace(IpOriginatingTag, $"[{ip.Item1}]");
                                        else
                                            mmsg.Headers.Replace(IpSourceTag, ip.Item1);
                                    } else {
                                        if (x > 4)
                                            mmsg.Headers.Add(IpOriginatingTag, $"[{ip.Item1}]");
                                        else
                                            mmsg.Headers.Add(IpSourceTag, ip.Item1);
                                    }
                                }
                            }

                            await client.SendAsync(mmsg)
                                        .ContinueWith((t) => {
                                            try {
                                                file.MoveTo(
                                                    Path.Combine(
                                                        Global.AppendPartDirectory(
                                                            rootpath, Global.DirectoryPlace.Send, DateTimeOffset.UtcNow),
                                                        Path.GetFileName(file.FullName)));
                                                t.Dispose();
                                            } catch { }
                                        })
                                        .ConfigureAwait(false);
                            Global.Instance.SmtpClientStat.SmtpLastMessageSend++;

                        } catch (Exception ex) { Global.Instance.Log.Add(nameof(MimeMessage), ex); }
                    }

                    try {
                        client.Connected -= Client_Connected;
                        client.MessageSent -= Client_MessageSent;
                        client.Disconnected -= Client_Disconnected;
                        client.Authenticated -= Client_Authenticated;
                    } catch { }

                    await client.DisconnectAsync(true);
                    OnCallEvent(MailEventId.DeliverySendMessage, nameof(Send));
                    return true;
                }
                catch (OperationCanceledException) { Global.Instance.Log.Add(nameof(Send), $"cancell Smtp client for {user}, close"); }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Send), ex); }
                finally { account.CurrentAction = AccountUsing.None; }
                return false;
            });
        #endregion
    }
}

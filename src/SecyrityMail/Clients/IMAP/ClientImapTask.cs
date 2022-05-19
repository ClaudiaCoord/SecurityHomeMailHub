/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using SecyrityMail.Data;
using SecyrityMail.MailAccounts;
using SecyrityMail.Messages;
using SecyrityMail.Messages.Bouncer;
using SecyrityMail.Utils;

namespace SecyrityMail.Clients.IMAP
{
    internal class ClientImapTask : ClientEvent
    {
        string user = string.Empty;
        readonly InitClientSession session;
        readonly EventHandler<EventActionArgs> evRoot;
        readonly MessagesCacheOpener cacheOpener;

        public ClientImapTask(InitClientSession s, EventHandler<EventActionArgs> a) {
            LogTag = nameof(ClientImap);
            session = s; evRoot = a;
            this.SubscribeProxyEvent(evRoot);
            cacheOpener = CacheOpener.Build(this.GetType());
        }
        ~ClientImapTask() {
            Global.Instance.Log.Add(nameof(ClientImapTask), $"Imap client for {user} end");
            this.UnSubscribeProxyEvent(evRoot);
        }

        #region Imap Receive
        public async Task<bool> Receive(UserAccount account, string rootpath, TokenSafe token) =>
            await Task.Run(async () => {
                try {
                    if (account.IsEmptyImapReceive)
                        throw new Exception($"Mail Account not complette to Imap {nameof(Receive)}");
                    account.CurrentAction = AccountUsing.Imap;
                    user = account.Email;

                    using ImapClient client = (ImapClient)await session.InitClient(
                        (a) => ClientImap.Create(a), account, rootpath, token);
                    if (client == default)
                        throw new Exception($"error initialize Imap client from proxy list {Global.Instance.ProxyList.ProxyType}");

                    try {
                        client.Alert += Client_Alert;
                        client.Connected += Client_Connected;
                        client.Disconnected += Client_Disconnected;
                        client.MetadataChanged += Client_MetadataChanged;
                    } catch { }

                    IMailFolder inbox = client.Inbox;
                    inbox.Open(Global.Instance.Config.IsImapClientMessagePurge ? FolderAccess.ReadWrite : FolderAccess.ReadOnly);

                    Global.Instance.ImapClientStat.ImapLastMessageReceive = inbox.Count;
                    Global.Instance.ImapClientStat.ImapLastMessageRecent = inbox.Recent;

                    MailMessages msgs = await cacheOpener.Open(account.Email)
                                                         .ConfigureAwait(false);

                    for (int i = 0; i < inbox.Count; i++) {

                        token.ThrowIfCancellationRequested();

                        try {
                            MimeMessage mmsg = inbox.GetMessage(i);
                            if (mmsg == null)
                                continue;

                            BounceDetectResult r = BounceDetector.Detect(mmsg);
                            Global.DirectoryPlace place = r.IsBounce ? Global.DirectoryPlace.Bounced : Global.DirectoryPlace.Msg;
                            if (r.IsBounce)
                                Global.Instance.Log.Add(nameof(MailMessage), r.CombinedStatus.ToString());

                            MailMessage msg = await new MailMessage()
                                                .CreateAndDelivery(place, mmsg, rootpath, msgs.Count + 1, account.IsPgpAutoDecrypt)
                                                .ConfigureAwait(false);
                            if (msg == null)
                                continue;
                            msgs.Add(msg);

                        } catch (Exception ex) { Global.Instance.Log.Add(nameof(MailMessage), ex); }

                        if (Global.Instance.Config.IsImapClientMessagePurge)
                            inbox.AddFlags(i, MessageFlags.Deleted, true, token.GetToken);
                    }
                    if (Global.Instance.Config.IsImapClientMessagePurge) {
                        Global.Instance.ImapClientStat.ImapLastMessageDelete = inbox.Count;
                        inbox.Expunge(token.GetToken);
                    }

                    try {
                        client.Alert -= Client_Alert;
                        client.Connected -= Client_Connected;
                        client.Disconnected -= Client_Disconnected;
                        client.MetadataChanged -= Client_MetadataChanged;
                    } catch { }

                    await client.DisconnectAsync(true, token.GetToken);
                    OnCallEvent(MailEventId.DeliveryInMessage, nameof(Receive));
                    return true;
                }
                catch (OperationCanceledException) { Global.Instance.Log.Add(nameof(Receive), $"cancell Imap client for {user}, close"); }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Receive), ex); }
                finally {
                    account.CurrentAction = AccountUsing.None;
                    await cacheOpener.Close(account.Email).ConfigureAwait(false);
                }
                return false;
            });
        #endregion
    }
}

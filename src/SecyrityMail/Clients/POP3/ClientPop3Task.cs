
using System;
using System.Threading.Tasks;
using MailKit.Net.Pop3;
using MimeKit;
using SecyrityMail.Clients.IMAP;
using SecyrityMail.Data;
using SecyrityMail.MailAccounts;
using SecyrityMail.Messages;
using SecyrityMail.Messages.Bouncer;
using SecyrityMail.Utils;

namespace SecyrityMail.Clients.POP3
{
    internal class ClientPop3Task : ClientEvent
    {
        string user = string.Empty;
        readonly InitClientSession session;
        readonly EventHandler<EventActionArgs> evRoot;

        public ClientPop3Task(InitClientSession s, EventHandler<EventActionArgs> a) {
            LogTag = nameof(ClientPop3);
            session = s; evRoot = a;
            this.SubscribeProxyEvent(evRoot);
        }
        ~ClientPop3Task() {
            Global.Instance.Log.Add(nameof(ClientPop3Task), $"Pop3 client for {user} end");
            this.UnSubscribeProxyEvent(evRoot);
        }

        #region POP3 Receive
        public async Task<bool> Receive(UserAccount account, string rootpath, TokenSafe token) =>
            await Task.Run(async () => {
                try
                {
                    if (account.IsEmptyPop3Receive)
                        throw new Exception($"Mail Account not complette to Pop3 {nameof(Receive)}");
                    account.CurrentAction = AccountUsing.Pop3;
                    user = account.Email;

                    using Pop3Client client = (Pop3Client)await session.InitClient(
                        (a) => ClientPop3.Create(a), account, rootpath, token);
                    if (client == default)
                        throw new Exception($"error initialize Pop3 client from proxy list {Global.Instance.ProxyList.ProxyType}");

                    try {
                        client.Authenticated += Client_Authenticated;
                        client.Connected += Client_Connected;
                        client.Disconnected += Client_Disconnected;
                    } catch { }

                    MailMessages msgs = await Global.Instance.MessagesManager.Open(account.Email)
                                        .ConfigureAwait(false);

                    Global.Instance.Pop3ClientStat.Pop3LastMessageTotal += client.Count;
                    for (int i = 0; i < client.Count; i++) {

                        token.ThrowIfCancellationRequested();

                        try {
                            MimeMessage mmsg = client.GetMessage(i);
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
                            Global.Instance.Pop3ClientStat.Pop3LastMessageReceive++;

                        } catch (Exception ex) { Global.Instance.Log.Add(nameof(MailMessage), ex); }

                        client.DeleteMessage(i, token.GetToken);
                        Global.Instance.Pop3ClientStat.Pop3LastMessageDelete++;
                    }

                    try {
                        client.Authenticated -= Client_Authenticated;
                        client.Connected -= Client_Connected;
                        client.Disconnected -= Client_Disconnected;
                    }
                    catch { }

                    await client.DisconnectAsync(true);
                    OnCallEvent(MailEventId.DeliveryInMessage, nameof(Receive));
                    return true;
                }
                catch (OperationCanceledException) { Global.Instance.Log.Add(nameof(Receive), $"cancell Pop3 client for {user}, close"); }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Receive), ex); }
                finally {
                    account.CurrentAction = AccountUsing.None;
                    Global.Instance.MessagesManager.Close(account.Email);
                }
                return false;
            });
        #endregion
    }
}

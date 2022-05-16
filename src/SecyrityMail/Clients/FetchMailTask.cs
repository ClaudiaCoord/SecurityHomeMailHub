
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SecyrityMail.Clients.IMAP;
using SecyrityMail.Clients.POP3;
using SecyrityMail.Clients.SMTP;
using SecyrityMail.Data;
using SecyrityMail.MailAccounts;
using SecyrityMail.Proxy;
using SecyrityMail.Utils;

namespace SecyrityMail.Clients
{
    public class FetchMailTask : MailEvent, IMailEventProxy, IAutoInit, IDisposable
    {
        private const string Tag = "Mail Check";

        volatile Timer timer = default;
        Thread mainThread = default;
        CancellationTokenSafe cancellation = new();
        MailEventId eventId = MailEventId.None;
        TimeSpan checkMailPeriod = Timeout.InfiniteTimeSpan;
        bool isReceiveOnSendOnly = false;
        private EventHandler<EventActionArgs> eventProxy;
        private EventHandler<EventActionArgs> eventMain;

        public TimeSpan CheckMailPeriod { get => checkMailPeriod; set { checkMailPeriod = value; OnPropertyChanged(); } }
        public bool IsReceiveOnSendOnly { get => isReceiveOnSendOnly; set { isReceiveOnSendOnly = value; OnPropertyChanged(); } }
        public bool IsCheckMailRun => mainThread != default;
        public MailEventId ServicesEventId {
            get => eventId;
            set {
                eventId = value;
                if (value == MailEventId.DeliveryOutMessage) {
                    if ((mainThread != default) && mainThread.IsAlive)
                        return;
                    OnPropertyChanged();
                    Run_();
                }
            }
        }
        public CancellationToken Token {
            get => cancellation.GetExtendedCancellationToken();
            set => cancellation.SetExtendedCancellationToken(value);
        }

        public FetchMailTask() {
            timer = new(TimerCb, default, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            eventProxy = new(Child_ProxyEventCb);
            eventMain = new(Global_EventCb);
        }
        ~FetchMailTask() => Dispose();
        public async Task AutoInit() { Global.Instance.EventCb += eventMain; _ = await Task.FromResult(true); }

        #region events
        private void TimerCb(object _)
        {
            if (mainThread != default)
                return;
            if ((Global.Instance.Config.ProxyType != ProxyType.None) && Global.Instance.Config.IsProxyCheckRun)
                return;
            Global.Instance.Log.Add(nameof(TimerCb), "running a mail task from the scheduler");
            Run_();
        }
        private void Global_EventCb(object sender, EventActionArgs a)
        {
            if (Global.Instance.Config.IsNewMessageSendImmediately && a.IsDeliveryOutMessage()) {
                Global.Instance.Log.Add(nameof(Global_EventCb), "new outgoing messages, start mail task");
                TimerCb(default);
            }
        }
        private void Child_ProxyEventCb(object _, EventActionArgs args) =>
            OnProxyEvent(args);
        #endregion

        public void Stop()
        {
            if (timer == default)
                return;

            timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            DisposeThread();
            OnPropertyChanged(nameof(Stop));
        }
        public void Start()
        {
            if (timer == default)
                return;

            DisposeThread();
#           if RELEASE
            if (CheckMailPeriod == Timeout.InfiniteTimeSpan)
                return;
            timer.Change(TimeSpan.FromMinutes(1.0), CheckMailPeriod);
#           endif
            OnPropertyChanged(nameof(Start));
        }
        
        #region Run
        public async Task<bool> Run() =>
            await Task.Run(() => {
                Run_();
                return true;
            });

        private void Run_()
        {
            if (IsCheckMailRun)
                return;

            cancellation.CheckExtendedCancellationToken();
            cancellation.Reload();
            mainThread = new Thread(async () =>
            {
                bool isvpn = false,
                     isvpnrun = false,
                     isproxylist = false,
                     isproxyssh = false,
                     isproxysshrun = false;
                try {
                    OnCallEvent(MailEventId.StartFetchMail, nameof(FetchMailTask), default);
                    Global.Instance.Log.Add(Tag, "begin all clients remote requests");

                    TokenSafe token = cancellation.TokenSafe;
                    InitClientSession session = new();
                    ProxyType proxyType = Global.Instance.Config.ProxyType;
                    isvpn = Global.Instance.Config.IsVpnEnable;
                    isvpnrun = Global.Instance.Config.IsVpnTunnelRunning;
                    isproxylist = Global.Instance.Config.ProxyType != ProxyType.None;
                    isproxyssh = Global.Instance.Config.ProxyType == ProxyType.SshSock4 || Global.Instance.Config.ProxyType == ProxyType.SshSock5;
                    isproxysshrun = Global.Instance.Config.IsSshRunning;
                    isproxylist = !isproxyssh && isproxylist;

                    if (!isvpn && Global.Instance.Config.IsVpnAlways)
                        throw new Exception($"Not selected compatible VPN account, abort");

                    if (isproxyssh && !isproxysshrun) {
                        if (Global.Instance.Proxy.SshProxy.IsEmpty)
                            throw new Exception($"SSH proxyes Accounts is empty, abort");

                        await Global.Instance.Proxy.SshProxy.RandomSelect(proxyType).ConfigureAwait(false);
                        if (!Global.Instance.Proxy.SshProxy.IsAccountSelected)
                            throw new Exception($"SSH Account not auto selected, abort");

                        if (Global.Instance.Proxy.SshProxy.AccountSelected.Type != proxyType)
                            throw new Exception($"SSH Account type not equals: {proxyType} -> {Global.Instance.Proxy.SshProxy.AccountSelected.Type}, abort");

                        if (Global.Instance.Proxy.SshProxy.IsExpired)
                            throw new Exception($"SSH Account is expired, abort");

                    } else if (isproxylist) {

                        bool isnotupdate = false;
                        ProxyListConverter plc = new();
                        if (Global.Instance.Config.IsProxyListRepack) {
                            isnotupdate = await plc.CheckBuild(Global.Instance.Config.ProxyType).ConfigureAwait(false);
                        }
                        if (!isnotupdate)
                            _ = await plc.AllBuild().ConfigureAwait(false);
                        Global.Instance.ProxyList.Clear();
                    }

                    Global.Instance.SmtpClientStat.Reset();
                    Global.Instance.Pop3ClientStat.Reset();
                    Global.Instance.ImapClientStat.Reset();
                    Global.Instance.ProxyList.ProxyType = Global.Instance.Config.ProxyType;

                    if (isvpn) {
                        if (!isvpnrun) {
                            if (Global.Instance.Config.IsVpnRandom || !Global.Instance.VpnAccounts.IsAccountSelected)
                                _ = await Global.Instance.VpnAccounts.RandomSelect().ConfigureAwait(false);
                            Global.Instance.Vpn.Begin();
                        }
                        bool b = await Global.Instance.Vpn.VpnWaiter().ConfigureAwait(false);
                        if (!b) return;
                    }

                    foreach (UserAccount a in Global.Instance.Accounts.Items) {

                        if (a.IsEmpty || !a.Enable) continue;
                        Global.Instance.Log.Add(Tag, $"start Pop3/Imap/Smtp client '{a.Email}' request");
                        string rootpath = Global.GetUserDirectory(a.Email);

                        token.ThrowIfCancellationRequested();
                        if (!a.IsEmptySend) {
                            OnCallEvent(MailEventId.BeginCall, "SMTP-OUT");
                            if (isvpn) {
                                bool b = await Global.Instance.Vpn.VpnWaiter().ConfigureAwait(false);
                                if (!b) return;
                            }
                            try {
                                int idx = 0;
                                try {
                                    idx = Directory.GetFiles(
                                        Global.AppendPartDirectory(
                                            rootpath, Global.DirectoryPlace.Out)).Length;
                                } catch { }
                                if (idx > 0) {
                                    SmtpClientStat stat = Global.Instance.SmtpClientStat;
                                    ClientSmtpTask clientSmtp = new(session, eventProxy);
                                    bool b = await clientSmtp.Send(a, rootpath, token).ConfigureAwait(false);
                                    if (!b) stat.SmtpLastMessageTotal = idx;
                                    Global.Instance.Log.Add(
                                        Tag,
                                        $"Smtp client return:{SrtringStatus(b)}, out:{stat.SmtpLastMessageTotal}, send:{stat.SmtpLastMessageSend}");
                                } else {
                                    Global.Instance.Log.Add(
                                        Tag,
                                        $"not outgoing mail to Smtp client:{a.Email}, next..");
                                }
                            } catch (Exception ex) { Global.Instance.Log.Add(nameof(ClientSmtp), ex); }
                            OnCallEvent(MailEventId.EndCall, "SMTP-OUT");
                        } else {
                            Global.Instance.Log.Add(Tag, $"not found Smtp/Send credentials for account '{a.Email}'");
                        }

                        token.ThrowIfCancellationRequested();
                        if (!a.IsEmptyPop3Receive) {
                            OnCallEvent(MailEventId.BeginCall, "POP3-OUT");
                            if (isvpn) {
                                bool b = await Global.Instance.Vpn.VpnWaiter().ConfigureAwait(false);
                                if (!b) return;
                            }
                            try {
                                Pop3ClientStat stat = Global.Instance.Pop3ClientStat;
                                ClientPop3Task clientPop3 = new(session, eventProxy);
                                bool b = await clientPop3.Receive(a, rootpath, token).ConfigureAwait(false);
                                Global.Instance.Log.Add(
                                    Tag,
                                    $"Pop3 client return:{SrtringStatus(b)}, receive:{stat.Pop3LastMessageReceive}, deleted:{stat.Pop3LastMessageDelete}");
                            } catch (Exception ex) { Global.Instance.Log.Add(nameof(ClientPop3), ex); }
                            OnCallEvent(MailEventId.EndCall, "POP3-OUT");
                        } else {
                            Global.Instance.Log.Add(Tag, $"not found Pop3/Receive credentials for account '{a.Email}'");
                        }

                        token.ThrowIfCancellationRequested();
                        if (!a.IsEmptyImapReceive) {
                            OnCallEvent(MailEventId.BeginCall, "IMAP-OUT");
                            if (isvpn) {
                                bool b = await Global.Instance.Vpn.VpnWaiter().ConfigureAwait(false);
                                if (!b) return;
                            }
                            try {
                                ImapClientStat stat = Global.Instance.ImapClientStat;
                                ClientImapTask clientImap = new(session, eventProxy);
                                bool b = await clientImap.Receive(a, rootpath, token).ConfigureAwait(false);
                                Global.Instance.Log.Add(
                                    Tag,
                                    $"Imap client return:{SrtringStatus(b)}, receive:{stat.ImapLastMessageReceive}, recent:{stat.ImapLastMessageReceive}, deleted:{stat.ImapLastMessageDelete}");
                            } catch (Exception ex) { Global.Instance.Log.Add(nameof(ClientImap), ex); }
                            OnCallEvent(MailEventId.EndCall, "IMAP-OUT");
                        } else {
                            Global.Instance.Log.Add(Tag, $"not found Imap/Receive credentials for account '{a.Email}'");
                        }

                        try {
                            foreach(var path in Directory.GetFiles(Global.AppendPartDirectory(rootpath, Global.DirectoryPlace.Log))) {
                                FileInfo f = new(path);
                                if ((f != default) && f.Exists && (f.Length == 0))
                                    f.Delete();
                            }
                        } catch { }
                    }
                }
                catch (OperationCanceledException) { Global.Instance.Log.Add(nameof(FetchMailTask), "cancell client sessions, close"); }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(FetchMailTask), ex); }
                finally {
                    if (isvpn && !isvpnrun)
                        Global.Instance.Vpn.End();
                    if (isproxyssh && !isproxysshrun)
                        Global.Instance.SshProxy.ProxySshDispose();
                    else if (isproxylist) {
                        if (Global.Instance.Config.IsProxyListRepack)
                            await Global.Instance.Proxy.MergeProxyes().ConfigureAwait(false);
                    }
                    OnCallEvent(MailEventId.StopFetchMail, nameof(FetchMailTask), default);
                    DisposeThread();
                }
            });
            mainThread.Name = nameof(FetchMailTask);
            mainThread.IsBackground = true;
            mainThread.Start();
            OnPropertyChanged(nameof(FetchMailTask));
        }
        #endregion

        public void Dispose()
        {
            Timer t = timer;
            timer = default;
            if (t != default)
                t.Dispose();
            DisposeThread();
        }
        private void DisposeThread()
        {
            if (!cancellation.IsCancellationRequested)
                cancellation.Cancel();
            cancellation.Clear();

            Thread th = mainThread;
            mainThread = default;
            if ((th != default) && th.IsAlive)
                th.Join();
        }

        private string SrtringStatus(bool b) => b ? "OK" : "ERROR";
    }
}

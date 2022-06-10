/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.IO;

using System.Threading;
using System.Threading.Tasks;
using SecyrityMail.Data;
using SecyrityMail.MailFilters;
using SecyrityMail.Utils;
using VPN.WireGuard;

namespace SecyrityMail.Vpn
{
    public class VpnEngine : MailEvent, IMailEventProxy, IAutoInit, IDisposable
    {
        public const string VpnTag = "VPN";
        public const string LogTag = "VPN Log";
        public const string TunnelTag = "VPN Tunnel";

        private string GetTag(string s) => $"{VpnTag} {s}";

        private EventHandler<EventActionArgs> eventMain;
        private CancellationTokenSafe cancellation = new();
        private Thread threadTunnel = null;

        private bool isEnableLogVpn = true,
                     isVpnReady = false,
                     isVpnBegin = false,
                     isVpnRandom = true;
        private long tunnelRunning = 0L;

        public CancellationToken Token {
            get => cancellation.GetExtendedCancellationToken();
            set => cancellation.SetExtendedCancellationToken(value);
        }
        public bool IsVpnRandom {
            get => isVpnRandom;
            set { isVpnRandom = value; OnPropertyChanged(); }
        }
        public bool IsEnableLogVpn {
            get => isEnableLogVpn;
            set { isEnableLogVpn = value; OnPropertyChanged(); }
        }
        public bool IsVpnBegin {
            get => isVpnBegin;
            set { isVpnBegin = value; OnPropertyChanged(); }
        }
        public bool IsVpnReady {
            get => isVpnReady;
            set { isVpnReady = value; OnPropertyChanged(); }
        }
        public bool IsTunnelRunning {
            get => Interlocked.Read(ref tunnelRunning) != 0L;
            set { Interlocked.Exchange(ref tunnelRunning, value ? 1L : 0L); OnPropertyChanged(); }
        }

        #region RX/TX
        private ulong tunnelURx { set => TunnelRx = (long)value; }
        private ulong tunnelUTx { set => TunnelTx = (long)value; }
        private  long tunnelRx = 0U;
        private  long tunnelTx = 0U;

        public long TunnelRx {
            get => Interlocked.Read(ref tunnelRx);
            private set { Interlocked.Exchange(ref tunnelRx, value); OnCallEvent(MailEventId.PropertyChanged, nameof(TunnelRx), value); }
        }
        public long TunnelTx {
            get => Interlocked.Read(ref tunnelTx);
            private set { Interlocked.Exchange(ref tunnelTx, value); OnCallEvent(MailEventId.PropertyChanged, nameof(TunnelTx), value); }
        }
        #endregion

        public VpnAccounts VpnAccounts { get; } = new();

        public VpnEngine() => eventMain = new(Global_EventCb);
        ~VpnEngine() => Dispose();

        private void Global_EventCb(object sender, EventActionArgs a)
        {
            if (!a.IsPropertyChanged())
                return;
            if (a.IsTypeOf<VpnEngine>())
                switch (a.Text) {
                    case "VPNReady": IsVpnReady = true; break;
                    case "VPNKeypair": IsVpnBegin = true; break;
                    case "VPNStarting":
                    case "VPNShutdown": IsVpnReady = isVpnBegin = false; break;
                    default: break;
                }
        }

        public void Dispose()
        {
            if (!cancellation.IsDisposed) {
                cancellation.Cancel();
                cancellation.Dispose();
            }
            Thread t = threadTunnel;
            threadTunnel = null;
            if (t != null) {
                if (t.ThreadState != ThreadState.Unstarted)
                    try { t.Join(TimeSpan.FromSeconds(10)); } catch { }
            }
        }

        #region Begin
        public void Begin()
        {
            if (IsTunnelRunning)
                return;
            IsTunnelRunning = true;

            Dispose();
            cancellation.CheckExtendedCancellationToken();
            cancellation.Reload();

            tunnelRx = 0U;
            tunnelTx = 0U;
            OnCallEvent(MailEventId.PropertyChanged, nameof(TunnelRx), TunnelRx);
            OnCallEvent(MailEventId.PropertyChanged, nameof(TunnelTx), TunnelTx);

            threadTunnel = new(async () => {

                FileInfo fcnf = default(FileInfo),
                         flog = default(FileInfo);
                Vpnlogger log = default(Vpnlogger);
                TokenSafe token = cancellation.TokenSafe;
                uint cursor = Vpnlogger.CursorAll;
                try {
                    {
                        VpnAccount account = VpnAccounts.AccountSelected;
                        if ((account == default) || account.IsEmpty)
                            throw new Exception("VPN account not selected");

                        string path = await account.Export();
                        if (string.IsNullOrEmpty(path))
                            throw new Exception("error get VPN configuration");

                        fcnf = new FileInfo(path);
                        if ((fcnf == default) || !fcnf.Exists)
                            throw new FileNotFoundException(path);

                        flog = new FileInfo(Global.GetRootFile(Global.DirectoryPlace.Vpn, "log.bin"));
                        if (flog == default)
                            throw new FileLoadException(flog.FullName);

                        Global.Instance.Log.Add(LogTag, $"start tunnel logging ({account.Name})");
                    }

                    token.ThrowIfCancellationRequested();

                    log = new(flog.FullName);
                    VpnService.Add(fcnf.FullName, true);
                    VpnDriver.VpnAdapter adapter = null;

                    while (IsTunnelRunning) {

                        token.ThrowIfCancellationRequested();

                        try {
                            List<string> list = log.FollowFromCursor(ref cursor);
                            foreach (string s in list) {
                                if (s.EndsWith("Startup complete"))
                                    OnPropertyChanged("VPNReady");
                                else if (s.EndsWith("Shutting down"))
                                    OnPropertyChanged("VPNShutdown");
                                else if (s.StartsWith("Keypair") && s.Contains("created for peer"))
                                    OnPropertyChanged("VPNKeypair");
                                else if (s.StartsWith("Starting WireGuard"))
                                    OnPropertyChanged("VPNStarting");

                                if (IsEnableLogVpn)
                                    Global.Instance.Log.Add(VpnTag, s);
                            }
                        } catch { await Task.Delay(150); }

                        if (adapter == null) {
                            try { adapter = VpnService.GetAdapter(fcnf.FullName); }
                            catch { await Task.Delay(300); }
                            if (adapter == null)
                                continue;
                        }
                        try {
                            VpnDriver.VpnAdapter.VpnStat stat = adapter.GetStatistic();
                            if (stat != null) {
                                tunnelURx = stat.RxBytes;
                                tunnelUTx = stat.TxBytes;
                            }
                            await Task.Delay(300);
                        }
                        catch (Exception ex) {
                            adapter = default;
                            Global.Instance.Log.Add(TunnelTag, ex);
                        }
                    }
                    Global.Instance.Log.Add(LogTag, "end tunnel logging");
                }
                catch (OperationCanceledException) { Global.Instance.Log.Add(TunnelTag, "cancell tunnel logging, close"); }
                catch (Exception ex) { Global.Instance.Log.Add(TunnelTag, ex); }
                finally {
                    if (fcnf != default) {
                        fcnf.Refresh();
                        try { VpnService.Remove(fcnf.FullName, true); } catch { }
                        if (fcnf.Exists)
                            try { fcnf.Delete(); } catch { }
                    }
                    if (log != default)
                        try { log.Dispose(); } catch { }
                    if (flog != default) {
                        flog.Refresh();
                        if (flog.Exists)
                            try { flog.Delete(); } catch { }
                    }
                    OnCallEvent(MailEventId.PropertyChanged, nameof(TunnelRx), TunnelRx);
                    OnCallEvent(MailEventId.PropertyChanged, nameof(TunnelTx), TunnelTx);
                    Dispose();
                    Global.Instance.Log.Add(LogTag, $"exit tunnel, session RX:{TunnelRx.Humanize()}, TX:{TunnelTx.Humanize()}");
                    IsVpnReady =
                    IsVpnBegin =
                    IsTunnelRunning = false;
                }
            });
            threadTunnel.Start();
        }
        #endregion

        public void End() {
            if (!IsTunnelRunning && threadTunnel == null)
                return;
            Dispose();
        }

        public async Task Start() =>
            await Task.Run(() => {
                try {
                    Begin();
                    OnCallEvent(
                        MailEventId.BeginCall,
                        $"{GetTag(nameof(Start))}/{VpnAccounts.AccountSelected.Name}");
                    Global.Instance.Log.Add(GetTag(nameof(Start)), $"start global {TunnelTag} ({VpnAccounts.AccountSelected.Name})");
                }
                catch (Exception ex) { Global.Instance.Log.Add(GetTag(nameof(Start)), ex); }
            });

        public void Stop() {
            try {
                End();
                OnCallEvent(
                    MailEventId.EndCall,
                    $"{GetTag(nameof(Stop))}/{VpnAccounts.AccountSelected.Name}");
                Global.Instance.Log.Add(GetTag(nameof(Stop)), $"stopping global {TunnelTag}");
            } catch (Exception ex) { Global.Instance.Log.Add(GetTag(nameof(Stop)), ex); }
        }


        public async Task AutoInit() {
            _ = await VpnAccounts.Load().ConfigureAwait(false);
            if (VpnAccounts.Count > 0)
                _ = await VpnAccounts.RandomSelect().ConfigureAwait(false);
            Global.Instance.EventCb += eventMain;
        }

        public async Task<bool> VpnWaiter(bool b = false) {
            try {
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                while (!IsVpnReady) {
                    await Task.Delay(25).ConfigureAwait(false);
                    if (cts.IsCancellationRequested)
                        break;
                }
                cts.Dispose();
                if (!IsVpnReady) {
                    if (IsVpnBegin && !b)
                        return await VpnWaiter(true).ConfigureAwait(false);
                    Global.Instance.Log.Add(nameof(VpnWaiter), "Vpn tunnel timeout exceeded, cancel all tasks");
                    return false;
                }
                return await VpnAccounts.Dns.CheckRoute(Global.Instance.Config.ForbidenRouteList);
            }
            catch { return false; }
        }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SecyrityMail.Clients;
using SecyrityMail.Clients.IMAP;
using SecyrityMail.Clients.POP3;
using SecyrityMail.Clients.SMTP;
using SecyrityMail.Data;
using SecyrityMail.IPFilters;
using SecyrityMail.MailAccounts;
using SecyrityMail.MailAddress;
using SecyrityMail.Messages;
using SecyrityMail.Proxy;
using SecyrityMail.Proxy.SshProxy;
using SecyrityMail.Servers.POP3;
using SecyrityMail.Servers.SMTP;
using SecyrityMail.Utils;
using SecyrityMail.Vpn;

namespace SecyrityMail
{
    public class Global : MailEvent
    {
        private static Global globs = default(Global);
        public static Global Instance {
            get {
                if (globs == default)
                    globs = new Global();
                return globs;
            }
        }
        ~Global() => DeInit();
        static Global() {
            _ = Global.Instance;
            Global.Instance.FindAutoInit();
        }
        private Global() => eventProxy = new EventHandler<EventActionArgs>(Child_ProxyEventCb);
        private EventHandler<EventActionArgs> eventProxy;
        private void Child_ProxyEventCb(object _, EventActionArgs args) =>
            OnProxyEvent(args);

        private static CancellationTokenSafe cancellationPop3 = new();
        private static CancellationTokenSafe cancellationSmtp = new();

        #region Init
        public async void Init(CancellationToken ct) {

            IPAddress ipa = default;
            do {
                if (!string.IsNullOrWhiteSpace(Config.ServicesInterfaceName)) {
                    if ((ipa = Config.ServicesInterfaceName.GetInterfaceIp(AddressFamily.InterNetwork)) == null)
                        throw new Exception($"Network Interface adapter {Config.ServicesInterfaceName} not found in system, or shutdown!");
                }
                if (ipa != null) break;
                if (!string.IsNullOrWhiteSpace(Config.ServicesInterfaceIp)) {
                    if (!Config.ServicesInterfaceIp.Contains("*")) {
                        if ((ipa = Config.ServicesInterfaceIp.CheckInterfaceIp()) == null)
                            throw new Exception($"IP address {Config.ServicesInterfaceIp} not found in system, or shutdown!");
                    }
                }
                if (ipa != null) break;
                ipa = IPAddress.Any;

            } while (false);

            cancellationPop3.SetExtendedCancellationToken(ct);
            cancellationSmtp.SetExtendedCancellationToken(ct);

            Pop3Service = new(Config.Pop3ServicePort, ipa, cancellationPop3);
            SmtpService = new(Config.SmtpServicePort, ipa, cancellationSmtp);
            Pop3Service.EventCb += ToMainEvent;
            SmtpService.EventCb += ToMainEvent;
            Vpn.Token = ct;
            Tasks.Token = ct;
            if (Config.IsProxyListRepack && (ProxyList.ProxyType != ProxyType.None))
                _ = await Proxy.CheckProxyes(ct).ConfigureAwait(false);
        }
        public void AsDispose(object obj) {
            try {
                if (IsIDisposableObject(obj.GetType(), obj) && (obj is IDisposable d)) {
                    try { d.Dispose(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"{nameof(AsDispose)}: {ex}"); }
                }
            } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"{nameof(AsDispose)}: {ex}"); }
        }
        #endregion

        #region ToMainEvent
        public void ToMainEvent(MailEventId id, string s, object obj) =>
            OnCallEvent(id, s, obj);
        public void ToMainEvent(object obj, EventActionArgs args) =>
            OnProxyEvent(obj, args);
        #endregion

        #region DeInit
        public void DeInit() {
            Pop3Service.EventCb -= ToMainEvent;
            SmtpService.EventCb -= ToMainEvent;
            cancellationPop3.Cancel();
            cancellationPop3.Dispose();
            cancellationSmtp.Cancel();
            cancellationSmtp.Dispose();
            IpAddressInfo.Dispose();
            FindAutoDispose();
        }
        #endregion

        public void Start() {
            if (Config.IsPop3Enable) Pop3Service.Start();
            if (Config.IsSmtpEnable) SmtpService.Start();
            Tasks.Start();
        }

        public void Wait() {
            Pop3Service.Wait();
            SmtpService.Wait();
        }

        public void Stop() {
            cancellationPop3.Cancel();
            Pop3Service.Stop();
            cancellationSmtp.Cancel();
            SmtpService.Stop();
            Tasks.Stop();
            cancellationPop3.Reload();
            cancellationSmtp.Reload();
        }

        #region Mail Services
        public void StartPop3Service() => Pop3Service.Start();
        public void StartSmtpService() => SmtpService.Start();
        public void StopPop3Service() { cancellationPop3.Cancel(); Pop3Service.Stop(); cancellationPop3.Reload(); }
        public void StopSmtpService() { cancellationSmtp.Cancel(); SmtpService.Stop(); cancellationSmtp.Reload(); }

        Pop3Server Pop3Service = default;
        SmtpServer SmtpService = default;

        public bool IsPop3Run => (Pop3Service != default) && Pop3Service.IsServiceRun;
        public bool IsSmtpRun => (SmtpService != default) && SmtpService.IsServiceRun;
        #endregion

        #region Log
        public MailLog Log { get; } = new();
        #endregion

        #region Config
        public IConfiguration Config { get; } = new Configuration();
        #endregion

        #region IP filters
        public ForbidenIp ForbidenAccessIp { get; } = new();
        #endregion

        #region Proxy
        public MailProxy Proxy { get; } = new();
        public ProxyList ProxyList => Proxy.ProxyList;
        #endregion

        #region SSH
        public SshAccounts SshProxy => Proxy.SshProxy;
        #endregion

        #region VPN
        public VpnEngine Vpn { get; } = new();
        public VpnAccounts VpnAccounts {
            get => Vpn.VpnAccounts;
            set { Vpn.VpnAccounts.Copy(value); OnPropertyChanged(); }
        }
        #endregion

        #region Mail Task/Manager
        public FetchMailTask Tasks { get; } = new();
        public MailMessagesManager MessagesManager { get; } = new();
        public ImapClientStat ImapClientStat { get; } = new();
        public Pop3ClientStat Pop3ClientStat { get; } = new();
        public SmtpClientStat SmtpClientStat { get; } = new();
        #endregion

        #region Mail Accounts
        private UserAccounts accounts = new();
        public UserAccounts Accounts {
            get { return accounts; }
            set { accounts.Copy(value); }
        }
        public UserAccount FindAccount(string login) => accounts.Find(login);
        #endregion

        #region all Accounts backup/restore
        public async Task<bool> AccountsSave() => await AccountsSave_(false).ConfigureAwait(false);
        public async Task<bool> AccountsBackup() => await AccountsSave_(true).ConfigureAwait(false);
        public async Task<bool> AccountsLoad() => await AccountsLoad_(false).ConfigureAwait(false);
        public async Task<bool> AccountsRestore() => await AccountsLoad_(true).ConfigureAwait(false);

        private async Task<bool> AccountsSave_(bool b) =>
            await Task.Run(async () => {
                try {
                    _ = await Accounts.Save(b).ConfigureAwait(false);
                    _ = await SshProxy.Save(b).ConfigureAwait(false);
                    _ = await VpnAccounts.Save(b).ConfigureAwait(false);
                }
                catch (Exception ex) { Log.Add(nameof(AccountsBackup), ex); }
                return true;
            });

        private async Task<bool> AccountsLoad_(bool b) =>
            await Task.Run(async () => {
                try {
                    _ = await Accounts.Load(b).ConfigureAwait(false);
                    _ = await SshProxy.Load(b).ConfigureAwait(false);
                    _ = await SshProxy.RandomSelect().ConfigureAwait(false);
                    _ = await VpnAccounts.Load(b).ConfigureAwait(false);
                    _ = await VpnAccounts.RandomSelect().ConfigureAwait(false);
                }
                catch (Exception ex) { Log.Add(nameof(AccountsRestore), ex); }
                return true;
            });

        #endregion

        #region Addresses Book
        public AddressesBook EmailAddresses { get; } = new();
        #endregion

        #region Paths
        const string rootacc = "Accounts";
        public enum DirectoryPlace : int
        {
            Msg = 0,    // Msg -> read message directory            LOCAL*
            Bounced,    // Bounced -> return undelivery message     LOCAL*
            Out,        // Out -> Send (send remote and archive)    OUT*
            Send,       // Send -> (sending remote and archivied)   LOCAL*
            Attach,     // Nothing =?                               -
            Error,      // All error delivery/move                  LOCAL*
            Log,        // Log ditectory                            LOCAL*
            Root,       // Root directory
            Proxy,      // Proxy configuration directory
            Vpn,        // VPN configuration directory
            None        // -*-
        }

        public static string GetValidFileName(string name) =>
            Path.GetInvalidFileNameChars().Aggregate(name, (current, c) => current.Replace(c.ToString(), "_"));

        public static string GetValidDirectory(string name) =>
            Path.GetInvalidPathChars().Aggregate(name, (current, c) => current.Replace(c.ToString(), "_"));

        #region Append Part Directory
        public static string AppendPartDirectory(string path, DirectoryPlace place) =>
            AppendPartDirectory(path, place, default);

        public static string AppendPartDirectory(string path, DirectoryPlace place, DateTimeOffset dt)
        {
            DirectoryInfo dir = Directory.CreateDirectory(
                Path.Combine(
                    path,
                    GetPartDirectory_(default, default, place, dt)));
            return (dir == null) ? string.Empty : dir.FullName;
        }
        #endregion

        #region Get Part Directory
        public static string GetPartDirectory(DirectoryPlace place) =>
            GetPartDirectory_(default, default, place, default);

        public static string GetPartDirectory(DirectoryPlace place, DateTimeOffset dt) =>
            GetPartDirectory_(default, default, place, dt);

        private static string GetTreeDirectory_(string pref, string plogin, DirectoryPlace place, DateTimeOffset dt) =>
            GetPartDirectory_(pref, plogin, place, (dt == default) ? DateTime.Now : dt);

        private static string GetPartDirectory_(string pref, string plogin, DirectoryPlace place, DateTimeOffset dt)
        {
            List<string> list = new();
            if (pref != default)
                list.Add(pref);
            if (plogin != default)
                list.Add(plogin);
            if (place != DirectoryPlace.None)
                list.Add(place.ToString());
            if (dt != default)
            {
                list.Add(dt.Year.ToString());
                list.Add(dt.Month.ToString());
                list.Add(dt.Day.ToString());
            }
            return Path.Combine(list.ToArray());
        }
        #endregion

        #region Get User File
        public static string GetUserFile(string login, DirectoryPlace place, string file) =>
            Path.Combine(GetUserDirectory(login, place), file);
        public static string GetUserFile(DirectoryPlace place, string path, string file) {
            DirectoryInfo dir = Directory.CreateDirectory(Path.Combine(path, place.ToString()));
            if (dir == null) return string.Empty;
            return Path.Combine(dir.FullName, file);
        }
        #endregion

        #region Get User Directory
        public static string GetUserDirectory(string login) =>
            GetUserDirectory(login, DirectoryPlace.Root, default);

        public static string GetUserDirectory(DirectoryPlace place) =>
            GetUserDirectory(default, place, default);

        public static string GetUserDirectory(DirectoryPlace place, DateTimeOffset dt) =>
            GetUserDirectory(default, place, dt);

        public static string GetUserDirectory(string login, DirectoryPlace place, DateTimeOffset dt = default)
        {
            string plogin = (login == default) ? string.Empty : Global.GetValidDirectory(login),
                   path = place switch {
                       DirectoryPlace.Msg => GetTreeDirectory_(rootacc, plogin, place, dt),
                       DirectoryPlace.Send => GetTreeDirectory_(rootacc, plogin, place, dt),
                       DirectoryPlace.Error => GetTreeDirectory_(rootacc, plogin, place, dt),
                       DirectoryPlace.Bounced => GetTreeDirectory_(rootacc, plogin, place, dt),
                       DirectoryPlace.Out => GetPartDirectory_(rootacc, plogin, place, default),
                       DirectoryPlace.Root => GetPartDirectory_(rootacc, plogin, DirectoryPlace.None, default),
                       DirectoryPlace.Log => place.ToString(),
                       _ => string.Empty
                   };
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            return Global.GetRootDirectory(path);
        }
        #endregion

        public static string GetRootDirectory(string expath = default)
        {
            string path;
            if (string.IsNullOrWhiteSpace(expath))
                path = Path.Combine(
                         Path.GetDirectoryName(
                           Assembly.GetEntryAssembly().Location),
                         "mail");
            else
                path = Path.Combine(
                         Path.GetDirectoryName(
                           Assembly.GetEntryAssembly().Location),
                         "mail", expath);

            DirectoryInfo dir = Directory.CreateDirectory(path);
            return (dir == null) ? string.Empty : dir.FullName;
        }

        public static string GetRootFile(DirectoryPlace place, string file)
        {
            switch (place) {
                case DirectoryPlace.Root: return Path.Combine(GetRootDirectory(), file);
                case DirectoryPlace.Log:
                case DirectoryPlace.Vpn:
                case DirectoryPlace.Proxy: return Path.Combine(GetRootDirectory(place.ToString()), file);
                default: return Path.Combine(GetRootDirectory(), rootacc, file);
            }
        }
        #endregion

        #region local auto initialize
        private async void FindAutoInit() {
            try {
                OnCallEvent(MailEventId.BeginInit, nameof(FindAutoInit));
                foreach (PropertyInfo pi in this.GetType().GetProperties()) {
                    var a = pi.GetValue(this);
                    Type t = a.GetType();
                    if (IsIAutoInitObject(t, a) && (a is IAutoInit ev))
                        await ev.AutoInit();
                    if (IsIMailEventObject(t, a) && (a is IMailEventProxy me))
                        me.SubscribeProxyEvent(eventProxy);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"{nameof(FindAutoInit)}: {ex}"); }
            finally {
                OnCallEvent(MailEventId.EndInit, nameof(FindAutoInit));
            }
        }
        private void FindAutoDispose() {
            try {
                foreach (PropertyInfo pi in this.GetType().GetProperties()) {
                    var a = pi.GetValue(this);
                    Type t = a.GetType();
                    if (IsIDisposableObject(t, a) && (a is IDisposable d)) {
                        try { d.Dispose(); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"{nameof(FindAutoDispose)}: {ex}"); }
                    }
                }
            } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"{nameof(FindAutoInit)}: {ex}"); }
        }
        private bool IsIAutoInitObject<T>(Type type, T val) where T : class
        {
            try {
                if (val == default)
                    return false;

                InterfaceMapping m = type.GetInterfaceMap(typeof(IAutoInit));
                MethodInfo mi = type.GetMethod(nameof(IAutoInit.AutoInit));
                if ((mi != default) && (m.TargetMethods.Length > 0) && (mi == m.TargetMethods[0])) {
                    System.Diagnostics.Debug.WriteLine($"\tAutoInit: {type.Name}");
                    return true;
                }
            } catch { }
            return false;
        }
        private bool IsIMailEventObject<T>(Type type, T val) where T : class
        {
            try {
                if (val == default)
                    return false;

                InterfaceMapping m = type.GetInterfaceMap(typeof(IMailEventProxy));
                MethodInfo mi = type.GetMethod(nameof(IMailEventProxy.SubscribeProxyEvent));
                if ((mi != default) && (m.TargetMethods.Length > 0)) {
                    System.Diagnostics.Debug.WriteLine($"\tMailEventProxy: {type.Name}");
                    return true;
                }
                    
            } catch { }
            return false;
        }
        private bool IsIDisposableObject<T>(Type type, T val) where T : class
        {
            try {
                if (val == default)
                    return false;

                InterfaceMapping m = type.GetInterfaceMap(typeof(IDisposable));
                MethodInfo mi = type.GetMethod(nameof(IDisposable.Dispose));
                if ((mi != default) && (m.TargetMethods.Length > 0)) {
                    System.Diagnostics.Debug.WriteLine($"\tIDisposable: {type.Name}");
                    return true;
                }

            } catch { }
            return false;
        }
        #endregion
    }
}

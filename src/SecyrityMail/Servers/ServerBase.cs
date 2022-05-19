/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


/*
	Protocol Plain       TLS/SSL
    IMAP	 Port 143	Port 993
    POP      Port 110	Port 995
    SMTP	 Port 25    Port 465
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SecyrityMail.Data;
using SecyrityMail.Utils;

namespace SecyrityMail.Servers
{
    public abstract class ServerBase : MailEvent, IDisposable
    {
        protected TcpListener Listener;
        protected CancellationTokenSafe SafeToken;
        protected Thread MainThread = default;
        protected EventHandler<EventActionArgs> eventClient;
        protected List<Tuple<IPAddress, DateTime, int>> spamFilter = new();
        protected string LogLocation = string.Empty;
        private bool IsLog_ = false;
        private readonly object __lock = new();

        public EndPoint IpEndPoint => Listener.LocalEndpoint;
        public bool IsSecure { get; set; } = false;
        public bool IsServiceRun { get; protected set; } = false;
        public bool IsLog { get => IsLog_; set { if (IsLog_ != value) { IsLog_ = value; InitLog(value); }}}

        public ServerBase(int port, CancellationTokenSafe token, bool issecure) => Init(port, IPAddress.Any, token, issecure ? 0 : int.MaxValue);
        public ServerBase(int port, CancellationTokenSafe token, int max) => Init(port, IPAddress.Any, token, max);
        public ServerBase(int port, IPAddress ip, CancellationTokenSafe token, int max) => Init(port, ip, token, max);
        ~ServerBase() => Dispose();

        protected void Init(int port, IPAddress ip, CancellationTokenSafe token, int maxPort)
        {
            SafeToken = token;
            IsSecure = port > maxPort;
            Listener = new TcpListener(ip, port);
            Listener.ExclusiveAddressUse = !Global.Instance.Config.IsSharingSocket;
            InitOptions();
            InitLog(IsLog);
            eventClient = new EventHandler<EventActionArgs>(Client_ProxyEventCb);
        }
        protected virtual void InitOptions() { }
        protected void InitLog(bool islog) { 
            if (islog && string.IsNullOrEmpty(LogLocation)) {
                DirectoryInfo dir = Directory.CreateDirectory(
                    Path.Combine(Global.GetUserDirectory(Global.DirectoryPlace.Log), "session-log"));
                if (dir != null) {
                    if (!dir.Exists)
                        dir.Create();
                    LogLocation = dir.FullName;
                }
            }
        }
        protected void Client_ProxyEventCb(object _, EventActionArgs args) =>
            OnProxyEvent(args);

        protected bool CheckSpamFilter(EndPoint ep) {
            try {
                do {
                    IPAddress ipa = ((IPEndPoint)ep).Address;
                    Tuple<IPAddress, DateTime, int> t;
                    lock (__lock) {
                        t = (from i in spamFilter
                             where i.Item1 == ipa
                             select i).FirstOrDefault();
                    }
                    if (t != null) {
                        DateTime dt = DateTime.Now;
                        if (dt > t.Item2) {
                            lock (__lock)
                                spamFilter.Remove(t);
                            t = null;
                        }
                        if ((t != null) && (t.Item3 >= Global.Instance.Config.SpamCheckCount)) {
                            Global.Instance.Log.Add(nameof(CheckSpamFilter), $"Spam count limit: {t.Item3}/{ipa} - access denied");
                            break;
                        }
                    }
                    if (!Global.Instance.ForbidenAccessIp.Check(ipa)) {
                        Global.Instance.Log.Add(nameof(CheckSpamFilter), $"Banned IP: {ipa} - access denied");
                        break;
                    }
                    return true;
                } while (false);
            } catch (Exception ex) { Global.Instance.Log.Add(nameof(CheckSpamFilter), ex); }
            return false;
        }

        protected void AddSpamFilter(EndPoint ep) {
            try {
                Tuple<IPAddress, DateTime, int> t;
                IPAddress ipa = ((IPEndPoint)ep).Address;
                DateTime dt = DateTime.Now.AddMinutes(Global.Instance.Config.SpamClientIdle);
                lock (__lock) {
                    t = (from i in spamFilter
                         where i.Item1 == ipa
                         select i).FirstOrDefault();
                }
                if (t == null) {
                    lock (__lock)
                        spamFilter.Add(new Tuple<IPAddress, DateTime, int>(ipa, dt, 1));
                } else {
                    lock (__lock) {
                        spamFilter.Remove(t);
                        spamFilter.Add(new Tuple<IPAddress, DateTime, int>(t.Item1, dt, t.Item3 + 1));
                    }
                }
            } catch { }
        }

        public virtual void Start() { }
        public virtual void Stop() => Dispose();
        public virtual void Wait()
        {
            if ((MainThread != default) && MainThread.IsAlive)
                MainThread.Join();
            IsServiceRun = false;
        }

        public void Dispose()
        {
            if (Listener != default)
                Listener.Stop();

            Thread th = MainThread;
            MainThread = default;
            if ((th != default) && th.IsAlive)
                th.Join();
            IsServiceRun = false;
        }
    }
}

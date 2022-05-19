/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SecyrityMail.Data;
using SecyrityMail.IPFilters;
using SecyrityMail.Proxy.SshProxy;

namespace SecyrityMail.Proxy
{
    public enum ProxyType : int {
        None = 0,
        All,
        Http,
        Https,
        Sock4,
        Sock5,
        SshSock4,
        SshSock5
    }

    public class MailProxy : MailEvent, IMailEventProxy, IAutoInit
    {
        public static readonly string[] Files = new string[] {
            "",
            "",
            "ProxyHttp.list",
            "ProxyHttps.list",
            "ProxySock4.list",
            "ProxySock5.list",
            "",
            ""
        };
        public static readonly ProxyType[] AllProxyTypeList = new ProxyType[] {
            ProxyType.None, ProxyType.Http, ProxyType.Https, ProxyType.Sock4, ProxyType.Sock5,
            ProxyType.SshSock4, ProxyType.SshSock5, ProxyType.All
        };
        public static readonly ProxyType[] SelectableProxyTypeList = new ProxyType[] {
            ProxyType.Http, ProxyType.Https, ProxyType.Sock4, ProxyType.Sock5
        };
        public bool IsProxyCheck { get; private set; } = false;
        public ProxyList ProxyList { get; } = new();
        public SshAccounts SshProxy { get; } = new();
        public HostData CheckHost => new(Global.Instance.Config.CheckProxyEndPointUrl);
        ~MailProxy() => ProxyList.Clear();

        public async Task AutoInit() => _ = await new ProxyListConverter().AllBuild().ConfigureAwait(false);

        public string GetProxyFileName(ProxyType type) =>
            type switch
            {
                ProxyType.Http => Files[(int)type],
                ProxyType.Https => Files[(int)type],
                ProxyType.Sock4 => Files[(int)type],
                ProxyType.Sock5 => Files[(int)type],
                _ => string.Empty
            };

        #region Get Proxyes
        public async Task<bool> GetSystemProxyes() =>
            await GetSystemProxyes(ProxyList.ProxyType).ConfigureAwait(false);

        public async Task<bool> GetSystemProxyes(ProxyType type) {
            switch (type) {
                case ProxyType.Http:
                case ProxyType.Https:
                case ProxyType.Sock4:
                case ProxyType.Sock5: break;
                case ProxyType.SshSock4:
                case ProxyType.SshSock5: return !SshProxy.IsEmpty;
                default: return false;
            }
            List<Tuple<string, int>> proxylist = await GetProxyesAsTuple(type).ConfigureAwait(false);
            if ((proxylist != null) && (proxylist.Count > 0)) {
                ProxyList.Set(type, proxylist);
                return true;
            }
            return false;
        }

        public async Task<List<string>> GetAndSaveProxyesAsString(ProxyType type, ProxyType listtype, List<string> list) =>
            await Task<List<string>>.Run(async () => {
                try {
                    if (!IsProxyValid(type))
                        return new();

                    do {
                        if (!IsProxyValid(listtype) || (list == default) || (list.Count == 0))
                            break;

                        List<string> load = new List<string>(list).Distinct().ToList();
                        FileInfo f = new FileInfo(Global.GetRootFile(Global.DirectoryPlace.Proxy, Files[(int)listtype]));
                        if (f != default)
                            File.WriteAllLines(f.FullName, load);
                        if (listtype == type)
                            return load;

                    } while (false);
                    return await GetProxyesAsString(type).ConfigureAwait(false);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(GetProxyesAsString), ex); }
                return new();
            });

        public async Task<List<string>> GetProxyesAsString(ProxyType type) =>
            await Task<List<string>>.Run(() => {
                try {
                    if (!IsProxyValid(type))
                        return new();

                    FileInfo f = new FileInfo(Global.GetRootFile(Global.DirectoryPlace.Proxy, Files[(int)type]));
                    if ((f == default) || !f.Exists) return new();
                    if (f.LastWriteTime.AddDays(3) < DateTime.Now)
                        OnCallEvent(MailEventId.DateExpired, type.ToString(), DateTime.Now - f.LastWriteTime.AddDays(3));

                    return File.ReadAllLines(f.FullName).ToList();
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(GetProxyesAsString), ex); }
                return new();
            });

        public async Task<List<Tuple<string, int>>> GetProxyesAsTuple(ProxyType type) =>
            await Task<List<Tuple<string, int>>>.Run(() =>
            {
                List<Tuple<string, int>> proxylist = new();
                try {
                    if (!IsProxyValid(type))
                        return proxylist;

                    FileInfo f = new FileInfo(Global.GetRootFile(Global.DirectoryPlace.Proxy, Files[(int)type]));
                    if ((f == default) || !f.Exists) return proxylist;
                    if (f.LastWriteTime.AddDays(3) < DateTime.Now)
                        OnCallEvent(MailEventId.DateExpired, type.ToString(), DateTime.Now - f.LastWriteTime.AddDays(3));

                    string[] lines = File.ReadAllLines(f.FullName);
                    foreach (string s in lines)
                    {
                        string[] parts = s.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((parts != null) && (parts.Length == 2))
                            try { proxylist.Add(Tuple.Create(parts[0].Trim(), int.Parse(parts[1].Trim()))); } catch { }
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(GetProxyesAsTuple), ex); }
                return proxylist;
            });

        public async Task<bool> MergeProxyes() =>
            await Task.Run(async () => {
                try {
                    if (ProxyList.IsEmpty || !ProxyList.IsActive)
                        return false;

                    if (!IsProxyValid(ProxyList.ProxyType))
                        return false;

                    int idx = ProxyList.GetActiveIndex();
                    if (idx < 0)
                        return false;

                    for (int i = idx - 1; i >= 0; i--) ProxyList.Remove(i);

                    StringBuilder sb = new();
                    for (int i = 0; i < ProxyList.Count; i++) {
                        Tuple<string, int> proxy = ProxyList.Get(i);
                        sb.AppendLine($"{proxy.Item1}:{proxy.Item2}");
                    }
                    return await SaveProxyList(ProxyList.ProxyType, sb.ToString(), Global.Instance.Log.Add)
                                .ConfigureAwait(false);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(MergeProxyes), ex); }
                return false;
            });
        #endregion

        #region Check Proxy
        public async Task<bool> CheckProxyes(CancellationToken token) {
            try {
                return await CheckProxyes(ProxyType.All, default(List<Tuple<string, int>>), token, Global.Instance.Log.Add)
                            .ConfigureAwait(false);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(CheckProxyes), ex); }
            return false;
        }

        public async Task<bool> CheckProxyes(
            ProxyType select, List<Tuple<string, int>> proxyes, CancellationToken token, Action<string, string> action = default) =>
            await Task.Run(async () =>
            {
                if (IsProxyCheck || token.IsCancellationRequested)
                    return false;
                IsProxyCheck = true;
                action = (action == default) ? Global.Instance.Log.Add : action;
                try {

                    if (select == ProxyType.All) {

                        ProxyType[] types = new ProxyType[] { ProxyType.Http, ProxyType.Https };
                        foreach (ProxyType type in types) {
                            if (token.IsCancellationRequested)
                                break;

                            bool b = await GetSystemProxyes().ConfigureAwait(false);
                            if (!b || (ProxyList.Count > 0)) continue;

                            _ = await CheckProxyType(type, ProxyList.GetItems(), token, action)
                                     .ConfigureAwait(false);
                        }
                    } else if (proxyes != null) {
                        return await CheckProxyType(select, proxyes, token, action)
                                    .ConfigureAwait(false);
                    }
                }
                catch (Exception ex) { action.Invoke(nameof(CheckProxyes), ex.Message); }
                finally {
                    IsProxyCheck = false;
                }
                return false;
            });

        public async Task<bool> CheckProxyes(
            ProxyType select, List<string> proxyes, CancellationToken token, Action<string, string> action) =>
            await Task.Run(async () => {

                if (IsProxyCheck || token.IsCancellationRequested || (proxyes == null))
                    return false;
                IsProxyCheck = true;
                try {
                    return await CheckProxyType(select, proxyes, token, action)
                                .ConfigureAwait(false);
                }
                catch (Exception ex) { action.Invoke(nameof(CheckProxyes), ex.Message); }
                finally {
                    IsProxyCheck = false;
                }
                return false;
            });

        private async Task<bool> CheckProxyType(
            ProxyType type, List<Tuple<string, int>> proxyes, CancellationToken token, Action<string, string> action) =>
            await Task.Run(async () => {
                try {
                    ProxyCheck pchk = new();
                    List<Tuple<string, int>> list = new();
                    HostData dest = CheckHost;
                    OnCallEvent(MailEventId.ProxyCheckStart, type.ToString(), default);

                    foreach (Tuple<string, int> t in proxyes) {
                        try {
                            if (token.IsCancellationRequested)
                                return false;

                            bool status = await pchk.CheckConnect(type, t.Item1, t.Item2, dest, 1500, token, action).ConfigureAwait(false);
                            CheckProxyHostLog(t.Item1, t.Item2, status, action);
                            if (status)
                                list.Add(t);

                        } catch (Exception ex) { action.Invoke(nameof(CheckProxyType), ex.Message); }
                    }
                    if (list.Count > 0) {
                        proxyes.Clear();
                        proxyes.AddRange(list);
                        return true;
                    }
                }
                catch (Exception ex) { action.Invoke(nameof(CheckProxyType), ex.Message); }
                finally {
                    OnCallEvent(MailEventId.ProxyCheckEnd, type.ToString(), default);
                }
                return false;
            });

        private async Task<bool> CheckProxyType(
            ProxyType type, List<string> proxyes, CancellationToken token, Action<string, string> action) =>
            await Task.Run(async () => {
                try {
                    ProxyCheck pchk = new();
                    List<string> list = new();
                    HostData dest = CheckHost;
                    OnCallEvent(MailEventId.ProxyCheckStart, type.ToString(), default);

                    foreach (string s in proxyes) {
                        try {
                            if (token.IsCancellationRequested)
                                return false;

                            bool status = await CheckProxyHost(type, s, token, action).ConfigureAwait(false);
                            if (status)
                                list.Add(s.Trim());
                        }
                        catch (Exception ex) { action.Invoke(nameof(CheckProxyType), ex.Message); }
                    }
                    if (list.Count > 0) {
                        proxyes.Clear();
                        proxyes.AddRange(list);
                        return true;
                    }
                }
                catch (Exception ex) { action.Invoke(nameof(CheckProxyType), ex.Message); }
                finally {
                    OnCallEvent(MailEventId.ProxyCheckEnd, type.ToString(), default);
                }
                return false;
            });

        public async Task<bool> CheckProxyHost(
            ProxyType type, string addr, CancellationToken token, Action<string, string> action) =>
            await Task.Run(async () => {
                try {
                    ProxyCheck pchk = new();
                    List<string> list = new();
                    HostData dest = CheckHost;

                    string[] ss = addr.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if ((ss == null) || (ss.Length != 2) || (!int.TryParse(ss[1].Trim(), out int port)))
                        return false;

                    string ip = ss[0].Trim();
                    if (string.IsNullOrEmpty(ip))
                        return false;

                    bool status = await pchk.CheckConnect(type, ip, port, dest, 10000, token, action).ConfigureAwait(false);
                    CheckProxyHostLog(ip, port, status, action);
                    return status;
                }
                catch (Exception ex) { action.Invoke(nameof(CheckProxyHost), ex.Message); }
                return false;
            });

        public async Task<bool> CheckProxyHost(
            ProxyType type, string ip, int port, CancellationToken token, Action<string, string> action) =>
            await Task.Run(async () => {
                try {
                    ProxyCheck pchk = new();
                    List<string> list = new();
                    HostData dest = CheckHost;

                    bool status = await pchk.CheckConnect(type, ip, port, dest, 10000, token, action).ConfigureAwait(false);
                    CheckProxyHostLog(ip, port, status, action);
                    return status;
                }
                catch (Exception ex) { action.Invoke(nameof(CheckProxyHost), ex.Message); }
                return false;
            });

        private void CheckProxyHostLog(string ip, int port, bool status, Action<string, string> action) =>
            action.Invoke(nameof(CheckProxyType),
                string.Format(
                    "check {0}:{1} -> {2} {3}",
                    ip, port, status ? "OK" : "ERROR", ip.GetIpDescription()));
        #endregion

        #region Save
        public async Task<bool> SaveProxyes(ProxyType type, List<Tuple<string, int>> list) =>
            await Task.Run(async () => {
                try {
                    if ((list == null) || (list.Count == 0))
                        return false;

                    if (!IsProxyValid(type))
                        return false;

                    return await SaveProxyList(type,
                        string.Join("\n", list.Select(x => $"{x.Item1}:{x.Item2}").ToArray()),
                        Global.Instance.Log.Add)
                              .ConfigureAwait(false);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(MergeProxyes), ex); }
                return false;
            });

        public async Task<bool> SaveProxyes(ProxyType type, List<string> list) =>
            await Task.Run(async () => {
                try {
                    if ((list == null) || (list.Count == 0))
                        return false;

                    if (!IsProxyValid(type))
                        return false;

                    return await SaveProxyList(type, list, Global.Instance.Log.Add)
                                .ConfigureAwait(false);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(MergeProxyes), ex); }
                return false;
            });

        private async Task<bool> SaveProxyList(
            ProxyType type, List<string> list, Action<string, string> act) =>
            await Task.Run(() => {
                if (list.Count > 0) {
                    try {
                        FileInfo f = new FileInfo(Global.GetRootFile(Global.DirectoryPlace.Proxy, Files[(int)type]));
                        if (f == default)
                            return false;
                        File.WriteAllLines(f.FullName, list.ToArray());
                        act.Invoke(nameof(SaveProxyList), $"save {type} list proxyes, count {list.Count}");
                        return true;
                    } catch (Exception ex) { act.Invoke(nameof(SaveProxyList), ex.Message); }
                }
                return false;
            });

        private async Task<bool> SaveProxyList(
            ProxyType type, string body, Action<string, string> act) =>
            await Task.Run(() => {
                if (body.Length > 0) {
                    try {
                        FileInfo f = new FileInfo(Global.GetRootFile(Global.DirectoryPlace.Proxy, Files[(int)type]));
                        if (f == default)
                            return false;
                        File.WriteAllText(f.FullName, body);
                        act.Invoke(nameof(SaveProxyList), $"save {type} list proxyes, size {body.Length}");
                        return true;
                    } catch (Exception ex) { act.Invoke(nameof(SaveProxyList), ex.Message); }
                }
                return false;
            });
        #endregion

        private bool IsProxyValid(ProxyType type) =>
            type switch {
                ProxyType.Http => true,
                ProxyType.Https => true,
                ProxyType.Sock4 => true,
                ProxyType.Sock5 => true,
                _ => false
            };
    }
}

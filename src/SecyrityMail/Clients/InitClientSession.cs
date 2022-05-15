
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Proxy;
using SecyrityMail.IPFilters;
using SecyrityMail.MailAccounts;
using SecyrityMail.Proxy;
using SecyrityMail.Proxy.SshProxy;
using SecyrityMail.Utils;

namespace SecyrityMail.Clients
{
    internal class InitClientSession
    {
        #region InitClient
        public async Task<IMailClient> InitClient(
            Func<IProtocolLogger, IMailClient> create, UserAccount account, string rootpath, TokenSafe token) {

            ProxyList proxy = Global.Instance.Proxy.ProxyList;
            ProxyType current = proxy.ProxyType;

            if (account.CurrentAction == AccountUsing.None)
                throw new Exception($"Account action {account.CurrentAction} invalid, abort");

            Global.Instance.Log.Add(nameof(InitClientSession), $"Create client {account.CurrentAction} session");

            if ((current == ProxyType.SshSock4) || (current == ProxyType.SshSock5)) {

                try {
                    SshAccounts accs = Global.Instance.Proxy.SshProxy;
                    if (!Global.Instance.Config.IsSshRunning) {
                        _ = await accs.RandomSelect(current).ConfigureAwait(false);
                        Global.Instance.Log.Add(nameof(InitClient), $"select SSH proxy: {accs.AccountSelected.Name}");
                    } else if (accs.IsAccountSelected) {
                        Global.Instance.Log.Add(nameof(InitClient), $"using running SSH proxy: {accs.AccountSelected.Name}");
                    }
                    if (accs.IsAccountSelected)
                        return await CreateClient(create, account, current, default, rootpath, token).ConfigureAwait(false);
                    else {
                        for (int i = 0; i < accs.Count; i++) {
                            try {
                                SshAccount sshacc = accs[i];
                                if (sshacc.IsEmpty || sshacc.IsExpired)
                                    continue;
                                _ = await accs.SelectAccount(i).ConfigureAwait(false);
                                Global.Instance.Log.Add(nameof(InitClient), $"select next SSH proxy: {sshacc.Name}");
                                return await CreateClient(create, account, current, default, rootpath, token).ConfigureAwait(false);
                            } catch (Exception ex) { Global.Instance.Log.Add(nameof(InitClient), ex); }
                        }
                    }
                    throw new Exception("No working servers found among SSH proxy accounts, abort");
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(InitClient), ex); return default; }
            }
            else if (current == ProxyType.None) {
                try {
                    return await CreateClient(create, account, current, default, rootpath, token).ConfigureAwait(false);
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(InitClient), ex); return default; }
            }
            else if (proxy.IsActive) {
                do {
                    Tuple<string, int> p = proxy.Active;
                    if (p == null)
                        break;

                    try {
                        IMailClient client = await CreateClient(create, account, current, new(p.Item1, p.Item2), rootpath, token).ConfigureAwait(false);
                        if (client != null)
                            return client;
                    } catch (Exception ex) { Global.Instance.Log.Add(nameof(InitClient), ex); }

                } while (false);
                proxy.Reset();
                proxy.ActiveClean();
                proxy.ProxyType = Global.Instance.Config.ProxyType;
                return await InitClient(create, account, rootpath, token).ConfigureAwait(false);
            }
            else if (current == ProxyType.All) {

                foreach(ProxyType pt in MailProxy.SelectableProxyTypeList) {

                    if (token.IsCancellationRequested)
                        break;

                    int x = await LoadProxyList(pt).ConfigureAwait(false);
                    switch (x) {
                        case -1: Global.Instance.Log.Add(nameof(InitClient), $"Proxy list {pt} empty? not load, continue"); continue;
                        case  0: Global.Instance.Log.Add(nameof(InitClient), $"Proxy list {pt} not elements, continue"); continue;
                        default: break;
                    }
                    IMailClient client = await ProxyForEach(create, account, current, rootpath, token).ConfigureAwait(false);
                    if (client != null)
                        return client;
                }
                return default;
            }
            else {
                if (Global.Instance.Proxy.ProxyList.Count == 0) {
                    int x = await LoadProxyList(current).ConfigureAwait(false);
                    switch (x) {
                        case -1: throw new Exception($"Proxy list {current} not load, abort");
                        case 0: throw new Exception($"Proxy list {current} not elements, abort");
                        default: break;
                    }
                }
                return await ProxyForEach(create, account, current, rootpath, token).ConfigureAwait(false);
            }
        }
        #endregion

        #region Create Client
        private async Task<IMailClient> CreateClient(
            Func<IProtocolLogger, IMailClient> create,
            UserAccount account, ProxyType type, Tuple<string, int> host, string rootpath, TokenSafe token) {

            IMailClient client = default;
            try {
                bool issshrun = Global.Instance.Config.IsSshRunning;
                client = create.Invoke(
                    new ProtocolLogger(
                        Global.GetUserFile(
                            Global.DirectoryPlace.Log, rootpath, SessionLogName(host, account.CurrentAction))));
                client.Timeout = Global.Instance.Config.ClientTimeout;
                client.LocalDomain = Path.GetRandomFileName().Replace('.', '-');
                switch (type) {
                    case ProxyType.Http:  client.ProxyClient = new HttpProxyClient(host.Item1, host.Item2); break;
                    case ProxyType.Https: client.ProxyClient = new HttpsProxyClient(host.Item1, host.Item2) {
                        ServerCertificateValidationCallback = (s, c, ch, e) => true,
                        CheckCertificateRevocation = false
                    }; break;
                    case ProxyType.Sock4: client.ProxyClient = new Socks4Client(host.Item1, host.Item2); break;
                    case ProxyType.Sock5: client.ProxyClient = new Socks5Client(host.Item1, host.Item2); break;
                    case ProxyType.SshSock4: client.ProxyClient = (ProxySshSocks4)Global.Instance.SshProxy.ProxySsh; break;
                    case ProxyType.SshSock5: client.ProxyClient = (ProxySshSocks5)Global.Instance.SshProxy.ProxySsh; break;
                    default: break;
                }
                if (token.IsCancellationRequested) {
                    ClientUnload(client, token);
                    return default;
                }
                if (Global.Instance.Config.IsVpnEnable &&
                    Global.Instance.Config.IsVpnSelected &&
                    (Global.Instance.Config.VpnEndpoint != null)) {

                    if (((type == ProxyType.Sock4) || (type == ProxyType.Sock5)) && issshrun) {}
                    else if (client.ProxyClient != null)
                        client.ProxyClient.LocalEndPoint = Global.Instance.Config.VpnEndpoint;
                    else
                        client.LocalEndPoint = Global.Instance.Config.VpnEndpoint;
                }
                switch (account.CurrentAction) {
                    case AccountUsing.Pop3: {
                            await client.ConnectAsync(account.Pop3Addr, account.Pop3Port, account.Pop3Secure, token.GetToken);
                            break;
                        }
                    case AccountUsing.Imap: {
                            await client.ConnectAsync(account.ImapAddr, account.ImapPort, account.ImapSecure, token.GetToken);
                            break;
                        }
                    case AccountUsing.Smtp: {
                            await client.ConnectAsync(account.SmtpAddr, account.SmtpPort, account.SmtpSecure, token.GetToken);
                            break;
                        }
                    default: throw new NotImplementedException("Account current action invalid");
                }
                if (token.IsCancellationRequested) {
                    ClientUnload(client, token);
                    return default;
                }

                await client.AuthenticateAsync(account.Login, account.Pass);
                if (client.ProxyClient != null)
                    Global.Instance.ProxyList.ActiveSelected();
                return client;
            }
            catch (SocketException ex) {
                var s = ex.Message;
                Global.Instance.Log.Add(nameof(CreateClient), s.Substring(0, (s.Length > 48) ? 48 : s.Length));
            }
            catch (Exception ex)
            {
#               if DEBUG
                System.Diagnostics.Debug.WriteLine(ex);
#               endif

                if (host != null)
                    Global.Instance.Log.Add(nameof(CreateClient), $"{host.Item1}:{host.Item2} = {ex.Message}");
                else
                    Global.Instance.Log.Add(nameof(CreateClient), ex);
                ClientUnload(client, token);
            }
            return default;
        }
        #endregion

        #region private
        private async Task<IMailClient> ProxyForEach(
            Func<IProtocolLogger, IMailClient> create, UserAccount account, ProxyType type, string rootpath, TokenSafe token) {

            Tuple<string, int> proxy;
            Global.Instance.Proxy.ProxyList.ActiveClean();
            while ((proxy = Global.Instance.Proxy.ProxyList.Next) != default) {
                try {
                    if (proxy == null)
                        continue;
                    
                    Tuple<string, int, string> proxyinfo = proxy.GetIpInfo();
                    string hostinfo = string.Format("{0}:{1} {2}",
                        (proxyinfo == null) ? proxy.Item1  : proxyinfo.Item1,
                        (proxyinfo == null) ? proxy.Item2  : proxyinfo.Item2,
                        (proxyinfo == null) ? string.Empty : proxyinfo.Item3);

                    Global.Instance.Log.Add("CheckProxy", hostinfo);

                    IMailClient client = await CreateClient(create, account, type, new(proxy.Item1, proxy.Item2), rootpath, token);
                    if (client != null) {
                        Tuple<string, int> active = Global.Instance.ProxyList.Active;
                        if (active != null)
                            Global.Instance.Log.Add("ActiveProxy", $"using: {hostinfo}");
                        else {
                            ClientUnload(client, token);
                            throw new ArgumentNullException(nameof(active));
                        }
                        return client;
                    }
                }
                catch (MailKit.ServiceNotConnectedException) { continue; }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(ProxyForEach), ex); }
            }
            return default;
        }

        private async Task<int> LoadProxyList(ProxyType type) {
            bool b = await Global.Instance.Proxy.GetSystemProxyes(type).ConfigureAwait(false);
            if (!b) return -1;
            return (Global.Instance.Proxy.ProxyList.Count == 0) ? 0 : 1;
        }

        private string SessionLogName(Tuple<string, int> host, AccountUsing type) {
            DateTime dt = DateTime.Now;
            if (host != null)
                return $"{type}-{DateTime.Now:yyyy-MM-dd-HH-mm}-{host.Item1}-{host.Item2}.log";
            else
                return $"{type}-{DateTime.Now:yyyy-MM-dd-HH-mm}-empty.log";
        }

        private void ClientUnload(IMailClient client, TokenSafe token) {
            if (client != null) {
                try { client.Disconnect(true, token.GetToken); } catch { }
                try { client.Dispose(); } catch { }
            }
        }
        #endregion
    }
}

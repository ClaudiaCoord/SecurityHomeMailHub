/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SecyrityMail.Data;

namespace SecyrityMail.Proxy.SshProxy
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = false)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    [XmlInclude(typeof(AccountsBase<SshAccounts, SshAccount>))]
    public class SshAccounts : AccountsBase<SshAccounts, SshAccount>, IAccountsBase<SshAccounts, SshAccount>, IAutoInit
    {
        private string GetTag(string s) => $"SSH {s}";

        public override SshAccounts AddOnce(SshAccount acc) {
            Items.Add(acc);
            OnPropertyChanged(nameof(SshAccount));
            return this;
        }
        public override bool Copy(SshAccounts accs) {
            if ((accs == null) || (accs.Items == null) || (accs.Items.Count == 0))
                return false;
            Items.Clear();
            Items.AddRange(accs.Items);
            OnPropertyChanged(nameof(SshAccount));
            return Items.Count > 0;
        }
        public override SshAccount Find(string login) {
            if (Items.Count == 0)
                return default;
            return (from i in Items where i.Login == login && i.Enable select i).FirstOrDefault();
        }

        [XmlIgnore]
        public override SshAccount AccountSelected {
            get => AccountSelected_;
            protected set {
                if (AccountSelected_ == value) return;
                AccountSelected_ = value;
                if ((AccountSelected_ != null) && AccountSelected_.IsExpired)
                    OnCallEvent(MailEventId.DateExpired,
                        $"{GetTag("Account")}/{AccountSelected_.Login}", DateTime.Now - AccountSelected_.Expired);
                OnPropertyChanged(nameof(AccountSelected), nameof(IsAccountSelected));
            }
        }

        [XmlIgnore]
        public bool IsAccountRunning => IsAccountSelected && (proxySsh_ != default);

        [XmlIgnore]
        public override bool IsAccountSelected => (AccountSelected != default) && !AccountSelected.IsEmpty && !AccountSelected.IsExpired;

        [XmlIgnore]
        public override bool IsExpired => IsAccountSelected && AccountSelected.IsExpired;

        [XmlIgnore]
        public IProxySsh ProxySsh {
            get {
                if (proxySsh_ != default)
                    return proxySsh_;

                if (!IsAccountSelected || AccountSelected.IsEmpty || AccountSelected.IsExpired)
                    return default;

                switch(AccountSelected.Type) {
                    case ProxyType.SshSock4: proxySsh_ = new ProxySshSocks4(AccountSelected); break;
                    case ProxyType.SshSock5: proxySsh_ = new ProxySshSocks5(AccountSelected); break;
                    default: return default;
                }
                return proxySsh_;
            }
            set {
                if (proxySsh_ == value)
                    return;
                ProxySshDispose();
                proxySsh_ = value;
                OnPropertyChanged();
            }
        }
        private IProxySsh proxySsh_ = default;

        public void ProxySshDispose() {

            IProxySsh p = proxySsh_;
            proxySsh_ = null;
            if (p != default)
                try { p.Dispose(); } catch { }
        }

        public async Task Start() =>
            await Task.Run(() => {
                try {
                    if (IsAccountRunning)
                        return;
                    if (!IsAccountSelected)
                        return;
                    ProxySsh = AccountSelected.Type switch {
                        ProxyType.SshSock4 => new ProxySshSocks4(AccountSelected),
                        ProxyType.SshSock5 => new ProxySshSocks5(AccountSelected),
                        _ => default
                    };
                    if (Global.Instance.Config.IsVpnTunnelRunning)
                        ProxySsh.LocalEndPoint = Global.Instance.Config.VpnEndpoint;

                    OnCallEvent(
                        (ProxySsh == default) ? MailEventId.EndCall : MailEventId.BeginCall,
                        $"{GetTag("Account")}/{AccountSelected.Login}");

                    if (IsAccountRunning) {
                        int ver = (AccountSelected.Type == ProxyType.SshSock4) ? 4 :
                                    (AccountSelected.Type == ProxyType.SshSock5) ? 5 : -1;
                        if (ver == -1) {
                            Global.Instance.Log.Add(GetTag(nameof(Start)), "error check version SOCK proxy, valid only 4 or 5");
                            return;
                        }
                        string host = (ver == 4) ?
                                $"{ProxySshSocks4.SshProxyHost}:{ProxySshSocks4.SshProxyPort}" :
                                    $"{ProxySshSocks5.SshProxyHost}:{ProxySshSocks5.SshProxyPort}";

                        StringBuilder sb = new();
                        sb.Append("using global");
                        if (Global.Instance.Config.IsVpnTunnelRunning && Global.Instance.VpnAccounts.IsAccountSelected)
                            sb.Append($" VPN:({Global.Instance.VpnAccounts.AccountSelected.Name}) ->");
                        else if (Global.Instance.Config.IsVpnTunnelRunning)
                            sb.Append($" VPN ->");

                        if (!string.IsNullOrWhiteSpace(AccountSelected.Name))
                            sb.Append($" SOCK{ver}:({AccountSelected.Name}) proxy -> {host}");
                        else
                            sb.Append($" SOCK{ver} proxy -> {host}");
                        Global.Instance.Log.Add(GetTag(nameof(Start)), sb.ToString());
                    }
                } catch (Exception ex) { Global.Instance.Log.Add(GetTag(nameof(Start)), ex); }
            });

        public void Stop() {

            try {
                if (!IsAccountRunning)
                    return;
                ProxySshDispose();
                OnCallEvent(
                    MailEventId.EndCall,
                    $"{nameof(SshAccount)}/{AccountSelected.Login}");
                Global.Instance.Log.Add(GetTag(nameof(Stop)), "stopping global SOCK5 proxy");
            } catch (Exception ex) { Global.Instance.Log.Add(GetTag(nameof(Stop)), ex); }
        }

        public async Task<bool> Save() => await Save(this);

        public async Task<bool> RandomSelect(ProxyType proxyType = ProxyType.None) =>
            await Task.Run(() => {

                AccountSelected = default;
                List<SshAccount> list;
                list = (from i in Items where i.IsExpired select i).ToList();
                if ((list != null) && (list.Count > 0))
                    foreach (SshAccount acc in list)
                        OnCallEvent(MailEventId.DateExpired,
                            $"{GetTag("Account")}/{acc.Login}", DateTime.Now - acc.Expired);

                if (proxyType == ProxyType.None)
                    list = (from i in Items where !i.IsExpired && !i.IsEmpty && i.Enable select i).ToList();
                else
                    list = (from i in Items where !i.IsExpired && !i.IsEmpty && i.Enable && i.Type == proxyType select i).ToList();

                if ((list == null) || (list.Count == 0)) {
                    AccountSelected = default;
                } else {
                    Random rnd = new Random();
                    AccountSelected = list[rnd.Next(0, list.Count)];
                }
                if (!IsAccountSelected)
                    OnCallEvent(MailEventId.NotFound,
                        $"{GetTag("Account")}/{nameof(RandomSelect)}");
                return IsAccountSelected;
            });

        public async Task<bool> SelectAccount(int i) =>
            await Task.Run(() => {
                if ((i >= Items.Count) || (i < 0) || !Items[i].Enable)
                    return false;
                AccountSelected = Items[i];
                OnPropertyChanged(nameof(AccountSelected));
                return true;
            });

        public async Task<bool> SelectAccount(string s) =>
            await Task.Run(() => {
                if (string.IsNullOrWhiteSpace(s))
                    return false;

                AccountSelected = (from i in Items where i.Name.Equals(s) select i).FirstOrDefault();
                OnPropertyChanged(nameof(AccountSelected));
                return true;
            });

        public async Task AutoInit() {
            _ = await Load().ConfigureAwait(false);
            if (Items.Count > 0)
                _ = await RandomSelect().ConfigureAwait(false);
        }
        public async Task<bool> Save(bool isbackup = false) => await Save(this, isbackup);
    }
}

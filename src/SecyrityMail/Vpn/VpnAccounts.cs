
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SecyrityMail.Data;

namespace SecyrityMail.Vpn
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = false)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    [XmlInclude(typeof(AccountsBase<VpnAccounts, VpnAccount>))]
    public class VpnAccounts : AccountsBase<VpnAccounts, VpnAccount>, IAccountsBase<VpnAccounts, VpnAccount>
    {
        private IPEndPoint ipEndpoint = default(IPEndPoint);
        [XmlIgnore]
        public IPEndPoint IpEndpoint {
            get => ipEndpoint;
            private set { ipEndpoint = value; OnPropertyChanged(); }
        }

        private string [] splitDns = default;
        [XmlIgnore]
        public string Dns => (splitDns != default) ? splitDns[new Random().Next(0, splitDns.Length)] : string.Empty;

        [XmlIgnore]
        public override VpnAccount AccountSelected {
            get => AccountSelected_;
            protected set {
                if (AccountSelected_ == value) return;
                AccountSelected_ = value;
                if (AccountSelected_ != null) {
                    try {
                        string ip;
                        string[] ips = AccountSelected_.Interface.Address.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        if ((ips != null) && (ips.Length > 0))
                            ip = ips[0];
                        else
                            ip = AccountSelected_.Interface.Address;
                        IpEndpoint = new IPEndPoint(IPAddress.Parse(ip), 0);

                        if (!string.IsNullOrWhiteSpace(AccountSelected_.Interface.DNS))
                            splitDns = AccountSelected_.Interface.DNS.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                    }
                    catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine($"{nameof(VpnAccounts)}: {ex}");
                        IpEndpoint = default;
                    }
                    if (AccountSelected_.IsExpired)
                        OnCallEvent(MailEventId.DateExpired, $"{nameof(VpnAccount)}/{AccountSelected_.Name}", DateTime.Now - AccountSelected_.Expired);
                }
                else
                    IpEndpoint = default;
                OnPropertyChanged(nameof(AccountSelected), nameof(IsAccountSelected));
            }
        }
        [XmlIgnore]
        public override bool IsAccountSelected { get => (AccountSelected != default) && !AccountSelected.IsEmpty; }

        [XmlIgnore]
        public override bool IsExpired { get => IsAccountSelected && AccountSelected.IsExpired; }

        public override VpnAccounts AddOnce(VpnAccount acc) {
            Items.Add(acc);
            OnPropertyChanged(nameof(VpnAccount));
            return this;
        }
        public override bool Copy(VpnAccounts accs) {
            if ((accs == null) || (accs.Items == null) || (accs.Items.Count == 0))
                return false;
            Items.Clear();
            Items.AddRange(accs.Items);
            OnPropertyChanged(nameof(VpnAccounts));
            return Items.Count > 0;
        }
        public override VpnAccount Find(string name) {
            if (Items.Count == 0)
                return default;
            return (from i in Items where i.Name == name && i.Enable select i).FirstOrDefault();
        }

        public async Task<bool> ImportAccount(string path) {
            FileInfo fi = new(path);
            if ((fi == default) || !fi.Exists)
                return false;

            VpnAccount acc = new VpnAccount();
            bool b = await acc.Import(fi.FullName);

            if (b) {
                int i = Items.Count;
                Add(acc);
                b = await SelectAccount(i).ConfigureAwait(false);
            }
            return b;
        }

        public async Task<bool> SelectAccount(int i) =>
            await Task.Run(() => {
                if ((i >= Items.Count) || (i < 0) || !Items[i].Enable)
                    return false;
                AccountSelected = Items[i];
                OnPropertyChanged(nameof(AccountSelected));
                return IsAccountSelected;
            });

        public async Task<bool> SelectAccount(string s) =>
            await Task.Run(() => {
                if (string.IsNullOrWhiteSpace(s))
                    return false;

                AccountSelected = (from i in Items where i.Name.Equals(s) && !i.IsEmpty && !i.IsExpired && i.Enable select i).FirstOrDefault();
                OnPropertyChanged(nameof(AccountSelected));
                return IsAccountSelected;
            });

        public override async Task<bool> RandomSelect() =>
            await Task.Run(() => {

                AccountSelected = default;
                List<VpnAccount> list;
                list = (from i in Items where i.IsExpired select i).ToList();
                if ((list != null) && (list.Count > 0))
                    foreach (VpnAccount acc in list)
                        OnCallEvent(MailEventId.DateExpired, $"{nameof(VpnAccount)}/{acc.Name}", DateTime.Now - acc.Expired);

                list = (from i in Items where !i.IsExpired && !i.IsEmpty && i.Enable select i).ToList();
                if ((list == null) || (list.Count == 0)) {
                    AccountSelected = default;
                } else {
                    Random rnd = new Random();
                    AccountSelected = list[rnd.Next(0, list.Count)];
                }
                if (!IsAccountSelected)
                    OnCallEvent(MailEventId.NotFound, $"{nameof(VpnAccount)}/{nameof(RandomSelect)}");
                return IsAccountSelected;
            });

        public async Task<bool> Save(bool isbackup = false) => await Save(this, isbackup);
    }
}

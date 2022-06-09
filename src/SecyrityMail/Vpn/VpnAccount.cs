/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SecyrityMail.Utils;

namespace SecyrityMail.Vpn
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class VpnInterface
    {
        [XmlIgnore]
        const string DnsDefault = "1.1.1.1, 1.0.0.1, 8.8.8.8, 8.8.4.4";

        [XmlElement("PrivateKey")]
        public string PrivateKey { get; set; } = string.Empty;
        [XmlElement("Address")]
        public string Address { get; set; } = string.Empty;
        [XmlElement("DNS")]
        public string DNS { get; set; } = DnsDefault;
        [XmlElement("MTU")]
        public string MTU { get; set; } = string.Empty;

        public void SetDefault() =>
            DNS = string.IsNullOrWhiteSpace(Global.Instance.Config.VpnDnsDefault) ?
                DnsDefault : Global.Instance.Config.VpnDnsDefault;
    }
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class VpnPeer
    {
        [XmlIgnore]
        const string AllowedIPsAll = "0.0.0.0/0, ::/0";
        const string AllowedIPsDefault = "0.0.0.0/0";

        [XmlElement("PublicKey")]
        public string PublicKey { get; set; } = string.Empty;
        [XmlElement("PresharedKey")]
        public string PresharedKey { get; set; } = string.Empty;
        [XmlElement("AllowedIPs")]
        public string AllowedIPs { get; set; } = AllowedIPsDefault;
        [XmlElement("Endpoint")]
        public string Endpoint { get; set; } = string.Empty;
        [XmlElement("PersistentKeepalive")]
        public short PersistentKeepalive { get; set; } = 0;

        public void SetDefault() =>
            AllowedIPs = string.IsNullOrWhiteSpace(Global.Instance.Config.VpnAllowedIPDefault) ?
                AllowedIPsDefault : Global.Instance.Config.VpnAllowedIPDefault;

        public void SetAllowedAll() =>
            AllowedIPs = AllowedIPsAll;
    }
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class VpnAccount
    {
        private string _Name = string.Empty;

        [XmlElement("Interface")]
        public VpnInterface Interface { get; set; } = new();
        [XmlElement("Peer")]
        public VpnPeer Peer { get; set; } = new();
        [XmlElement("Expired")]
        public DateTime Expired { get; set; } = DateTime.MinValue;
        [XmlElement("Enable")]
        public bool Enable { get; set; } = true;
        [XmlElement("Name")]
        public string Name {
            get {
                if (string.IsNullOrWhiteSpace(_Name)) {
                    if (string.IsNullOrWhiteSpace(Peer.Endpoint))
                        return string.Empty;
                    int idx = Peer.Endpoint.IndexOf(':');
                    _Name = (idx > 0) ? Peer.Endpoint.Substring(0, idx) : Peer.Endpoint;
                }
                return _Name;
            }
            set => _Name = value; }

        [XmlIgnore]
        public bool IsEmpty => (Interface == null) || (Peer == null) ||
            string.IsNullOrWhiteSpace(Interface.PrivateKey) || string.IsNullOrWhiteSpace(Peer.PublicKey) ||
            string.IsNullOrWhiteSpace(Interface.Address) || string.IsNullOrWhiteSpace(Peer.Endpoint);

        [XmlIgnore]
        public bool IsExpired =>
            Expired != DateTime.MinValue && Expired <= DateTime.Now;

        public bool Copy(VpnAccount vpn)
        {
            if (vpn == default)
                return false;

            if (vpn.Interface != default) {
                Interface.PrivateKey = vpn.Interface.PrivateKey;
                Interface.Address = vpn.Interface.Address;
                Interface.DNS = vpn.Interface.DNS;
                Interface.MTU = vpn.Interface.MTU;
            }
            if (vpn.Peer != default) {
                Peer.PublicKey = vpn.Peer.PublicKey;
                Peer.Endpoint = vpn.Peer.Endpoint;
                Peer.AllowedIPs = vpn.Peer.AllowedIPs;
                Peer.PersistentKeepalive = vpn.Peer.PersistentKeepalive;
            }
            Name = vpn.Name;
            Expired = vpn.Expired;
            Enable = vpn.Enable;
            return !IsEmpty;
        }

        #region Export
        public async Task<string> Export(string path = default) =>
            await Task.Run(() => {
                try {
                    if (IsEmpty)
                        throw new Exception("VPN account is empty..");

                    if (string.IsNullOrWhiteSpace(path)) {
                        path = Global.GetRootFile(Global.DirectoryPlace.Vpn,
                            $"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.conf");
                    }
                    Encoding enc = new UTF8Encoding(false);
                    StringBuilder sb = new();
                    sb.Append($"[{nameof(VpnAccount.Interface)}]\n");
                    sb.Append($"{nameof(VpnInterface.PrivateKey)} = {Interface.PrivateKey}\n");
                    sb.Append($"{nameof(VpnInterface.Address)} = {Interface.Address}\n");
                    if (!string.IsNullOrWhiteSpace(Interface.DNS))
                        sb.Append($"{nameof(VpnInterface.DNS)} = {Interface.DNS}\n");
                    if (!string.IsNullOrWhiteSpace(Interface.MTU))
                        sb.Append($"{nameof(VpnInterface.MTU)} = {Interface.MTU}\n");

                    sb.Append($"[{nameof(VpnAccount.Peer)}]\n");
                    sb.Append($"{nameof(VpnPeer.PublicKey)} = {Peer.PublicKey}\n");
                    if (!string.IsNullOrWhiteSpace(Peer.PresharedKey))
                        sb.Append($"{nameof(VpnPeer.PresharedKey)} = {Peer.PresharedKey}\n");
                    sb.Append($"{nameof(VpnPeer.Endpoint)} = {Peer.Endpoint}\n");
                    sb.Append($"{nameof(VpnPeer.AllowedIPs)} = {Peer.AllowedIPs}\n");
                    sb.Append($"{nameof(VpnPeer.PersistentKeepalive)} = {Peer.PersistentKeepalive}");
                    File.WriteAllBytes(path, enc.GetBytes(sb.ToString()));
                    return path;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Export), ex); }
                return string.Empty;
            });
        #endregion

        #region Import
        public async Task<bool> Import(string path) =>
            await Task.Run(() => {
                try {
                    if (string.IsNullOrWhiteSpace(path))
                        throw new Exception("import path is empty..");

                    if (!File.Exists(path))
                        throw new Exception($"import file not found: {path}");

                    Name = Path.GetFileNameWithoutExtension(path);
                    string[] ss = File.ReadAllLines(path);
                    foreach (string s in ss)
                    {
                        if (s.Length == 0) continue;
                        Match m = Regex.Match(s, @"^(\S+)\s?=\s?(.+)$",
                            RegexOptions.CultureInvariant |
                            RegexOptions.Multiline |
                            RegexOptions.IgnoreCase |
                            RegexOptions.Compiled);

                        if ((!m.Success) || (m.Groups.Count != 3))
                            continue;

                        string key = m.Groups[1].Value,
                               val = m.Groups[2].Value;

                        ImportSetValues(key, val);
                    }
                    return !IsEmpty;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Import), ex); }
                return false;
            });

        public async Task<bool> ImportFromString(string s) =>
            await Task.Run(() => {
                try {
                    if (string.IsNullOrWhiteSpace(s))
                        throw new Exception("import string is empty..");

                    Regex r = new(@"\[(?<grp>\w+)\]|(?<key>\w+)\s?=\s?(?<val>[\:\=\.\+\/A-Za-z0-9]+)\s?",
                        RegexOptions.CultureInvariant |
                        RegexOptions.Singleline |
                        RegexOptions.IgnoreCase |
                        RegexOptions.ExplicitCapture |
                        RegexOptions.Compiled);

                    MatchCollection matches = r.Matches(s);
                    if (matches.Count > 0) {

                        string[] names = r.GetGroupNames();
                        string group = string.Empty;
                        foreach (Match m in matches) {

                            Group grp = m.Groups["grp"];
                            if ((grp != null) && !string.IsNullOrWhiteSpace(grp.Value)) {
                                group = grp.Value;
                                continue;
                            }
                            if (string.IsNullOrWhiteSpace(group))
                                continue;

                            string key = string.Empty,
                                   val = string.Empty;

                            foreach (string name in names) {
                                if (name.Equals("key"))
                                    key = m.Groups[name].Value;
                                else if (name.Equals("val"))
                                    val = m.Groups[name].Value;
                            }
                            ImportSetValues(key, val);
                        }
                    }
                    return !IsEmpty;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Import), ex); }
                return false;
            });

        private void ImportSetValues(string key, string val) {

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val))
                return;

            val = val.Trim();
            key = key.Trim();

            if (key.Equals(nameof(VpnInterface.PrivateKey)))
                Interface.PrivateKey = val;
            else if (key.Equals(nameof(VpnInterface.Address)))
                Interface.Address = val;
            else if (key.Equals(nameof(VpnInterface.DNS)))
                Interface.DNS = val;
            else if (key.Equals(nameof(VpnInterface.MTU)))
                Interface.MTU = val;
            else if (key.Equals(nameof(VpnPeer.PublicKey)))
                Peer.PublicKey = val;
            else if (key.Equals(nameof(VpnPeer.PresharedKey)))
                Peer.PresharedKey = val;
            else if (key.Equals(nameof(VpnPeer.Endpoint)))
                Peer.Endpoint = val;
            else if (key.Equals(nameof(VpnPeer.AllowedIPs)))
                Peer.AllowedIPs = val;
            else if (key.Equals(nameof(VpnPeer.PersistentKeepalive)))
                Peer.PersistentKeepalive = short.Parse(val);
        }
        #endregion

        public async Task<bool> Load(string path) =>
            await Task.Run(() => {
                try {
                    VpnAccount acc = path.DeserializeFromFile<VpnAccount>();
                    return Copy(acc);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Load), ex); }
                return false;
            });

        public async Task<bool> Save(string path) =>
            await Task.Run(() => {
                try {
                    path.SerializeToFile(this);
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Save), ex); }
                return false;
            });
    }
}

/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SecyrityMail.Data;
using SecyrityMail.GnuPG;
using SecyrityMail.Proxy;
using SecyrityMail.Utils;

namespace SecyrityMail
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class Configuration : MailEvent, IMailEventProxy, IConfiguration
    {
        private bool _pgpOnce = false,
                     _pgpOnceCheck = false,
                     _isVpnEnable = false,
                     _smtpAllOutPgpSign = false,
                     _smtpAllOutPgpCrypt = false,
                     _incommingPgpDecrypt = false;

        public string PgpPassword { get; set; } = string.Empty;
        public string PgpKeyHost { get; set; } = string.Empty;

        [XmlIgnore]
        public bool IsIncommingPgpDecrypt { get => _incommingPgpDecrypt; set => _incommingPgpDecrypt = pgpOnce(value); }
        [XmlIgnore]
        public bool IsSmtpAllOutPgpSign { get => _smtpAllOutPgpSign; set => _smtpAllOutPgpSign = pgpOnce(value); }
        [XmlIgnore]
        public bool IsSmtpAllOutPgpCrypt { get => _smtpAllOutPgpCrypt; set => _smtpAllOutPgpCrypt = pgpOnce(value); }

        public bool IsSmtpDeliveryLocal { get; set; } = false;
        public bool IsSmtpCheckFrom { get; set; } = true;
        public bool IsSmtpEnable { get; set; } = false;
        public bool IsSmtpSecure { get; set; } = false;
        public bool IsSmtpLog { get; set; } = false;
        public int  SmtpServicePort { get; set; } = 25;
        public double SmtpClientIdle { get; set; } = 15.0;

        public bool IsPop3DeleteAllMessages { get; set; } = false;
        public bool IsPop3Enable { get; set; } = false;
        public bool IsPop3Secure { get; set; } = false;
        public bool IsPop3Log { get; set; } = false;

        public int    Pop3ServicePort { get; set; } = 110;
        public double Pop3ClientIdle { get; set; } = 20.0;

        public int  ClientTimeout { get; set; } = 4 * 60 * 1000;
        public bool IsSharingSocket { get; set; } = false;
        public bool IsSaveAttachments { get; set; } = false;
        public bool IsAlwaysNewMessageId { get; set; } = false;
        public bool IsImapClientMessagePurge { get; set; } = false;
        public bool IsSmtpClientFakeIp { get; set; } = false;
        public bool IsModifyMessageDeliveredLocal { get; set; } = true;
        public bool IsNewMessageSendImmediately { get; set; } = false;
        public bool IsProxyListRepack { get; set; } = true;
        public bool IsReceiveOnSendOnly { get => Global.Instance.Tasks.IsReceiveOnSendOnly; set => Global.Instance.Tasks.IsReceiveOnSendOnly = value; }
        public TimeSpan CheckMailPeriod { get => Global.Instance.Tasks.CheckMailPeriod; set => Global.Instance.Tasks.CheckMailPeriod = value; }

        public string VpnDnsDefault { get; set; } = string.Empty;
        public string VpnAllowedIPDefault { get; set; } = string.Empty;
        public string CheckProxyEndPointUrl { get; set; } = "http://api.ipify.org:80?format=json"; // "http://api.myip.com:80";

        public string ServicesInterfaceName { get; set; } = string.Empty;
        public string ServicesInterfaceIp { get; set; } = string.Empty;

        public int    SpamCheckCount { get; set; } = 3;
        public double SpamClientIdle { get; set; } = 20.0; /* min */
        public bool   IsAccessIpWhiteList { get; set; } = false;
        public bool   IsAccessIpCheckDns { get; set; } = false;
        public bool   IsDnsblIpCheck { get; set; } = false;
        public string DnsblHost { get; set; } = string.Empty;

        public bool IsSpamCheckAkismet { get; set; } = false;
        public bool IsAkismetLearn { get; set; } = false;
        public string SpamCheckAkismetKey { get; set; } = string.Empty;

        public List<string> ForbidenRouteList { get; set; } = new();
        public List<string> ForbidenEntryList { get; set; } = new();

        public bool IsCacheMessagesLog {
            get => Global.Instance.MessagesManager.IsCacheMessagesLog;
            set { Global.Instance.MessagesManager.IsCacheMessagesLog = value; OnPropertyChanged(); }
        }

        private ProxyType proxyType = ProxyType.None;
        public ProxyType ProxyType {
            get => proxyType;
            set { Global.Instance.ProxyList.ProxyType = proxyType = value; OnPropertyChanged(); }
        }

        public bool IsVpnAlways {
            get => _isVpnEnable;
            set { _isVpnEnable = value; OnPropertyChanged(); }
        }
        public bool IsVpnEnable {
            get => Global.Instance.VpnAccounts.IsAccountSelected && _isVpnEnable;
            set { _isVpnEnable = value; OnPropertyChanged(); }
        }
        public bool IsVpnRandom {
            get => Global.Instance.Vpn.IsVpnRandom;
            set { Global.Instance.Vpn.IsVpnRandom = value; OnPropertyChanged(); }
        }
        public bool IsEnableLogVpn {
            get => Global.Instance.Vpn.IsEnableLogVpn;
            set { Global.Instance.Vpn.IsEnableLogVpn = value; OnPropertyChanged(); }
        }
        [XmlIgnore]
        public IPEndPoint VpnEndpoint => Global.Instance.VpnAccounts.IpEndpoint;
        [XmlIgnore]
        public bool IsVpnSelected => Global.Instance.VpnAccounts.IsAccountSelected;
        [XmlIgnore]
        public bool IsVpnReady => Global.Instance.Vpn.IsVpnReady;
        [XmlIgnore]
        public bool IsVpnBegin => Global.Instance.Vpn.IsVpnBegin;
        [XmlIgnore]
        public bool IsVpnTunnelRunning => Global.Instance.Vpn.IsTunnelRunning;
        [XmlIgnore]
        public bool IsProxyCheckRun => Global.Instance.Proxy.IsProxyCheck;
        [XmlIgnore]
        public bool IsSshSelected => Global.Instance.SshProxy.IsAccountSelected;
        [XmlIgnore]
        public bool IsSshRunning => Global.Instance.SshProxy.IsAccountRunning;
        [XmlIgnore]
        public bool IsCheckMailRun => Global.Instance.Tasks.IsCheckMailRun;

        private bool pgpOnce(bool b)
        {
            if (_pgpOnce)
                return _pgpOnceCheck && b;

            if (!b) return false;
            _pgpOnce = true;
            _pgpOnceCheck = CryptGpgContext.CheckInstalled();
            return _pgpOnceCheck;
        }

        #region Copy
        public void Copy(IConfiguration cfg, bool isfull = true)
        {
            if (cfg == null)
                throw new ArgumentNullException(nameof(IConfiguration));

            if (isfull) {
                IsIncommingPgpDecrypt = cfg.IsIncommingPgpDecrypt;
                IsSmtpAllOutPgpCrypt = cfg.IsSmtpAllOutPgpCrypt;
                IsSmtpAllOutPgpSign = cfg.IsSmtpAllOutPgpSign;
            }

            CheckMailPeriod = cfg.CheckMailPeriod;
            ClientTimeout = cfg.ClientTimeout;
            IsAlwaysNewMessageId = cfg.IsAlwaysNewMessageId;
            IsCacheMessagesLog = cfg.IsCacheMessagesLog;
            IsEnableLogVpn = cfg.IsEnableLogVpn;
            IsImapClientMessagePurge = cfg.IsImapClientMessagePurge;
            IsModifyMessageDeliveredLocal = cfg.IsModifyMessageDeliveredLocal;
            IsNewMessageSendImmediately = cfg.IsNewMessageSendImmediately;
            IsPop3DeleteAllMessages = cfg.IsPop3DeleteAllMessages;
            IsPop3Log = cfg.IsPop3Log;
            IsPop3Secure = cfg.IsPop3Secure;
            IsPop3Enable = cfg.IsPop3Enable;
            IsReceiveOnSendOnly = cfg.IsReceiveOnSendOnly;
            IsSaveAttachments = cfg.IsSaveAttachments;
            IsSmtpCheckFrom = cfg.IsSmtpCheckFrom;
            IsSmtpClientFakeIp = cfg.IsSmtpClientFakeIp;
            IsSmtpDeliveryLocal = cfg.IsSmtpDeliveryLocal;
            IsSmtpLog = cfg.IsSmtpLog;
            IsSmtpSecure = cfg.IsSmtpSecure;
            IsSmtpEnable = cfg.IsSmtpEnable;
            IsVpnAlways = cfg.IsVpnAlways;
            IsVpnEnable = cfg.IsVpnEnable;
            IsVpnRandom = cfg.IsVpnRandom;
            IsProxyListRepack = cfg.IsProxyListRepack;
            IsSharingSocket = cfg.IsSharingSocket;
            IsAccessIpWhiteList = cfg.IsAccessIpWhiteList;
            IsAccessIpCheckDns = cfg.IsAccessIpCheckDns;
            IsDnsblIpCheck = cfg.IsDnsblIpCheck;
            IsAkismetLearn = cfg.IsAkismetLearn;
            IsSpamCheckAkismet = cfg.IsSpamCheckAkismet;
            SpamCheckAkismetKey = cfg.SpamCheckAkismetKey;
            DnsblHost = cfg.DnsblHost;
            PgpKeyHost = cfg.PgpKeyHost;
            PgpPassword = cfg.PgpPassword;
            Pop3ClientIdle = cfg.Pop3ClientIdle;
            Pop3ServicePort = cfg.Pop3ServicePort;
            ProxyType = cfg.ProxyType;
            SmtpClientIdle = cfg.SmtpClientIdle;
            SmtpServicePort = cfg.SmtpServicePort;
            SpamCheckCount = cfg.SpamCheckCount;
            SpamClientIdle = cfg.SpamClientIdle;
            VpnAllowedIPDefault = cfg.VpnAllowedIPDefault;
            VpnDnsDefault = cfg.VpnDnsDefault;
            ServicesInterfaceName = cfg.ServicesInterfaceName;
            ServicesInterfaceIp = cfg.ServicesInterfaceIp;

            ForbidenRouteList.Clear();
            if ((cfg.ForbidenRouteList != default) && (cfg.ForbidenRouteList.Count > 0))
                ForbidenRouteList.AddRange(cfg.ForbidenRouteList);

            ForbidenEntryList.Clear();
            if ((cfg.ForbidenEntryList != default) && (cfg.ForbidenEntryList.Count > 0))
                ForbidenEntryList.AddRange(cfg.ForbidenEntryList);
        }
        #endregion

        #region Load
        public async Task<bool> Load() =>
            await Load(Path.Combine(Global.GetRootDirectory(), $"{nameof(Configuration)}.conf")).ConfigureAwait(false);
        public async Task<bool> Load(string path) =>
            await Task.Run(() => {
                try {
                    if (!File.Exists(path))
                        return false;
                    Configuration conf = path.DeserializeFromFile<Configuration>();
                    Copy(conf, false);
                    return true;
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(Load), ex); }
                return false;
            });
        #endregion

        #region Save
        public async Task<bool> Save() =>
            await Save(Path.Combine(Global.GetRootDirectory(), $"{nameof(Configuration)}.conf")).ConfigureAwait(false);
        public async Task<bool> Save(string path) =>
            await Task.Run(() => {
                try {
                    path.SerializeToFile<Configuration>(this);
                    return true;
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(Save), ex); }
                return false;
            });
        #endregion
    }
}

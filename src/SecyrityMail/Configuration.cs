
using System;
using System.Collections.Generic;
using System.Net;
using SecyrityMail.Data;
using SecyrityMail.Proxy;

namespace SecyrityMail
{
    public class Configuration : MailEvent, IMailEventProxy, IConfiguration
    {
        private bool _pgpOnce = false,
                     _pgpOnceCheck = false,
                     _isVpnEnable = false,
                     _smtpAllOutPgpSign = false,
                     _smtpAllOutPgpCrypt = false,
                     _incommingPgpDecrypt = false;

        public string PgpPassword { get; set; } = string.Empty;
        public bool IsIncommingPgpDecrypt { get => _incommingPgpDecrypt; set => _incommingPgpDecrypt = pgpOnce(value); }

        public bool IsSharingSocket { get; set; } = false;
        public bool IsSmtpAllOutPgpSign { get => _smtpAllOutPgpSign; set => _smtpAllOutPgpSign = pgpOnce(value); }
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
        public bool IsSaveAttachments { get; set; } = false;
        public bool IsAlwaysNewMessageId { get; set; } = false;
        public bool IsImapClientMessagePurge { get; set; } = false;
        public bool IsSmtpClientFakeIp { get; set; } = false;
        public bool IsModifyMessageDeliveredLocal { get; set; } = true;
        public bool IsNewMessageSendImmediately { get; set; } = false;
        public bool IsProxyListRepack { get; set; } = true;
        public bool IsReceiveOnSendOnly { get => Global.Instance.Tasks.IsReceiveOnSendOnly; set => Global.Instance.Tasks.IsReceiveOnSendOnly = value; }
        public TimeSpan CheckMailPeriod { get => Global.Instance.Tasks.CheckMailPeriod; set => Global.Instance.Tasks.CheckMailPeriod = value; }
        public MailEventId ServicesEventId { get => Global.Instance.Tasks.ServicesEventId; set => Global.Instance.Tasks.ServicesEventId = value; }

        public string VpnDnsDefault { get; set; } = string.Empty;
        public string VpnAllowedIPDefault { get; set; } = string.Empty;
        public string CheckProxyEndPointUrl { get; set; } = "http://api.ipify.org:80?format=json"; // "http://api.myip.com:80";

        public string ServicesInterfaceName { get; set; } = string.Empty;
        public string ServicesInterfaceIp { get; set; } = string.Empty;

        public int    SpamCheckCount { get; set; } = 3;
        public double SpamClientIdle { get; set; } = 20.0; /* min */
        public bool   IsAccessIpWhiteList { get; set; } = false;

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
        public IPEndPoint VpnEndpoint => Global.Instance.VpnAccounts.IpEndpoint;
        public bool IsVpnSelected => Global.Instance.VpnAccounts.IsAccountSelected;
        public bool IsVpnReady => Global.Instance.Vpn.IsVpnReady;
        public bool IsVpnBegin => Global.Instance.Vpn.IsVpnBegin;
        public bool IsVpnTunnelRunning => Global.Instance.Vpn.IsTunnelRunning;
        public bool IsProxyCheckRun => Global.Instance.Proxy.IsProxyCheck;
        public bool IsSshSelected => Global.Instance.SshProxy.IsAccountSelected;
        public bool IsSshRunning => Global.Instance.SshProxy.IsAccountRunning;
        public bool IsCheckMailRun => Global.Instance.Tasks.IsCheckMailRun;

        private bool pgpOnce(bool b)
        {
            if (_pgpOnce)
                return _pgpOnceCheck && b;

            if (!b) return false;
            _pgpOnce = true;
            _pgpOnceCheck = CryptPGContext.CheckInstalled();
            return _pgpOnceCheck;
        }

        public void Copy(IConfiguration cfg)
        {
            if (cfg == null)
                throw new ArgumentNullException(nameof(IConfiguration));

            CheckMailPeriod = cfg.CheckMailPeriod;
            ClientTimeout = cfg.ClientTimeout;
            IsAlwaysNewMessageId = cfg.IsAlwaysNewMessageId;
            IsCacheMessagesLog = cfg.IsCacheMessagesLog;
            IsEnableLogVpn = cfg.IsEnableLogVpn;
            IsImapClientMessagePurge = cfg.IsImapClientMessagePurge;
            IsIncommingPgpDecrypt = cfg.IsIncommingPgpDecrypt;
            IsModifyMessageDeliveredLocal = cfg.IsModifyMessageDeliveredLocal;
            IsNewMessageSendImmediately = cfg.IsNewMessageSendImmediately;
            IsPop3DeleteAllMessages = cfg.IsPop3DeleteAllMessages;
            IsPop3Log = cfg.IsPop3Log;
            IsPop3Secure = cfg.IsPop3Secure;
            IsPop3Enable = cfg.IsPop3Enable;
            IsReceiveOnSendOnly = cfg.IsReceiveOnSendOnly;
            IsSaveAttachments = cfg.IsSaveAttachments;
            IsSmtpAllOutPgpCrypt = cfg.IsSmtpAllOutPgpCrypt;
            IsSmtpAllOutPgpSign = cfg.IsSmtpAllOutPgpSign;
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
            PgpPassword = cfg.PgpPassword;
            Pop3ClientIdle = cfg.Pop3ClientIdle;
            Pop3ServicePort = cfg.Pop3ServicePort;
            ProxyType = cfg.ProxyType;
            ServicesEventId = cfg.ServicesEventId;
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
    }
}

/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using SecyrityMail;
using SecyrityMail.Proxy;

namespace HomeMailHub
{
    internal class ConfigurationLoad : IConfiguration
    {
        public TimeSpan CheckMailPeriod { get; set; }
        public int ClientTimeout { get; set; }
        public bool IsAlwaysNewMessageId { get; set; }
        public bool IsCacheMessagesLog { get; set; }
        public bool IsEnableLogVpn { get; set; }
        public bool IsImapClientMessagePurge { get; set; }
        public bool IsIncommingPgpDecrypt { get; set; }
        public bool IsModifyMessageDeliveredLocal { get; set; }
        public bool IsNewMessageSendImmediately { get; set; }
        public bool IsPop3DeleteAllMessages { get; set; }
        public bool IsPop3Log { get; set; }
        public bool IsPop3Secure { get; set; }
        public bool IsPop3Enable { get; set; }
        public bool IsReceiveOnSendOnly { get; set; }
        public bool IsSaveAttachments { get; set; }
        public bool IsSmtpAllOutPgpCrypt { get; set; }
        public bool IsSmtpAllOutPgpSign { get; set; }
        public bool IsSmtpCheckFrom { get; set; }
        public bool IsSmtpClientFakeIp { get; set; }
        public bool IsSmtpDeliveryLocal { get; set; }
        public bool IsSmtpLog { get; set; }
        public bool IsSmtpSecure { get; set; }
        public bool IsSmtpEnable { get; set; }
        public bool IsVpnAlways { get; set; }
        public bool IsVpnEnable { get; set; }
        public bool IsVpnRandom { get; set; }
        public bool IsProxyListRepack { get; set; }
        public bool IsSharingSocket { get; set; }
        public bool IsAccessIpWhiteList { get; set; }
        public bool IsAccessIpCheckDns { get; set; }
        public bool IsDnsblIpCheck { get; set; }
        public string DnsblHost { get; set; }
        public string PgpPassword { get; set; }
        public string PgpKeyHost { get; set; }
        public double Pop3ClientIdle { get; set; }
        public int Pop3ServicePort { get; set; }
        public ProxyType ProxyType { get; set; }
        public double SmtpClientIdle { get; set; }
        public int SmtpServicePort { get; set; }
        public int SpamCheckCount { get; set; }
        public double SpamClientIdle { get; set; }
        public string VpnAllowedIPDefault { get; set; }
        public string VpnDnsDefault { get; set; }
        public string ServicesInterfaceName { get; set; }
        public string ServicesInterfaceIp { get; set; }
        public string CheckProxyEndPointUrl { get; set; }
        public List<string> ForbidenRouteList { get; set; } = default;
        public List<string> ForbidenEntryList { get; set; } = default;
        public List<string> FilterFromList { get; set; } = default;

        public bool IsSpamCheckAkismet { get; set; }
        public bool IsAkismetLearn { get; set; }
        public string SpamCheckAkismetKey { get; set; }

        public bool IsProxyCheckRun { get; } /* no set */
        public bool IsVpnTunnelRunning { get; } /* no set */
        public bool IsVpnBegin { get; } /* no set */
        public bool IsVpnReady { get; } /* no set */
        public bool IsVpnSelected { get; } /* no set */
        public bool IsSshSelected { get; } /* no set */
        public bool IsSshRunning { get; } /* no set */
        public bool IsCheckMailRun { get; } /* no set */
        public IPEndPoint VpnEndpoint { get; } /* no set */

        public ConfigurationLoad()
        {
            IsIncommingPgpDecrypt = Properties.Settings.Default.IsIncommingPgpDecrypt;
            IsSmtpAllOutPgpSign = Properties.Settings.Default.IsSmtpAllOutPgpSign;
            IsSmtpAllOutPgpCrypt = Properties.Settings.Default.IsSmtpAllOutPgpCrypt;
            IsSmtpDeliveryLocal = Properties.Settings.Default.IsSmtpDeliveryLocal;
            IsSmtpCheckFrom = Properties.Settings.Default.IsSmtpCheckFrom;
            IsSmtpEnable = Properties.Settings.Default.IsSmtpEnable;
            IsSmtpSecure = Properties.Settings.Default.IsSmtpSecure;
            IsSmtpLog = Properties.Settings.Default.IsSmtpLog;
            SmtpServicePort = (Properties.Settings.Default.SmtpServicePort <= 0) ? 25 : Properties.Settings.Default.SmtpServicePort;

            SmtpClientIdle = (double.IsNaN(Properties.Settings.Default.SmtpClientIdle) || (Properties.Settings.Default.SmtpClientIdle == 0.0)) ?
                15.0 : Properties.Settings.Default.SmtpClientIdle;

            IsPop3DeleteAllMessages = Properties.Settings.Default.IsPop3DeleteAllMessages;
            IsPop3Enable = Properties.Settings.Default.IsPop3Enable;
            IsPop3Secure = Properties.Settings.Default.IsPop3Secure;
            IsPop3Log = Properties.Settings.Default.IsPop3Log;
            Pop3ServicePort = (Properties.Settings.Default.Pop3ServicePort <= 0) ? 110 : Properties.Settings.Default.Pop3ServicePort;

            Pop3ClientIdle = (double.IsNaN(Properties.Settings.Default.Pop3ClientIdle) || (Properties.Settings.Default.Pop3ClientIdle == 0.0)) ?
                20.0 : Properties.Settings.Default.Pop3ClientIdle;

            SpamClientIdle = (double.IsNaN(Properties.Settings.Default.SpamClientIdle) || (Properties.Settings.Default.SpamClientIdle == 0.0)) ?
                20.0 : Properties.Settings.Default.SpamClientIdle;

            SpamCheckCount = (Properties.Settings.Default.SpamCheckCount <= 0) ? 3 : Properties.Settings.Default.SpamCheckCount;
            ClientTimeout = (Properties.Settings.Default.ClientTimeout == 0.0) ? (4 * 60 * 1000) : Properties.Settings.Default.ClientTimeout;

            IsSaveAttachments = Properties.Settings.Default.IsSaveAttachments;
            IsAlwaysNewMessageId = Properties.Settings.Default.IsAlwaysNewMessageId;
            IsImapClientMessagePurge = Properties.Settings.Default.IsImapClientMessagePurge;
            IsSmtpClientFakeIp = Properties.Settings.Default.IsSmtpClientFakeIp;
            IsModifyMessageDeliveredLocal = Properties.Settings.Default.IsModifyMessageDeliveredLocal;
            IsNewMessageSendImmediately = Properties.Settings.Default.IsNewMessageSendImmediately;
            IsReceiveOnSendOnly = Properties.Settings.Default.IsReceiveOnSendOnly;
            IsCacheMessagesLog = Properties.Settings.Default.IsCacheMessagesLog;
            IsVpnAlways = Properties.Settings.Default.IsVpnAlways;
            IsVpnEnable = Properties.Settings.Default.IsVpnEnable;
            IsVpnRandom = Properties.Settings.Default.IsVpnRandom;
            IsEnableLogVpn = Properties.Settings.Default.IsEnableLogVpn;
            IsProxyListRepack = Properties.Settings.Default.IsProxyListRepack;
            IsSharingSocket = Properties.Settings.Default.IsSharingSocket;
            IsAccessIpWhiteList = Properties.Settings.Default.IsAccessIpWhiteList;
            IsAccessIpCheckDns = Properties.Settings.Default.IsAccessIpCheckDns;
            IsDnsblIpCheck = Properties.Settings.Default.IsDnsblIpCheck;
            PgpPassword = string.IsNullOrWhiteSpace(Properties.Settings.Default.PgpPassword) ? string.Empty : Properties.Settings.Default.PgpPassword;
            PgpKeyHost = string.IsNullOrWhiteSpace(Properties.Settings.Default.PgpKeyHost) ? string.Empty : Properties.Settings.Default.PgpKeyHost;

            SpamCheckAkismetKey = Properties.Settings.Default.SpamCheckAkismetKey;
            IsSpamCheckAkismet = !string.IsNullOrWhiteSpace(SpamCheckAkismetKey) && Properties.Settings.Default.IsSpamCheckAkismet;
            IsAkismetLearn = !string.IsNullOrWhiteSpace(SpamCheckAkismetKey) && Properties.Settings.Default.IsAkismetLearn;

            CheckMailPeriod = (!double.IsNaN(Properties.Settings.Default.CheckMailPeriod) &&
                (Properties.Settings.Default.CheckMailPeriod > 0.0) && (Properties.Settings.Default.CheckMailPeriod < 721.0)) ?
                    TimeSpan.FromHours(Properties.Settings.Default.CheckMailPeriod) : Timeout.InfiniteTimeSpan;

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.ServicesInterfaceName))
                ServicesInterfaceName = Properties.Settings.Default.ServicesInterfaceName;

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.ServicesInterfaceIp))
                ServicesInterfaceIp = Properties.Settings.Default.ServicesInterfaceIp;

            if (string.IsNullOrWhiteSpace(ServicesInterfaceName) && string.IsNullOrWhiteSpace(ServicesInterfaceIp))
                ServicesInterfaceIp = "*";

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.ProxyType) &&
                Enum.TryParse(Properties.Settings.Default.ProxyType, out ProxyType pt))
                ProxyType = pt;

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.VpnDnsDefault))
                VpnDnsDefault = Properties.Settings.Default.VpnDnsDefault;

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.VpnAllowedIPDefault))
                VpnAllowedIPDefault = Properties.Settings.Default.VpnAllowedIPDefault;

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.CheckProxyEndPointUrl))
                CheckProxyEndPointUrl = Properties.Settings.Default.CheckProxyEndPointUrl;

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.DnsblHost))
                DnsblHost = Properties.Settings.Default.DnsblHost;

            if ((Properties.Settings.Default.ForbidenRouteList != default) && (Properties.Settings.Default.ForbidenRouteList.Count > 0)) {
                string[] ss = new string[Properties.Settings.Default.ForbidenRouteList.Count];
                Properties.Settings.Default.ForbidenRouteList.CopyTo(ss, 0);
                ForbidenRouteList = new(ss);
            }

            if ((Properties.Settings.Default.FilterFromList != default) && (Properties.Settings.Default.FilterFromList.Count > 0)) {
                string[] ss = new string[Properties.Settings.Default.FilterFromList.Count];
                Properties.Settings.Default.FilterFromList.CopyTo(ss, 0);
                FilterFromList = new(ss);
            }
        }

        public void Copy(IConfiguration cfg, bool isfull = true) { }
    }
}

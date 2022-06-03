/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Net;
using SecyrityMail.Data;
using SecyrityMail.Proxy;

namespace SecyrityMail
{
    public interface IConfiguration
    {
        TimeSpan CheckMailPeriod { get; set; }
        int  ClientTimeout { get; set; }
        bool IsAlwaysNewMessageId { get; set; }
        bool IsCacheMessagesLog { get; set; }
        bool IsEnableLogVpn { get; set; }
        bool IsImapClientMessagePurge { get; set; }
        bool IsIncommingPgpDecrypt { get; set; }
        bool IsModifyMessageDeliveredLocal { get; set; }
        bool IsNewMessageSendImmediately { get; set; }
        bool IsPop3DeleteAllMessages { get; set; }
        bool IsPop3Log { get; set; }
        bool IsPop3Secure { get; set; }
        bool IsPop3Enable { get; set; }
        bool IsReceiveOnSendOnly { get; set; }
        bool IsSaveAttachments { get; set; }
        bool IsSmtpAllOutPgpCrypt { get; set; }
        bool IsSmtpAllOutPgpSign { get; set; }
        bool IsSmtpCheckFrom { get; set; }
        bool IsSmtpClientFakeIp { get; set; }
        bool IsSmtpDeliveryLocal { get; set; }
        bool IsSmtpLog { get; set; }
        bool IsSmtpSecure { get; set; }
        bool IsSmtpEnable { get; set; }
        bool IsVpnTunnelRunning { get; }
        bool IsVpnBegin { get; }
        bool IsVpnAlways { get; set; }
        bool IsVpnEnable { get; set; }
        bool IsVpnRandom { get; set; }
        bool IsVpnReady { get; }
        bool IsVpnSelected { get; }
        bool IsProxyCheckRun { get; }
        bool IsSshSelected { get; }
        bool IsSshRunning { get; }
        bool IsProxyListRepack { get; set; }
        bool IsSharingSocket { get; set; }
        bool IsAccessIpWhiteList { get; set; }
        bool IsAccessIpCheckDns { get; set; }
        bool IsDnsblIpCheck { get; set; }
        bool IsCheckMailRun { get; }

        string ServicesInterfaceName { get; set; }
        string ServicesInterfaceIp { get; set; }
        List<string> ForbidenRouteList { get; set; }
        List<string> ForbidenEntryList { get; set; }

        string PgpPassword { get; set; }
        string PgpKeyHost { get; set; }
        string DnsblHost { get; set; }

        double Pop3ClientIdle { get; set; }
        int Pop3ServicePort { get; set; }
        ProxyType ProxyType { get; set; }
        double SmtpClientIdle { get; set; }
        int SmtpServicePort { get; set; }
        int SpamCheckCount { get; set; }
        double SpamClientIdle { get; set; }
        string VpnAllowedIPDefault { get; set; }
        string VpnDnsDefault { get; set; }
        IPEndPoint VpnEndpoint { get; }

        string CheckProxyEndPointUrl { get; set; }

        void Copy(IConfiguration cfg);
    }
}
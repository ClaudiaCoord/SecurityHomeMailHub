/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Threading.Tasks;
using SecyrityMail;

namespace HomeMailHub
{
    internal class ConfigurationSave
    {
        public async Task<bool> Save() => await Save(Global.Instance.Config);
        public async Task<bool> Save(IConfiguration cfg)
        {
            if (cfg == null)
                throw new ArgumentNullException(nameof(IConfiguration));

            return await Task.Run(async () => {

                Properties.Settings.Default.CheckMailPeriod = cfg.CheckMailPeriod.TotalMinutes;
                Properties.Settings.Default.ClientTimeout = cfg.ClientTimeout;
                Properties.Settings.Default.IsAlwaysNewMessageId = cfg.IsAlwaysNewMessageId;
                Properties.Settings.Default.IsCacheMessagesLog = cfg.IsCacheMessagesLog;
                Properties.Settings.Default.IsEnableLogVpn = cfg.IsEnableLogVpn;
                Properties.Settings.Default.IsImapClientMessagePurge = cfg.IsImapClientMessagePurge;
                Properties.Settings.Default.IsIncommingPgpDecrypt = cfg.IsIncommingPgpDecrypt;
                Properties.Settings.Default.IsModifyMessageDeliveredLocal = cfg.IsModifyMessageDeliveredLocal;
                Properties.Settings.Default.IsNewMessageSendImmediately = cfg.IsNewMessageSendImmediately;
                Properties.Settings.Default.IsPop3DeleteAllMessages = cfg.IsPop3DeleteAllMessages;
                Properties.Settings.Default.IsPop3Log = cfg.IsPop3Log;
                Properties.Settings.Default.IsPop3Secure = cfg.IsPop3Secure;
                Properties.Settings.Default.IsPop3Enable = cfg.IsPop3Enable;
                Properties.Settings.Default.IsReceiveOnSendOnly = cfg.IsReceiveOnSendOnly;
                Properties.Settings.Default.IsSaveAttachments = cfg.IsSaveAttachments;
                Properties.Settings.Default.IsSmtpAllOutPgpCrypt = cfg.IsSmtpAllOutPgpCrypt;
                Properties.Settings.Default.IsSmtpAllOutPgpSign = cfg.IsSmtpAllOutPgpSign;
                Properties.Settings.Default.IsSmtpCheckFrom = cfg.IsSmtpCheckFrom;
                Properties.Settings.Default.IsSmtpClientFakeIp = cfg.IsSmtpClientFakeIp;
                Properties.Settings.Default.IsSmtpDeliveryLocal = cfg.IsSmtpDeliveryLocal;
                Properties.Settings.Default.IsSmtpLog = cfg.IsSmtpLog;
                Properties.Settings.Default.IsSmtpSecure = cfg.IsSmtpSecure;
                Properties.Settings.Default.IsSmtpEnable = cfg.IsSmtpEnable;
                Properties.Settings.Default.IsVpnAlways = cfg.IsVpnAlways;
                Properties.Settings.Default.IsVpnEnable = cfg.IsVpnEnable;
                Properties.Settings.Default.IsVpnRandom = cfg.IsVpnRandom;
                Properties.Settings.Default.IsProxyListRepack = cfg.IsProxyListRepack;
                Properties.Settings.Default.IsSharingSocket = cfg.IsSharingSocket;
                Properties.Settings.Default.IsAccessIpWhiteList = cfg.IsAccessIpWhiteList;
                Properties.Settings.Default.PgpKeyHost = cfg.PgpKeyHost;
                Properties.Settings.Default.PgpPassword = cfg.PgpPassword;
                Properties.Settings.Default.Pop3ClientIdle = cfg.Pop3ClientIdle;
                Properties.Settings.Default.Pop3ServicePort = cfg.Pop3ServicePort;
                Properties.Settings.Default.ProxyType = cfg.ProxyType.ToString();
                Properties.Settings.Default.SmtpClientIdle = cfg.SmtpClientIdle;
                Properties.Settings.Default.SmtpServicePort = cfg.SmtpServicePort;
                Properties.Settings.Default.SpamCheckCount = cfg.SpamCheckCount;
                Properties.Settings.Default.SpamClientIdle = cfg.SpamClientIdle;
                Properties.Settings.Default.VpnAllowedIPDefault = cfg.VpnAllowedIPDefault;
                Properties.Settings.Default.VpnDnsDefault = cfg.VpnDnsDefault;
                Properties.Settings.Default.ServicesInterfaceName = cfg.ServicesInterfaceName;
                Properties.Settings.Default.ServicesInterfaceIp = cfg.ServicesInterfaceIp;

                Properties.Settings.Default.ForbidenRouteList = new();
                if ((cfg.ForbidenRouteList != default) && (cfg.ForbidenRouteList.Count > 0))
                    Properties.Settings.Default.ForbidenRouteList.AddRange(cfg.ForbidenRouteList.ToArray());

                Properties.Settings.Default.ForbidenEntryList = new();
                if ((cfg.ForbidenEntryList != default) && (cfg.ForbidenEntryList.Count > 0))
                    Properties.Settings.Default.ForbidenEntryList.AddRange(cfg.ForbidenEntryList.ToArray());

                Properties.Settings.Default.Save();
                await Global.Instance.AccountsSave().ConfigureAwait(false);
                return true;
            });
        }
    }
}

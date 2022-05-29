/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Reflection;
using System.Threading;
using HomeMailHub.CmdLine;
using HomeMailHub.Properties;
using SecyrityMail.Proxy;

namespace HomeMailHub
{
    public enum LanguageSupport : int
    {
        English = 0,
        Russian,
        Default,
        En = 0,
        Ru = 1
    }

    internal class Options
    {
        [CmdOption(Key = "--help", IsSwitch = true, ResourceId = "CMD_LINE_H1")]
        public bool IsHelp { get; set; } = false;
        [CmdOption(Key = "--lang", IsEnum = true, ResourceId = "CMD_LINE_H2")]
        public LanguageSupport LanguageOverride { get; set; } = LanguageSupport.Default;
        [CmdOption(Key = "--log", IsSwitch = true, ResourceId = "CMD_LINE_H3")]
        public bool IsAllLog { get; set; } = false;
        [CmdOption(Key = "--not-pop3", IsSwitch = true, ResourceId = "CMD_LINE_H4")]
        public bool IsNotPop3Enable { get; set; } = false;
        [CmdOption(Key = "--not-smtp", IsSwitch = true, ResourceId = "CMD_LINE_H5")]
        public bool IsNotSmtpEnable { get; set; } = false;
        [CmdOption(Key = "--proxy-repack", IsSwitch = true, ResourceId = "MENU_PROXYREPACK")]
        public bool IsProxyListRepack { get; set; } = false;
        [CmdOption(Key = "--vpn-always", IsSwitch = true, ResourceId = "MENU_VPNALWAYS")]
        public bool IsVpnAlways { get; set; } = false;
        [CmdOption(Key = "--msg-antispy", IsSwitch = true, ResourceId = "CHKBOX_DELIVERYMSGMOD")]
        public bool IsModifyMessageDeliveredLocal { get; set; } = false;
        [CmdOption(Key = "--msg-newid", IsSwitch = true, ResourceId = "CHKBOX_NEWMSGID")]
        public bool IsAlwaysNewMessageId { get; set; } = false;
        [CmdOption(Key = "--msg-fakeip", IsSwitch = true, ResourceId = "CHKBOX_SMTPFAKEIP")]
        public bool IsSmtpClientFakeIp { get; set; } = false;
        [CmdOption(Key = "--msg-from", IsSwitch = true, ResourceId = "CHKBOX_CHECKFROM")]
        public bool IsSmtpCheckFrom { get; set; } = false;
        [CmdOption(Key = "--msg-attach-save", IsSwitch = true, ResourceId = "CHKBOX_SAVEATTACH")]
        public bool IsSaveAttachments { get; set; } = false;
        [CmdOption(Key = "--mail-check", ResourceId = "TAG_CLIENTSMAILPERIODHOURS")]
        public double CheckMailPeriod { get; set; } = 0.0;
        [CmdOption(Key = "--proxy-type", IsEnum = true, ResourceId = "CMD_LINE_H6")]
        public ProxyType ProxyTypeOverride { get; set; } = ProxyType.None;
        [CmdOption(Key = "--pgp-out-crypt", IsSwitch = true, ResourceId = "CHKBOX_OUT_PGPCRYPT")]
        public bool IsSmtpAllOutPgpCrypt { get; set; } = false;
        [CmdOption(Key = "--pgp-out-sign", IsSwitch = true, ResourceId = "CHKBOX_OUT_PGPSIGN")]
        public bool IsSmtpAllOutPgpSign { get; set; } = false;
        [CmdOption(Key = "--pgp-in-decrypt", IsSwitch = true, ResourceId = "CHKBOX_IN_PGP")]
        public bool IsIncommingPgpDecrypt { get; set; } = false;
        [CmdOption(Key = "--pgp-key-server", ResourceId = "CMD_LINE_H7")]
        public string PgpKeyHost { get; set; } = string.Empty;

        public bool Check()
        {
            string lang = LanguageOverride switch {
                LanguageSupport.Russian => "ru",
                LanguageSupport.English => "en",
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(lang))
                Thread.CurrentThread.CurrentCulture =
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(lang);

            if (IsHelp) {
                Console.WriteLine($"{Environment.NewLine}\tVersion: {Assembly.GetExecutingAssembly().GetName().Version}{Environment.NewLine}");
                Console.WriteLine($"\tUsing: {nameof(HomeMailHub)}.exe <options>{Environment.NewLine}");
                CmdOption.Help<Options>((a) => Resources.ResourceManager.GetString(a).Replace("_", ""));
                return false;
            }

            if (IsAllLog)
                Settings.Default.IsCacheMessagesLog =
                Settings.Default.IsEnableLogVpn =
                Settings.Default.IsPop3Log =
                Settings.Default.IsSmtpLog = true;

            if (IsVpnAlways)
                Settings.Default.IsVpnEnable = true;
            if (IsNotPop3Enable)
                Settings.Default.IsPop3Enable = false;
            if (IsNotSmtpEnable)
                Settings.Default.IsSmtpEnable = false;
            if (IsSmtpCheckFrom)
                Settings.Default.IsSmtpCheckFrom = true;
            if (IsProxyListRepack)
                Settings.Default.IsProxyListRepack = true;
            if (IsSaveAttachments)
                Settings.Default.IsSaveAttachments = true;
            if (IsSmtpClientFakeIp)
                Settings.Default.IsSmtpClientFakeIp = true;
            if (IsAlwaysNewMessageId)
                Settings.Default.IsAlwaysNewMessageId = true;
            if (IsModifyMessageDeliveredLocal)
                Settings.Default.IsModifyMessageDeliveredLocal = true;
            if (IsSmtpAllOutPgpCrypt)
                Settings.Default.IsSmtpAllOutPgpCrypt = true;
            if (IsSmtpAllOutPgpSign)
                Settings.Default.IsSmtpAllOutPgpSign = true;
            if (IsIncommingPgpDecrypt)
                Settings.Default.IsIncommingPgpDecrypt = true;

            if (ProxyTypeOverride != ProxyType.None)
                Settings.Default.ProxyType = ProxyTypeOverride.ToString();

            if (CheckMailPeriod > 0.0)
                Settings.Default.CheckMailPeriod = CheckMailPeriod;

            if (!string.IsNullOrWhiteSpace(PgpKeyHost))
                Settings.Default.PgpKeyHost = PgpKeyHost;

            return true;
        }
    }
}

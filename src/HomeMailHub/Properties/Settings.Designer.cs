﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HomeMailHub.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.3.0.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string PgpPassword {
            get {
                return ((string)(this["PgpPassword"]));
            }
            set {
                this["PgpPassword"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsIncommingPgpDecrypt {
            get {
                return ((bool)(this["IsIncommingPgpDecrypt"]));
            }
            set {
                this["IsIncommingPgpDecrypt"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsSmtpAllOutPgpSign {
            get {
                return ((bool)(this["IsSmtpAllOutPgpSign"]));
            }
            set {
                this["IsSmtpAllOutPgpSign"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsSmtpAllOutPgpCrypt {
            get {
                return ((bool)(this["IsSmtpAllOutPgpCrypt"]));
            }
            set {
                this["IsSmtpAllOutPgpCrypt"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsSmtpDeliveryLocal {
            get {
                return ((bool)(this["IsSmtpDeliveryLocal"]));
            }
            set {
                this["IsSmtpDeliveryLocal"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsSmtpCheckFrom {
            get {
                return ((bool)(this["IsSmtpCheckFrom"]));
            }
            set {
                this["IsSmtpCheckFrom"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsSmtpSecure {
            get {
                return ((bool)(this["IsSmtpSecure"]));
            }
            set {
                this["IsSmtpSecure"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsSmtpLog {
            get {
                return ((bool)(this["IsSmtpLog"]));
            }
            set {
                this["IsSmtpLog"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("25")]
        public int SmtpServicePort {
            get {
                return ((int)(this["SmtpServicePort"]));
            }
            set {
                this["SmtpServicePort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("15")]
        public double SmtpClientIdle {
            get {
                return ((double)(this["SmtpClientIdle"]));
            }
            set {
                this["SmtpClientIdle"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsPop3DeleteAllMessages {
            get {
                return ((bool)(this["IsPop3DeleteAllMessages"]));
            }
            set {
                this["IsPop3DeleteAllMessages"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsPop3Secure {
            get {
                return ((bool)(this["IsPop3Secure"]));
            }
            set {
                this["IsPop3Secure"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsPop3Log {
            get {
                return ((bool)(this["IsPop3Log"]));
            }
            set {
                this["IsPop3Log"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("110")]
        public int Pop3ServicePort {
            get {
                return ((int)(this["Pop3ServicePort"]));
            }
            set {
                this["Pop3ServicePort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("20")]
        public double Pop3ClientIdle {
            get {
                return ((double)(this["Pop3ClientIdle"]));
            }
            set {
                this["Pop3ClientIdle"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public int SpamCheckCount {
            get {
                return ((int)(this["SpamCheckCount"]));
            }
            set {
                this["SpamCheckCount"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("20")]
        public double SpamClientIdle {
            get {
                return ((double)(this["SpamClientIdle"]));
            }
            set {
                this["SpamClientIdle"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("240000")]
        public int ClientTimeout {
            get {
                return ((int)(this["ClientTimeout"]));
            }
            set {
                this["ClientTimeout"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsSaveAttachments {
            get {
                return ((bool)(this["IsSaveAttachments"]));
            }
            set {
                this["IsSaveAttachments"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsAlwaysNewMessageId {
            get {
                return ((bool)(this["IsAlwaysNewMessageId"]));
            }
            set {
                this["IsAlwaysNewMessageId"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsImapClientMessagePurge {
            get {
                return ((bool)(this["IsImapClientMessagePurge"]));
            }
            set {
                this["IsImapClientMessagePurge"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsSmtpClientFakeIp {
            get {
                return ((bool)(this["IsSmtpClientFakeIp"]));
            }
            set {
                this["IsSmtpClientFakeIp"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsModifyMessageDeliveredLocal {
            get {
                return ((bool)(this["IsModifyMessageDeliveredLocal"]));
            }
            set {
                this["IsModifyMessageDeliveredLocal"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsNewMessageSendImmediately {
            get {
                return ((bool)(this["IsNewMessageSendImmediately"]));
            }
            set {
                this["IsNewMessageSendImmediately"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsReceiveOnSendOnly {
            get {
                return ((bool)(this["IsReceiveOnSendOnly"]));
            }
            set {
                this["IsReceiveOnSendOnly"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("24")]
        public double CheckMailPeriod {
            get {
                return ((double)(this["CheckMailPeriod"]));
            }
            set {
                this["CheckMailPeriod"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1.1.1.1, 1.0.0.1, 8.8.8.8, 8.8.4.4")]
        public string VpnDnsDefault {
            get {
                return ((string)(this["VpnDnsDefault"]));
            }
            set {
                this["VpnDnsDefault"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.0.0.0/0")]
        public string VpnAllowedIPDefault {
            get {
                return ((string)(this["VpnAllowedIPDefault"]));
            }
            set {
                this["VpnAllowedIPDefault"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsCacheMessagesLog {
            get {
                return ((bool)(this["IsCacheMessagesLog"]));
            }
            set {
                this["IsCacheMessagesLog"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SshSock5")]
        public string ProxyType {
            get {
                return ((string)(this["ProxyType"]));
            }
            set {
                this["ProxyType"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsVpnEnable {
            get {
                return ((bool)(this["IsVpnEnable"]));
            }
            set {
                this["IsVpnEnable"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsEnableLogVpn {
            get {
                return ((bool)(this["IsEnableLogVpn"]));
            }
            set {
                this["IsEnableLogVpn"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string ServicesInterfaceName {
            get {
                return ((string)(this["ServicesInterfaceName"]));
            }
            set {
                this["ServicesInterfaceName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string ServicesInterfaceIp {
            get {
                return ((string)(this["ServicesInterfaceIp"]));
            }
            set {
                this["ServicesInterfaceIp"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsProxyListRepack {
            get {
                return ((bool)(this["IsProxyListRepack"]));
            }
            set {
                this["IsProxyListRepack"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection ForbidenRouteList {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["ForbidenRouteList"]));
            }
            set {
                this["ForbidenRouteList"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsSharingSocket {
            get {
                return ((bool)(this["IsSharingSocket"]));
            }
            set {
                this["IsSharingSocket"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://api.ipify.org:80?format=json")]
        public string CheckProxyEndPointUrl {
            get {
                return ((string)(this["CheckProxyEndPointUrl"]));
            }
            set {
                this["CheckProxyEndPointUrl"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsSmtpEnable {
            get {
                return ((bool)(this["IsSmtpEnable"]));
            }
            set {
                this["IsSmtpEnable"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsPop3Enable {
            get {
                return ((bool)(this["IsPop3Enable"]));
            }
            set {
                this["IsPop3Enable"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection ForbidenEntryList {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["ForbidenEntryList"]));
            }
            set {
                this["ForbidenEntryList"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsAccessIpWhiteList {
            get {
                return ((bool)(this["IsAccessIpWhiteList"]));
            }
            set {
                this["IsAccessIpWhiteList"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsGuiLightText {
            get {
                return ((bool)(this["IsGuiLightText"]));
            }
            set {
                this["IsGuiLightText"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsVpnRandom {
            get {
                return ((bool)(this["IsVpnRandom"]));
            }
            set {
                this["IsVpnRandom"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsVpnAlways {
            get {
                return ((bool)(this["IsVpnAlways"]));
            }
            set {
                this["IsVpnAlways"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsConfirmRestoreMessages {
            get {
                return ((bool)(this["IsConfirmRestoreMessages"]));
            }
            set {
                this["IsConfirmRestoreMessages"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsConfirmDeleteMessages {
            get {
                return ((bool)(this["IsConfirmDeleteMessages"]));
            }
            set {
                this["IsConfirmDeleteMessages"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string PgpBinPath {
            get {
                return ((string)(this["PgpBinPath"]));
            }
            set {
                this["PgpBinPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string PgpKeyHost {
            get {
                return ((string)(this["PgpKeyHost"]));
            }
            set {
                this["PgpKeyHost"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::System.Collections.Specialized.StringCollection SignaturesText {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["SignaturesText"]));
            }
            set {
                this["SignaturesText"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool IsAccessIpCheckDns {
            get {
                return ((bool)(this["IsAccessIpCheckDns"]));
            }
            set {
                this["IsAccessIpCheckDns"] = value;
            }
        }
    }
}

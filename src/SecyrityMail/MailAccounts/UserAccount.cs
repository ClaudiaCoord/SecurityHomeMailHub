/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SecyrityMail.Utils;

namespace SecyrityMail.MailAccounts
{
    public enum AccountUsing : int
    {
        None = 0,
        Pop3,
        Imap,
        Smtp
    }
    
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class UserAccount
    {
        [XmlElement("smtpport")]
        public int SmtpPort { get; set; } = -1;
        [XmlElement("smtpaddr")]
        public string SmtpAddr { get; set; } = string.Empty;
        [XmlElement("smtpsec")]
        public MailKit.Security.SecureSocketOptions SmtpSecure { get; set; } = MailKit.Security.SecureSocketOptions.Auto;

        [XmlElement("imapport")]
        public int ImapPort { get; set; } = -1;
        [XmlElement("imapaddr")]
        public string ImapAddr { get; set; } = string.Empty;
        [XmlElement("imapsec")]
        public MailKit.Security.SecureSocketOptions ImapSecure { get; set; } = MailKit.Security.SecureSocketOptions.Auto;

        [XmlElement("pop3port")]
        public int Pop3Port { get; set; } = -1;
        [XmlElement("pop3addr")]
        public string Pop3Addr { get; set; } = string.Empty;
        [XmlElement("pop3sec")]
        public MailKit.Security.SecureSocketOptions Pop3Secure { get; set; } = MailKit.Security.SecureSocketOptions.Auto;

        [XmlElement("login")]
        public string Login { get; set; } = string.Empty;
        [XmlElement("pass")]
        public string Pass { get; set; } = string.Empty;
        [XmlElement("name")]
        public string Name { get; set; } = string.Empty;
        [XmlElement("email")]
        public string EmailAddress { get; set; } = string.Empty;
        [XmlElement("replay")]
        public string ReplayTo { get; set; } = string.Empty;
        [XmlElement("pgpdecrypt")]
        public bool IsPgpAutoDecrypt { get; set; } = false;
        [XmlElement("enable")]
        public bool Enable { get; set; } = true;

        [XmlIgnore]
        public string Email => string.IsNullOrWhiteSpace(EmailAddress) ? Login : EmailAddress;

        [XmlIgnore]
        public AccountUsing CurrentAction { get; set; } = AccountUsing.None;

        [XmlIgnore]
        public bool IsEmpty => Login == string.Empty || Pass == string.Empty;
        [XmlIgnore]
        public bool IsEmptySend => SmtpPort == -1 || SmtpAddr == string.Empty || IsEmpty;
        [XmlIgnore]
        public bool IsEmptyImapReceive => ImapPort == -1 || ImapAddr == string.Empty || IsEmpty;
        [XmlIgnore]
        public bool IsEmptyPop3Receive => Pop3Port == -1 || Pop3Addr == string.Empty || IsEmpty;
        [XmlIgnore]
        public bool IsEmptyCredentials => string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Pass);

        public bool Copy(UserAccount acc)
        {
            if (acc == default)
                return false;

            SmtpPort = acc.SmtpPort;
            SmtpAddr = acc.SmtpAddr;
            SmtpSecure = acc.SmtpSecure;

            ImapPort = acc.ImapPort;
            ImapAddr = acc.ImapAddr;
            ImapSecure = acc.ImapSecure;

            Pop3Port = acc.Pop3Port;
            Pop3Addr = acc.Pop3Addr;
            Pop3Secure = acc.Pop3Secure;

            Enable = acc.Enable;
            Login = acc.Login;
            Pass = acc.Pass;
            Name = acc.Name;
            EmailAddress = acc.EmailAddress;
            IsPgpAutoDecrypt = acc.IsPgpAutoDecrypt;

            return !IsEmpty;
        }

        public async Task<bool> Load(string path = default) =>
            await Task.Run(() => {
                try {
                    UserAccount acc;
                    if (string.IsNullOrWhiteSpace(path))
                        acc = Global.GetRootFile(Global.DirectoryPlace.Root, $"{nameof(UserAccount)}.config")
                                        .DeserializeFromFile<UserAccount>();
                    else
                        acc = path.DeserializeFromFile<UserAccount>();
                    return Copy(acc);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Load), ex); }
                return false;
            });

        public async Task<bool> Save() =>
            await Save(
                Global.GetRootFile(Global.DirectoryPlace.Root, $"{nameof(UserAccount)}-{Global.GetValidFileName(Email)}.config")
                ).ConfigureAwait(false);

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

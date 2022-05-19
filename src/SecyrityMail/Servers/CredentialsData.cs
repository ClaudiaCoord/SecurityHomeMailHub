/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using SecyrityMail.MailAccounts;

namespace SecyrityMail.Servers
{
    public enum CredentialsCheckId : int
    {
        None = 0,
        Login,
        Password
    }

    internal class UserAccount
    {
        private MailAccounts.UserAccount account { get; set; } = null;

        public bool IsEmpty => (account == null) || account.IsEmptyCredentials;
        public bool IsEmptyImapReceive => (account == null) || account.IsEmptyImapReceive;
        public bool IsEmptySend => (account == null) || account.IsEmptySend;
        public bool IsPgpAutoDecrypt => (account == null) || account.IsPgpAutoDecrypt;
        public bool IsRootPath => !string.IsNullOrEmpty(UserRoot);
        public string Login => (account == null) ? string.Empty : account.Login;
        public string Pass => (account == null) ? string.Empty : account.Pass;
        public string Email => (account == null) ? string.Empty : account.Email;
        public string UserRoot { get; private set; } = string.Empty;
        public bool EmailCompare(string s) => account != null && s.Equals(account.Email);
        public bool LoginCompare(string s) => account != null && s.Equals(account.Login);
        public bool PasswordCompare(string s) => account != null && s.Equals(account.Pass);
        public void SetAccount(MailAccounts.UserAccount a) {
            account = a;
            UserRoot = (a != null) ? Global.GetUserDirectory(Email) : string.Empty;
        }
        public Tuple<MailAccounts.UserAccount, string> GetAccount() => new(account, UserRoot);

        public override string ToString() =>
            $"Email:{Email}, Login:{Login}, Pass:{!string.IsNullOrWhiteSpace(Pass)}, Root:{UserRoot}";
    }

    internal class CredentialsData
    {
        public UserAccount UserAccount { get; }
        public CredentialsRoute MessageRoute { get; }
        private readonly bool [] authorize = new bool[2];

        public string Domain { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To {
            get => MessageRoute.To;
            set => MessageRoute.To = value;
        }
        public string OriginalTo
        {
            get => MessageRoute.To;
            set {
                if (!string.IsNullOrWhiteSpace(value) &&
                    (string.IsNullOrWhiteSpace(MessageRoute.To) ||
                    !MessageRoute.To.Equals(value)))
                    MessageRoute.To = value;
            }
        }

        public bool IsLogin => authorize[0];
        public bool IsPassword => authorize[1];
        public bool IsAuthorize => IsLogin && IsPassword && !MessageRoute.IsDeliveryLocal;
        public bool IsEmpty => string.IsNullOrEmpty(From) || string.IsNullOrEmpty(To);

        public CredentialsData() {
            UserAccount = new();
            MessageRoute = new(UserAccount.GetAccount);
        }

        public bool CheckFrom(string from) {
            if (string.IsNullOrEmpty(from))
                return false;

            From = from.Trim();
            if (UserAccount.IsEmpty || !IsAuthorize)
                return true;
            return UserAccount.Email.Equals(From);
        }

        public bool CheckTo(string to, string oto = default) {
            if (string.IsNullOrEmpty(to) && string.IsNullOrEmpty(oto))
                return false;
            return MessageRoute.CheckToLocalDelivery(to, oto);
        }

        public void ResetDelivery() {
            MessageRoute.ResetDelivery();
            From = To = string.Empty;
        }

        #region Credentials (auth)

        public void ClearCredentials() =>
            authorize[0] = authorize[1] = false;

        public string CRAMMD5Challenge { get; private set; } = string.Empty;
        public string CRAMMD5Credentials()
        {
            CRAMMD5Challenge = $"<{Guid.NewGuid().ToString().Replace("-", "")}@local>".EncodeMD5B64();
            return CRAMMD5Challenge;
        }
        public bool CRAMMD5Credentials(string b64)
        {
            try {
                do {
                    if (string.IsNullOrEmpty(b64))
                        break;
                    byte[] bytes = Convert.FromBase64String(b64);
                    if ((bytes == null) || (bytes.Length == 0))
                        break;
                    string[] ss = Encoding.UTF8.GetString(bytes).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if ((ss == null) || (ss.Length < 2))
                        break;

                    FindAccount(ss[0].Trim());
                    if (UserAccount.IsEmpty)
                        break;

                    string hash = CRAMMD5Challenge.HashHMACMD5(UserAccount.Pass).ToLower();
                    if (string.IsNullOrWhiteSpace(hash))
                        break;
                    if (!hash.Equals(ss[1].Trim()))
                        break;

                    if (AddCredentials(CredentialsCheckId.Login))
                        return AddCredentials(CredentialsCheckId.Password);

                } while (false);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(CRAMMD5Credentials), ex); }
            return false;
        }

        public bool LoginCredentials(CredentialsCheckId type, string user)
        {
            try {
                do {
                    if (string.IsNullOrEmpty(user))
                        break;

                    if (type == CredentialsCheckId.Login) {
                        ClearCredentials();
                        FindAccount(user);
                    }
                    if (UserAccount.IsEmpty)
                        break;

                    switch (type) {
                        case CredentialsCheckId.Login: return UserAccount.LoginCompare(user) ? AddCredentials(CredentialsCheckId.Login) : false;
                        case CredentialsCheckId.Password: return UserAccount.PasswordCompare(user) ? AddCredentials(CredentialsCheckId.Password) : false;
                        default: break;
                    }
                } while (false);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(LoginCredentials), ex); }
            return false;
        }

        public bool LoginCredentialsB64(CredentialsCheckId type, string b64)
        {
            try {
                byte[] bytes = Convert.FromBase64String(b64);
                string s = UTF8Encoding.UTF8.GetString(bytes);
                return LoginCredentials(type, s);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(LoginCredentialsB64), ex); }
            return false;
        }

        public bool ApopCredentials(string sid, string pass)
        {
            try {
                do {
                    if (UserAccount.IsEmpty)
                        break;

                    if (ServerExtension.EncodeMD5B64($"<{sid}.local>{UserAccount.Pass}".ToLower()).Equals(pass.ToLower()))
                        return AddCredentials(CredentialsCheckId.Password);
                } while (false);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(LoginCredentialsB64), ex); }
            return false;
        }

        public bool PlainCredentials(string b64)
        {
            try {
                do {
                    if (string.IsNullOrWhiteSpace(b64))
                        break;

                    byte[] bytes = Convert.FromBase64String(b64);
                    if ((bytes == default) || (bytes.Length == 0))
                        break;

                    string s = Encoding.UTF8.GetString(bytes);
                    if (string.IsNullOrEmpty(s))
                        break;
                    string[] ss = s.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                    if ((ss == default) || (ss.Length < 2))
                        break;

                    FindAccount(ss[0].Trim());
                    if (UserAccount.IsEmpty)
                        break;
                    if (!AddCredentials(CredentialsCheckId.Login))
                        break;
                    if (!UserAccount.PasswordCompare(ss[1].Trim()))
                        break;
                    if (!AddCredentials(CredentialsCheckId.Password))
                        break;

                    return true;
                } while (false);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(LoginCredentialsB64), ex); }
            return false;
        }

        public bool AddCredentials(CredentialsCheckId type)
        {
            int i = (type == CredentialsCheckId.Login) ? 0 : 1;
            authorize[i] = !authorize[i];
            return authorize[i];
        }
        #endregion

        private void FindAccount(string login)
        {
            MailAccounts.UserAccount account = Global.Instance.FindAccount(login);
            if (account != null)
                UserAccount.SetAccount(account);
        }

        public override string ToString() =>
            $"Domain:{Domain}, From:{From}, To:{To}, Sender:{Sender}, Auth:{IsAuthorize}";
    }
}

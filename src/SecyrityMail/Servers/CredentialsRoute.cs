/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MimeKit;
using SecyrityMail.Data;
using SecyrityMail.Messages;

namespace SecyrityMail.Servers
{
    public enum MessageStoreReturn : int
    {
        MessageNull,
        MessageDelivered,
        MessageErrorDelivery
    }

    public class CredentialsRoute
    {
        private bool isDeliveryLocal_ = false;
        private int toIndex_ = 0;
        private Func<Tuple<MailAccounts.UserAccount, string>> GetAccount = () => default;
        private MessagesCacheOpener cacheOpener { get; set; } = default(MessagesCacheOpener);

        public List<Tuple<bool, Global.DirectoryPlace, Tuple<string, string>, string, bool>> MailOutList = new();
        public List<Tuple<string, string>> ToList { get; } = new();
        public bool IsDeliveryLocal => isDeliveryLocal_;
        public string To {
            get => ToList.Count > 0 ? ToList[toIndex_].Item2 : string.Empty;
            set => ToList.Add(new("", value));
        }

        public CredentialsRoute(Func<Tuple<MailAccounts.UserAccount, string>> func) {
            GetAccount = func;
            cacheOpener = CacheOpener.Build(this.GetType());
        }
        public CredentialsRoute(MailAccounts.UserAccount acc) {
            GetAccount = () =>
                new Tuple<MailAccounts.UserAccount, string>(acc, Global.GetUserDirectory(acc.Email));
            cacheOpener = CacheOpener.Build(this.GetType());
        }

        #region Check message delivery to local address
        public bool CheckToLocalDelivery(string to, string oto)
        {
            to = to.Trim();
            oto = (oto != default) ? oto.Trim() : string.Empty;
            bool isDelivery = false;
            MailAccounts.UserAccount account;

            if (!string.IsNullOrWhiteSpace(to)) {
                account = Global.Instance.FindFromEmail(to);
                if ((account != null) && !account.IsEmptyCredentials) {
                    SetLocalDelivery(to);
                    MailOutList.Add(
                        new(true, Global.DirectoryPlace.Msg, new(account.Name, account.Email), Global.GetUserDirectory(account.Email), account.IsPgpAutoDecrypt));
                    isDelivery = true;
                }
            }
            if (!string.IsNullOrWhiteSpace(oto)) {
                account = Global.Instance.FindFromEmail(oto);
                if ((account != null) && !account.IsEmptyCredentials) {
                    SetLocalDelivery(oto);
                    MailOutList.Add(
                        new(true, Global.DirectoryPlace.Msg, new(account.Name, account.Email), Global.GetUserDirectory(account.Email), account.IsPgpAutoDecrypt));
                    isDelivery = true;
                }
            }
            return isDelivery ? isDelivery : isDeliveryLocal_;
        }
        #endregion

        #region Check message delivery
        public async Task<bool> CheckDelivery(MimeMessage mmsg) =>
            await Task.Run(() => {

                if (mmsg == null)
                    return false;

                if (ToList.Count > 0)
                    AddDeliveryList(ToList);

                try {
                    List<Tuple<string, string>> lists = new();

                    if (mmsg.To != null) {
                        var list = ParseAddress(mmsg.To);
                        if (list.Count > 0)
                            lists.AddRange(list);
                    }
                    if (mmsg.Cc != null) {
                        var list = ParseAddress(mmsg.Cc);
                        if (list.Count > 0)
                            lists.AddRange(list);
                    }
                    if (mmsg.Bcc != null) {
                        var list = ParseAddress(mmsg.Bcc);
                        if (list.Count > 0)
                            lists.AddRange(list);
                    }
                    if (lists.Count > 0)
                        AddDeliveryList(lists.Distinct().ToList());

                    mmsg.To.Clear();
                    mmsg.Cc.Clear();
                    mmsg.Bcc.Clear();
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(CheckDelivery), ex); }
                return false;
            });
        #endregion

        #region Set/Reset message delivery
        public void SetLocalDelivery() => isDeliveryLocal_ = true;
        public void SetLocalDelivery(string to) { ToSelector(to); isDeliveryLocal_ = true; }
        public void ResetDelivery() {
            ToList.Clear();
            MailOutList.Clear();
            toIndex_ = 0;
            isDeliveryLocal_ = false;
        }
        #endregion

        #region Message delivery Store
        public async Task<MessageStoreReturn> MessageStore(MimeMessage mmsg, Action<MailEventId, string, object> act) =>
            await Task.Run(async () => {
                try {
                    if (mmsg == default)
                        return MessageStoreReturn.MessageNull;

                    _ = await CheckDelivery(mmsg).ConfigureAwait(false);

                    try {
                        foreach (var t in MailOutList) {
                            MailMessage msg = default;
                            mmsg.To.Clear();
                            mmsg.To.Add(new MailboxAddress(t.Item3.Item1, t.Item3.Item2));

                            if (t.Item1 || (t.Item2 == Global.DirectoryPlace.Error))
                                msg = await LocalDelivery(t.Item2, t.Item4, t.Item3.Item2, mmsg, t.Item5)
                                            .ConfigureAwait(false);
                            else
                                msg = await OutDelivery(t.Item2, t.Item4, mmsg)
                                            .ConfigureAwait(false);

                            if (msg != null)
                                act.Invoke(
                                    (t.Item2 == Global.DirectoryPlace.Error) ? MailEventId.DeliveryErrorMessage :
                                        (t.Item1 ? MailEventId.DeliveryLocalMessage : MailEventId.DeliveryOutMessage),
                                    $"{t.Item3.Item1} {t.Item3.Item2}", msg);
                        }
                        return MessageStoreReturn.MessageDelivered;
                    }
                    catch (Exception ex) {
                        Global.Instance.Log.Add(nameof(MessageStore), ex);
                    }
                    finally {
                        if (mmsg != default) try { mmsg.Dispose(); } catch { }
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(MessageStore), ex); }
                return MessageStoreReturn.MessageErrorDelivery;
            });
        #endregion

        #region private
        private async Task<MailMessage> LocalDelivery(
            Global.DirectoryPlace place, string rootpath, string email, MimeMessage mmsg, bool autodecrypt) {
            try {
                MailMessages msgs = await cacheOpener.Open(email)
                                                     .ConfigureAwait(false);
                if (msgs == null)
                    return default(MailMessage);

                await Global.Instance.EmailAddresses.AddRangeAsync(mmsg)
                                                    .ConfigureAwait(false);

                MailMessage msg = await new MailMessage().CreateAndDelivery(
                    place, mmsg, rootpath, msgs.Count + 1, autodecrypt, Global.Instance.Config.IsModifyMessageDeliveredLocal)
                                                         .ConfigureAwait(false);
                if (msg == null)
                    return default(MailMessage);

                msgs.Add(msg);
                return msg;
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(LocalDelivery), ex); }
            finally { await cacheOpener.Close(email).ConfigureAwait(false); }
            return default(MailMessage);
        }

        private async Task<MailMessage> OutDelivery(
            Global.DirectoryPlace place, string rootpath, MimeMessage mmsg) {
            try {
                int i = Directory.GetFiles(Global.AppendPartDirectory(rootpath, place)).Length;
                await Global.Instance.EmailAddresses.AddRangeAsync(mmsg)
                                                    .ConfigureAwait(false);

                return await new MailMessage().CreateAndDelivery(place, mmsg, rootpath, i)
                                              .ConfigureAwait(false);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(OutDelivery), ex); }
            return default(MailMessage);
        }

        private void ToSelector(string to)
        {
            to = to.Trim();
            var item = ContainsToList(to);
            if (item != null) {
                toIndex_ = ToList.IndexOf(item);
                toIndex_ = (toIndex_ == -1) ? 0 : toIndex_;
            }
            else {
                toIndex_ = ToList.Count;
                ToList.Add(new("", to));
            }
        }

        private void AddDeliveryList(List<Tuple<string, string>> list)
        {
            if (list.Count == 0)
                return;

            Tuple<MailAccounts.UserAccount, string> t = GetAccount.Invoke();
            bool isUserAccount = (t != default) && (t.Item1 != default) && !t.Item1.IsEmptyCredentials;
            string outpath = isUserAccount ? Global.GetUserDirectory(t.Item1.Email) : string.Empty;

            foreach (var a in list) {
                try {
                    if (ContainsDeliveryList(a.Item2) || !a.Item2.Contains('@'))
                        continue;

                    Tuple<string, string> x = new Tuple<string, string>(a.Item1, a.Item2);
                    MailAccounts.UserAccount account = Global.Instance.FindFromEmail(x.Item2);

                    if ((account != null) && string.IsNullOrWhiteSpace(x.Item1) && !string.IsNullOrWhiteSpace(account.Name))
                        x = new Tuple<string, string>(account.Name, account.Email);

                    if (account == null) {

                        if (isUserAccount && !t.Item1.IsEmptySend)
                            MailOutList.Add(
                                new(false, Global.DirectoryPlace.Out, x, outpath, false));
                        else if (isUserAccount)
                            MailOutList.Add(
                                new(false, Global.DirectoryPlace.Error, x, outpath, false));
                        else
                            Global.Instance.Log.Add(
                                nameof(AddDeliveryList), $"Skip send to: '{x.Item1}' {x.Item2}, not owner found");
                    }
                    else if (!account.IsEmptyCredentials)
                        MailOutList.Add(
                            new(true, Global.DirectoryPlace.Msg, x, Global.GetUserDirectory(account.Email), account.IsPgpAutoDecrypt));
                    else
                        Global.Instance.Log.Add(
                            nameof(AddDeliveryList), $"Wrong account, not delivery to: '{x.Item1}' {x.Item2}, incomplette credentials");
                } catch { }
                MailOutList = MailOutList.Distinct().ToList();
            }
        }

        private bool ContainsDeliveryList(string a) =>
            (from i in MailOutList where i.Item3.Item2.Equals(a) select i).FirstOrDefault() != default;

        private Tuple<string, string> ContainsToList(string a) =>
            (from i in ToList where i.Item2.Equals(a) select i).FirstOrDefault();

        private List<Tuple<string, string>> ParseAddress(InternetAddressList ialist)
        {
            if (ialist == null)
                return new();
            return (from i in ialist.Mailboxes
                    select new Tuple<string, string>(i.Name, i.Address)).ToList();
        }
        #endregion
    }
}

/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecyrityMail.Messages
{
    internal class MessagesCache : IDisposable
    {
        private readonly object __lock = new object();
        private List<CacheOpenerData> UsingOpener { get; } = new();
        private MailMessages Messages { get; set; } = default(MailMessages);

        public const string Tag = "Cache Messages";
        public bool IsEmpty => (Messages == default) || string.IsNullOrWhiteSpace(Email);
        public bool CanClose => UsingOpener.Count == 0;
        public long Count => UsingOpener.Count;
        public string UsingClass => string.Join(",", UsingOpener.Select(t => t.TypeName));
        public string Email { get; set; } = default;

        public int  MessagesCount => (Messages == default) ? 0 : Messages.Count;
        public bool MessagesIsBusy => (Messages == default) ? false : Messages.IsBusy;
        public bool MessagesIsModify => (Messages == default) ? false : Messages.IsModify;

        public MessagesCache() { }

        public async Task<MailMessages> ReOpen(CacheOpenerData u, string email) =>
            await Task.Run(async () => {
                System.Diagnostics.Debug.WriteLine($"\tCache ReOpen: {email} -> {Email}/{Count}");
                if (CheckUsing(u)) {
                    if ((Count > 1) && (Messages != default)) {
                        Global.Instance.Log.Add(
                            Tag, $"can't reopen message cache, data is in use by another instance: {Email}/{Count}/{CanClose}");
                        return Messages;
                    }
                    if (!CheckEmail(email))
                        return default;
                    if (!IsEmpty && Messages.MailMessagesExist())
                        Messages.MailMessagesDelete();
                    Messages = default;
                }
                return await Open(u, email);
            });

        public async Task<MailMessages> Open(CacheOpenerData u, string email) =>
            await Task.Run(async () => {
                if (!CheckUsing(u)) {
                    lock(__lock)
                        UsingOpener.Add(u);
                    if (string.IsNullOrWhiteSpace(Email))
                        Email = email;
                }
                System.Diagnostics.Debug.WriteLine($"\tCache Open: {email}/{Count}");
                if (!CheckEmail(email))
                    return default;
                if (Messages == default)
                    Messages = await new MailMessages().Open(email).ConfigureAwait(false);
                return Messages;
            });

        public bool Close(CacheOpenerData u) {
            try {
                if (!CheckUsing(u))
                    return false;
                lock (__lock)
                    UsingOpener.Remove(u);
                System.Diagnostics.Debug.WriteLine($"\tCache Close: {Email}/{Count}/{CanClose} - {u.Id}/{u.Type}");
                Global.Instance.Log.Add(
                    Tag, $"close messages cache from {Email}/{Count}/{CanClose} - {u.Type.Name}");
                return CanClose;
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(MessagesCache), ex); }
            return false;
        }

        public async void Dispose() {
            MailMessages m = Messages;
            Messages = default;
            if ((m != default) && (m.IsModify || !m.MailMessagesExist()))
                await m.Save().ConfigureAwait(false);
        }

        private CacheOpenerData Find(CacheOpenerData u) =>
            (from i in UsingOpener where i == u select i).FirstOrDefault();
        private bool CheckUsing(CacheOpenerData u) {
            lock(__lock)
                return Find(u) != default;
        }
        private bool CheckEmail(string email) {
            if (string.IsNullOrWhiteSpace(Email) || !Email.Equals(email)) {
                Global.Instance.Log.Add(
                    Tag, $"bad cache data, email id not equals: {email} -> {Email}/{Count}/{CanClose}");
                return false;
            }
            return true;
        }

    }
}

/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SecyrityMail.Messages
{
    public class MailMessagesManager : IDisposable
    {
        private readonly object __lock = new object();
        private volatile Timer timer;
        private List<MailMessagesCache> MessagesCache { get; } = new();
        private bool isCacheMessagesLog = false;
        public bool IsCacheMessagesLog {
            get => isCacheMessagesLog;
            set {
                if (isCacheMessagesLog != value)
                    isCacheMessagesLog = value;
            }
        }
        public const string Tag = "Messages Manager";

        public MailMessagesManager() =>
            timer = new(TimerCb, default, TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(60.0));
        ~MailMessagesManager() => Dispose();

        private void TimerCb(object _) {
            if (MessagesCache.Count == 0) return;
            try {
                for (int i = MessagesCache.Count - 1; 0 <= i; i--) {
                    MailMessagesCache mscs;
                    lock (__lock)
                        mscs = MessagesCache[i];
                    if (mscs == null) continue;

                    if (IsCacheMessagesLog) {
                        Global.Instance.Log.Add(
                            MailMessagesCache.Tag, $"   using: {mscs.UsingClass}");
                        Global.Instance.Log.Add(
                            MailMessagesCache.Tag, "in cache: " +
                            $"{mscs.Email}/{mscs.Count}/{mscs.CanClose} = " +
                            $"{mscs.MessagesCount}/{mscs.MessagesIsModify}/{mscs.MessagesIsBusy}");
                    }
                    if (mscs.CanClose) {
                        lock (__lock) {
                            MessagesCache.Remove(mscs);
                            mscs.Dispose();
                        }
                    }
                }
            } catch (Exception ex) { Global.Instance.Log.Add(Tag, ex); }
        }

        public void Dispose() {
            try {
                lock (__lock) {
                    foreach (var cache in MessagesCache)
                        cache.Dispose();
                    MessagesCache.Clear();
                }
            } catch { }

            Timer t = timer;
            timer = default;
            if (t != null)
                t.Dispose();
        }

        public  async Task<MailMessages> Open(CacheOpenerData u, string email) => await OpenCache(u, email, false);
        public  async Task<MailMessages> ReOpen(CacheOpenerData u, string email) => await OpenCache(u, email, true);
        private async Task<MailMessages> OpenCache(CacheOpenerData u, string email, bool isreload) {
            MailMessagesCache mscs;
            lock (__lock)
                mscs = Find(email);
            if ((mscs == default) || mscs.IsEmpty) { isreload = false; mscs = new(); }
            MailMessages msgs = isreload ?
                await mscs.ReOpen(u, email).ConfigureAwait(false) :
                await mscs.Open(u, email).ConfigureAwait(false);
            if ((msgs == null) || mscs.IsEmpty)
                return default;
            lock (__lock)
                if (!MessagesCache.Contains(mscs))
                    MessagesCache.Add(mscs);
            return msgs;
        }

        public void Close(CacheOpenerData u, string email) {
            MailMessagesCache mscs;
            lock (__lock)
                mscs = Find(email);
            if (mscs != default) {
                if (mscs.Close(u)) {
                    lock (__lock) {
                        MessagesCache.Remove(mscs);
                        mscs.Dispose();
                    }
                }
            }
        }

        public void Close(CacheOpenerData u) {
            lock (__lock) {
                for (int i = MessagesCache.Count - 1; 0 <= i; i--) {
                    MailMessagesCache mscs = MessagesCache[i];
                    if (mscs == default) continue;
                    if (mscs.Close(u)) {
                        MessagesCache.Remove(mscs);
                        mscs.Dispose();
                    }
                }
            }
        }

        private MailMessagesCache Find(string email) =>
            (from i in MessagesCache where i.Email.Equals(email) select i).FirstOrDefault();
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SecyrityMail.Messages
{
    public class MailMessagesManager : IDisposable
    {
        private volatile Timer timer;
        private List<MailMessagesCache> MessagesCache { get; } = new();
        private bool isCacheMessagesLog = false;
        public bool IsCacheMessagesLog {
            get => isCacheMessagesLog;
            set {
                isCacheMessagesLog = value;
                if (value)
                    timer.Change(TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(60.0));
                else
                    timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }

        public MailMessagesManager() =>
            timer = new(TimerCb, default, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        ~MailMessagesManager() => Dispose();

        private void TimerCb(object _) {
            foreach(var msg in MessagesCache)
                Global.Instance.Log.Add(
                    "MessagesManager", "in cache: " +
                    $"{msg.Email}/{msg.Count}/{msg.CanClosed} = " +
                    $"{msg.MessagesCount}/{msg.MessagesIsModify}/{msg.MessagesIsBusy}");
        }

        public void Dispose() {
            try {
                foreach (var cache in MessagesCache)
                    cache.Dispose();
                MessagesCache.Clear();
            } catch { }

            Timer t = timer;
            timer = default;
            if (t != null)
                t.Dispose();
        }

        public async Task<MailMessages> Open(string email) {
            MailMessagesCache mscs = Find(email);
            if (mscs != default)
                return await mscs.Open(email).ConfigureAwait(false);

            mscs = new MailMessagesCache();
            MailMessages msgs = await mscs.Open(email).ConfigureAwait(false);
            if ((msgs == null) || mscs.IsEmpty)
                return default;
            MessagesCache.Add(mscs);
            return msgs;
        }

        public void Close(string email) {
            MailMessagesCache mscs = Find(email);
            if (mscs != default) {
                if (mscs.Close()) {
                    MessagesCache.Remove(mscs);
                    mscs.Dispose();
                }
            }
        }

        private MailMessagesCache Find(string email) =>
            (from i in MessagesCache where i.Email.Equals(email) select i).FirstOrDefault();
    }
}

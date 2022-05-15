
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SecyrityMail.Messages
{
    internal class MailMessagesCache : IDisposable
    {
        private long count_ = 0;
        private MailMessages messages { get; set; } = default(MailMessages);
        private long Count_ {
            get => Interlocked.Read(ref count_);
            set { if (value > 0) Interlocked.Increment(ref count_); else Interlocked.Decrement(ref count_); }
        }

        public bool IsEmpty => messages == default;
        public bool CanClosed => Count_ == 0L;
        public string Email { get; set; } = default;
        public long Count => Count_;

        public int MessagesCount => (messages == default) ? 0 : messages.Count;
        public bool MessagesIsBusy => (messages == default) ? false : messages.IsBusy;
        public bool MessagesIsModify => (messages == default) ? false : messages.IsModify;

        public MailMessagesCache() { }

        public async Task<MailMessages> Open(string email) {
            Count_ = 1;
            System.Diagnostics.Debug.WriteLine($"\tChache Open: {email}/{Count}");
            if (string.IsNullOrWhiteSpace(Email))
                Email = email;
            if (messages == default)
                messages = await new MailMessages().Open(email).ConfigureAwait(false);
            return messages;
        }
        public bool Close() {
            Count_ = 0;
            System.Diagnostics.Debug.WriteLine($"\tChache Close: {Email}/{Count}");
            Global.Instance.Log.Add(nameof(MailMessagesCache), $"close messages chache from {Email}/{Count}/{CanClosed}");
            return CanClosed;
        }

        public async void Dispose() {
            MailMessages m = messages;
            messages = default;
            if ((m != default) && m.IsModify)
                await m.Save().ConfigureAwait(false);
        }
    }
}

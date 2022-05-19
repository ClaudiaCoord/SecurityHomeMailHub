/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecyrityMail.Messages
{
    public class CacheOpenerData : IComparer<CacheOpenerData> {
        public Guid Id { get; set; } = default;
        public Type Type { get; set; } = Type.Missing.GetType();
        public string TypeName => Type.Name;
        public CacheOpenerData(Type type) {
            Id = Guid.NewGuid();
            Type = type;
        }
        public bool Equals(CacheOpenerData y) => (Id != default) && Id.Equals(y.Id) && Type.Equals(y.Type);
        public int Compare(CacheOpenerData x, CacheOpenerData y) =>
            x.Id.CompareTo(y.Id);
    }

    public class MessagesCacheOpener : IDisposable
    {
        private readonly CacheOpenerData cache;

        public MessagesCacheOpener(Type t) => cache = new(t);
        ~MessagesCacheOpener() => Dispose();

        public void Dispose() =>
            Global.Instance.MessagesManager.Close(cache);

        public async Task<MailMessages> Open(string userId) =>
            await Global.Instance.MessagesManager.Open(cache, userId)
                                                 .ConfigureAwait(false);

        public async Task<MailMessages> ReOpen(string userId) =>
            await Global.Instance.MessagesManager.ReOpen(cache, userId)
                                                 .ConfigureAwait(false);

        public async Task<bool> Close(string userId = default) =>
            await Task.Run(() => {
                if (userId == default)
                    Global.Instance.MessagesManager.Close(cache);
                else
                    Global.Instance.MessagesManager.Close(cache, userId);
                return true;
            });
    }

    public static class CacheOpener
    {
        public static MessagesCacheOpener Build(Type t) =>
            new MessagesCacheOpener(t);
    }
}

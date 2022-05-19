/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using SecyrityMail.Data;

namespace SecyrityMail
{
    public class MailLog : MailEvent
    {
        private ConcurrentQueue<Tuple<DateTime, string, string>> queue_ = new();
        public int Count() => queue_.Count;
        public void Add(string s) => AddQueue("MSG", s);
        public void Add(Exception ex) => AddQueue(ex.GetType().Name, ex.Message);
        public void Add(string tag, string s) => AddQueue(tag, s);
        public void Add(string tag, Exception ex) => AddQueue(tag, ex.Message);
        public void AddFullException(string tag, Exception ex) => AddQueue(tag, ex.ToString());
        public void AddFullException(Exception ex) => AddQueue(ex.GetType().Name, ex.ToString());
        public Tuple<DateTime, string, string> Get() { queue_.TryDequeue(out Tuple<DateTime, string, string> t); return t; }

        public void ForeachLog(Action<Tuple<DateTime, string, string>> act)
        {
            while (Count() > 0)
                act.Invoke(Get());
        }
        private void AddQueue(string tag, string s)
        {
            queue_.Enqueue(Tuple.Create(DateTime.Now, tag, s));
            OnPropertyChanged(nameof(MailLog));
        }

        public async Task DeleteEmptyLog()
        {
            await Task.Run(() => {
                try {
                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    DirectoryInfo dir = new(Global.GetUserDirectory(Global.DirectoryPlace.Log, now));
                    if ((dir == default) || !dir.Exists)
                        return;
                    foreach (FileInfo fi in dir.EnumerateFiles("*.log", SearchOption.TopDirectoryOnly))
                        if ((fi != default) && fi.Exists && (fi.Length == 0L)) fi.Delete();
                }
                catch (Exception ex) { AddQueue(nameof(DeleteEmptyLog), ex.Message); }
            });
        }
    }
}


using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SecyrityMail.Servers.POP3
{
    internal class Pop3Data
    {
        private readonly object __lock = new object();

        public DirectoryInfo Dir { get; private set; }
        public string Counts => Items.Count.ToString();
        public int  Count => Items.Count;
        public long Size { get; set; } = 0L;
        public List<Tuple<int, FileInfo>> Items { get; } = new();
        public List<Tuple<int, FileInfo>> ItemsDelete { get; } = new();

        public Pop3Data(DirectoryInfo d) => Dir = d;
        public void Add(int i, string s) { lock (__lock) Items.Add(new(i, new FileInfo(s))); }
        public void Add(int i, FileInfo f) { lock (__lock) Items.Add(new (i,f)); }
        public void Clear() { lock (__lock) Items.Clear(); }
        public Tuple<int, FileInfo> Get(int i) {
            if (i < 0) return default;
            Tuple<int, FileInfo> t;
            lock (__lock)
                t = (from x in Items
                     where x.Item1 == i
                     select x).FirstOrDefault();
            if (t == null) return default;
            t.Item2.Refresh();
            return t;
        }
        public void AddToDelete(int i) {
            Tuple<int, FileInfo> t = Get(i);
            if (t == null) return;
            t.Item2.Refresh();
            Size -= t.Item2.Length;
            lock (__lock) {
                Items.Remove(t);
                ItemsDelete.Add(t);
            }
        }
        public void UnDelete()
        {
            if (ItemsDelete.Count == 0) return;
            lock (__lock) {
                Items.AddRange(ItemsDelete);
                ItemsDelete.Clear();
            }
        }
        public void UnDelete(int i)
        {
            if (i < 0) return;
            Tuple<int, FileInfo> t;
            lock (__lock)
                t = (from x in ItemsDelete
                     where x.Item1 == i
                     select x).FirstOrDefault();
            if (t == null) return;
            lock (__lock) {
                if (!Items.Contains(t))
                    Items.Add(t);
                ItemsDelete.Remove(t);
            }
        }
        public void AllToDelete() {
            if (Items.Count == 0) return;
            lock (__lock) {
                ItemsDelete.AddRange(Items);
                Items.Clear();
            }
            Size = 0L;
        }
        public async Task Build()
        {
            await Task.Run(() => {
                try {
                    Size = 0L;
                    Clear();

                    if ((Dir == null) || !Dir.Exists) return;

                    FileInfo[] list = Dir.GetFiles("*.eml", SearchOption.TopDirectoryOnly);
                    if ((list == null) || (list.Length == 0))
                        return;

                    for(int i = 0; i < list.Length; i++) {
                        FileInfo f = list[i];
                        if (!f.Exists)
                            continue;
                        Size += f.Length;
                        Add(i + 1, f);
                    }
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(Build), ex); }
            });
        }

        public async Task Delete()
        {
            if (ItemsDelete.Count == 0) return;

            await Task.Run(() => {
                try {
                    lock (__lock) {
                        foreach (Tuple<int, FileInfo> t in ItemsDelete) {
                            FileInfo f = t.Item2;
                            f.Refresh();
                            if (!f.Exists || f.IsReadOnly)
                                continue;
                            f.Delete();
                        }
                        ItemsDelete.Clear();
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Delete), ex); }
            });
        }
    }
}

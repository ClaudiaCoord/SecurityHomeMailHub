using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SecyrityMail.Data;
using SecyrityMail.Utils;

namespace SecyrityMail.Messages
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class MailMessages : MailEvent
    {
        private static readonly Global.DirectoryPlace[] places_ = new Global.DirectoryPlace[] {
                        Global.DirectoryPlace.Msg, Global.DirectoryPlace.Bounced, Global.DirectoryPlace.Error
                    };
        private readonly object __lock = new object();
        private bool isModify = false;
        private long isBusyLong_ = 0L;
        private bool IsBusy_ {
            get => (Interlocked.Read(ref isBusyLong_) == 0L) ? false : true;
            set =>  Interlocked.Exchange(ref isBusyLong_, value ? 1L : 0L);
        }

        public MailMessages() { }
        public MailMessages(string dir) {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            RootDirectory = dir;
        }

        [XmlIgnore]
        public List<MailMessage> ItemsDeleted { get; set; } = new();

        [XmlIgnore]
        public long TotalSize {
            get => TotalSize_;
            set { TotalSize_ = (value >= 0) ? value : 0; OnPropertyChanged(nameof(TotalSize)); }
        }
        private long TotalSize_ = 0U;
        [XmlIgnore]
        public string TotalSizes => TotalSize.ToString();
        [XmlIgnore]
        public int Count => Items.Count;
        [XmlIgnore]
        public string Counts => Items.Count.ToString();
        [XmlIgnore]
        public bool IsBusy => IsBusy_;
        [XmlIgnore]
        public bool IsModify => (Items.Count > 0) && isModify;
        [XmlIgnore]
        public MailMessage this[int i] { get => Items[i]; set => Items[i] = value; }

        [XmlElement("directory")]
        public string RootDirectory { get; set; } = string.Empty;
        [XmlElement("items")]
        public List<MailMessage> Items { get; set; } = new();
        public void OnChange() { OnPropertyChanged(nameof(MailMessages)); }
        public void Clear() {
            lock(__lock)
                Items.Clear();
            TotalSize = 0U;
            isModify = true;
            OnPropertyChanged(nameof(MailMessages));
        }
        public void Delete(MailMessage msg) {
            lock (__lock) {
                Items.Remove(msg);
                ItemsDeleted.Add(msg);
            }
            isModify = true;
            TotalSize -= (long)msg.Size;
            OnPropertyChanged(nameof(MailMessages));
        }
        public void Add(MailMessage msg) {
            lock (__lock)
                Items.Add(msg);
            isModify = true;
            TotalSize += (long)msg.Size;
            OnPropertyChanged(nameof(MailMessages));
        }
        public MailMessages AddOnce(MailMessage msg) {
            lock (__lock)
                Items.Add(msg);
            isModify = true;
            TotalSize += (long)msg.Size;
            OnPropertyChanged(nameof(MailMessages));
            return this;
        }
        public MailMessage Get(int i) {
            lock (__lock)
                return (from x in Items where x.Id == i select x).FirstOrDefault();
        }
        public bool Copy(MailMessages msgs) {
            if ((msgs == null) || (msgs.Items == null) || (msgs.Items.Count == 0))
                return false;

            if (!string.IsNullOrEmpty(RootDirectory)) {
                if (!string.IsNullOrEmpty(msgs.RootDirectory) &&
                    !RootDirectory.Equals(msgs.RootDirectory))
                    throw new Exception("Root directory owner not equals");
            }
            else
                RootDirectory = msgs.RootDirectory;

            Clear();
            foreach (MailMessage m in msgs.Items)
                lock (__lock) Add(m);

            isModify = true;
            return Items.Count > 0;
        }
        public MailMessage Find(int id) {
            if ((id <= 0) || (Items.Count == 0)) return default;
            MailMessage msg;
            lock (__lock)
                msg = (from i in Items where i.Id == id select i).FirstOrDefault();
            return msg;
        }
        public MailMessage Find(string msgid) {
            if (string.IsNullOrWhiteSpace(msgid) || (Items.Count == 0)) return default;
            MailMessage msg;
            lock (__lock)
                msg = (from i in Items where i.MsgId.Equals(msgid) select i).FirstOrDefault();
            return msg;
        }

        #region Load* / Save*
        public async Task<MailMessages> Open(string email) {
            RootDirectory = Global.GetUserDirectory(email);
            _ = await Load();
            if (Items.Count == 0)
                _ = await Scan().ConfigureAwait(false);
            return this;
        }
        public async Task<bool> Load(string path = default)
        {
            if (IsBusy_)
                return false;
            IsBusy_ = true;
            return await Task.Run(() => {
                try {
                    if (string.IsNullOrWhiteSpace(RootDirectory))
                        return false;
                    if (string.IsNullOrWhiteSpace(path))
                        path = Path.Combine(
                            RootDirectory, $"{nameof(MailMessages)}.cache");
                    if (!File.Exists(path))
                        return false;
                    return Copy(path.DeserializeFromFile<MailMessages>());
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Load), ex); }
                finally { IsBusy_ = false; }
                return false;
            });
        }
        public async Task<bool> Save(string path = default)
        {
            if (IsBusy_)
                return false;
            IsBusy_ = true;
            return await Task.Run(() => {
                try {
                    if (string.IsNullOrWhiteSpace(path)) {
                        if (string.IsNullOrWhiteSpace(RootDirectory))
                            return false;
                        if (!Directory.Exists(RootDirectory))
                            if (Directory.CreateDirectory(RootDirectory) == default)
                                return false;

                        for (int i = 0; i < Items.Count; i++)
                            Items[i].Id = i + 1;

                        Path.Combine(
                                RootDirectory, $"{nameof(MailMessages)}.cache")
                        .SerializeToFile(this);
                    }
                    else
                        path.SerializeToFile(this);
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Save), ex); }
                finally { IsBusy_ = false; }
                return false;
            });
        }
        #endregion

        #region Sort*
        public void Sort() {
            if (Count > 0)
                lock (__lock)
                    Items = Items.OrderBy(o => o.Id).ToList();
        }
        #endregion

        #region Scan*
        public async Task<bool> Scan()
        {
            if (IsBusy_)
                return false;
            IsBusy_ = true;
            return await Task.Run(async () => {
                try
                {
                    if (string.IsNullOrWhiteSpace(RootDirectory))
                        return false;
                    if (!Directory.Exists(RootDirectory))
                        if (Directory.CreateDirectory(RootDirectory) == default)
                            return false;

                    Clear();
                    List<MailMessage> list = new();

                    foreach (Global.DirectoryPlace place in MailMessages.places_) {
                        try {
                            DirectoryInfo dir = new(Path.Combine(
                                RootDirectory, Global.GetPartDirectory(place)));
                            if ((dir == default) || !dir.Exists)
                                continue;

                            foreach (FileInfo fi in dir.EnumerateFiles("*.eml", SearchOption.AllDirectories)) {
                                if ((fi != default) && fi.Exists && (fi.Length > 0L)) {
                                    try {
                                        MailMessage msg = await new MailMessage().LoadAndCreate(fi, list.Count + 1)
                                                                                 .ConfigureAwait(false);
                                        if (msg != null) list.Add(msg);
                                    } catch (Exception ex) { Global.Instance.Log.Add(nameof(Scan), ex); }
                                }
                            }
                        } catch (Exception ex) { Global.Instance.Log.Add(nameof(Scan), ex); }
                    }
                    if (list.Count > 0)
                        foreach (MailMessage msg in list.OrderBy(o => o.Id)) Add(msg);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Scan), ex); }
                finally { IsBusy_ = false; }
                return Count > 0;
            });
        }
        #endregion

        #region New Message to send/msg/out*
        public async Task<bool> SpoolOutToOut(string path)
        {
            if (IsBusy_)
                return false;
            IsBusy_ = true;
            return await Task.Run(async () => {
                try {
                    MailMessage msg = await new MailMessage().CreateAndDelivery(Global.DirectoryPlace.Out, path, (Items.Count + 1))
                                                             .ConfigureAwait(false);
                    if (msg != null) {
                        Add(msg);
                        return true;
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(SafeDelete), ex); }
                finally { IsBusy_ = false; }
                return false;
            });
        }

        public async Task<bool> SpoolInToMsg(string path)
        {
            if (IsBusy_)
                return false;
            IsBusy_ = true;
            return await Task.Run(async () => {
                try {
                    MailMessage msg = await new MailMessage().CreateAndDelivery(Global.DirectoryPlace.Msg, path, (Items.Count + 1))
                                                             .ConfigureAwait(false);
                    if (msg != null) {
                        Add(msg);
                        return true;
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(SafeDelete), ex); }
                finally { IsBusy_ = false; }
                return false;
            });
        }
        #endregion

        #region Delete One Message*
        public async Task<bool> DeleteMessage(string msgid)
        {
            if (IsBusy_)
                return false;
            IsBusy_ = true;
            return await Task.Run(() => {
                try {
                    MailMessage msg = Find(msgid);
                    if (msg == null) return false;
                    FileDelete_(msg.FilePath);
                    Delete(msg);
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(SafeDelete), ex); }
                finally { IsBusy_ = false; }
                return false;
            });
        }

        public async Task<bool> DeleteMessage(int id)
        {
            if (IsBusy_)
                return false;
            IsBusy_ = true;
            return await Task.Run(() => {
                try {
                    MailMessage msg = Find(id);
                    if (msg == null) return false;
                    FileDelete_(msg.FilePath);
                    Delete(msg);
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(SafeDelete), ex); }
                finally { IsBusy_ = false; }
                return false;
            });
        }
        #endregion

        #region Safe Delete*
        public void UnDelete()
        {
            if (IsBusy_)
                return;
            IsBusy_ = true;
            try {
                if (ItemsDeleted.Count > 0) {
                    foreach (MailMessage m in ItemsDeleted) Add(m);
                    lock (__lock)
                        ItemsDeleted.Clear();
                }
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(UnDelete), ex); }
            finally { IsBusy_ = false; }
        }

        public async Task<bool> SafeDelete()
        {
            if (IsBusy_)
                return false;
            IsBusy_ = true;
            return await Task.Run(() => {
                try {
                    foreach (MailMessage m in ItemsDeleted) {
                        if (string.IsNullOrEmpty(m.FilePath))
                            continue;
                        FileDelete_(m.FilePath);
                    }
                    lock (__lock)
                        ItemsDeleted.Clear();
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(SafeDelete), ex); }
                finally { IsBusy_ = false; }
                return false;
            });
        }

        private void FileDelete_(string path) {
            try {
                FileInfo f = new(path);
                if ((f != default) && f.Exists && !f.IsReadOnly)
                    f.Delete();
            } catch (Exception ex) { Global.Instance.Log.Add(nameof(FileDelete_), ex); }
        }
        #endregion

    }
}
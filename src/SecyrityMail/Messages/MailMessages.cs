/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MimeKit;
using SecyrityMail.Data;
using SecyrityMail.Utils;

namespace SecyrityMail.Messages
{
    internal class DateComparer : IComparer<MailMessage> {
        public int Compare(MailMessage x, MailMessage y) =>
            DateTimeOffset.Compare(x.Date, y.Date);
    }

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
        private RunOnce runOnce = new();

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
        public bool IsBusy => runOnce.IsRunning;
        [XmlIgnore]
        public bool IsModify => isModify;
        [XmlIgnore]
        public MailMessage this[int i] { get => Items[i]; set => Items[i] = value; }

        [XmlElement("directory")]
        public string RootDirectory { get; set; } = string.Empty;
        [XmlElement("items")]
        public List<MailMessage> Items { get; set; } = new();
        public void OnChange() { isModify = true; OnPropertyChanged(nameof(OnChange)); }
        public void Clear() {
            lock(__lock)
                Items.Clear();
            TotalSize = 0U;
            isModify = true;
            OnPropertyChanged(nameof(Clear));
        }
        public void Delete(MailMessage msg) {
            lock (__lock) {
                Items.Remove(msg);
                ItemsDeleted.Add(msg);
            }
            isModify = true;
            TotalSize -= (long)msg.Size;
            OnPropertyChanged(nameof(Delete));
        }
        public void Remove(MailMessage msg) {
            lock (__lock)
                Items.Remove(msg);
            isModify = true;
            TotalSize -= (long)msg.Size;
            OnPropertyChanged(nameof(Remove));
        }
        public void Add(MailMessage msg) {
            lock (__lock)
                Items.Add(msg);
            isModify = true;
            TotalSize += (long)msg.Size;
            OnPropertyChanged(nameof(Add));
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

        #region Find Message
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
        #endregion

        #region Folders
        public List<MailMessage> GetFromFolder(Global.DirectoryPlace place) {
            if (Global.IsAccountFolderValid(place))
                return new(Items.Where(x => x.Folder == place));
            return Items;
        }
        public bool MoveToFolder(Global.DirectoryPlace place, MailMessage msg) {
            if ((msg != null) && msg.MoveToFolder(place, RootDirectory)) {
                isModify = true;
                return true;
            }
            return false;
        }
        public bool MoveToFolder(Global.DirectoryPlace place, int id) {
            if (id > 0) {
                MailMessage msg = Find(id);
                return MoveToFolder(place, msg);
            }
            return false;
        }
        public async Task<bool> MoveToMailBox(int id, string email) {
            if (id > 0) {
                CacheOpenerData cache = new(typeof(MailMessages));
                try {
                    MailMessage msg = Find(id);
                    if (msg == default) return false;
                    msg = msg.MoveToMailBox(email);
                    if (msg == default) return false;

                    MailMessages msgs = await Global.Instance.MessagesManager.Open(cache, email)
                                                                             .ConfigureAwait(false);
                    if (msgs == default) return false;
                    msgs.Add(msg);

                    Remove(msg);
                    return true;
                }
                catch { }
                finally { Global.Instance.MessagesManager.Close(cache, email); }
            }
            return false;
        }
        #endregion

        #region Combine Messages
        public async Task<bool> CombineMessages(List<int> list)
        {
            if (runOnce.IsRunning || (list == null) || (list.Count <= 1))
                return false;

            return await Task.Run(async () => {
                List<Tuple<MailMessage, MimeMessage, string>> listMsgs = new();

                try {
                    MailMessage msg = Find(list[0]);
                    if (msg == null) return false;
                    Tuple<MailMessage, MimeMessage, string> primary = await msg.MimeMessageToText()
                                                                               .ConfigureAwait(false);
                    if ((primary == null) || (primary.Item2 == null) || string.IsNullOrWhiteSpace(primary.Item3))
                        return false;

                    (Global.DirectoryPlace place, string rootdir, DateTimeOffset dt) = Global.GetFolderInfo(msg.FilePath);
                    if ((place == Global.DirectoryPlace.None) || string.IsNullOrWhiteSpace(rootdir) || (dt == default))
                        return false;
                    listMsgs.Add(primary);

                    DirectoryInfo attachdir = Directory.CreateDirectory(
                        Global.AppendPartDirectory(Global.GetUserDirectory(rootdir), Global.DirectoryPlace.Attach, dt));

                    BodyBuilder builder = new();
                    builder.TextBody = primary.Item3;
                    MailMessage.CopyAttachments(builder, primary.Item2.Attachments);

                    for (int i = 1; i < list.Count; i++) {
                        try {
                            MailMessage mmsg = Find(list[i]);
                            if (mmsg == null) continue;

                            Tuple<MailMessage, MimeMessage, string> secondary = await mmsg.MimeMessageToText()
                                                                                          .ConfigureAwait(false);
                            if ((secondary == null) || (secondary.Item2 == null) || string.IsNullOrWhiteSpace(secondary.Item3))
                                continue;

                            listMsgs.Add(secondary);
                            builder.TextBody += secondary.Item3;
                            var attachs = secondary.Item2.Attachments;
                            if (attachs != null) {
                                MailMessage.CopyAttachments(builder, attachs);
                                foreach (var a in attachs) {
                                    try {
                                        string name = MailMessage.GetMimeEntryName(a);
                                        if (string.IsNullOrWhiteSpace(name))
                                            continue;

                                        (Global.DirectoryPlace pplace, string prootdir, DateTimeOffset pdt) = Global.GetFolderInfo(secondary.Item1.FilePath);
                                        if ((pplace == Global.DirectoryPlace.None) || string.IsNullOrWhiteSpace(prootdir) || (pdt == default))
                                            continue;

                                        FileInfo f = new(MailMessage.GetAttachFilePath(
                                            Global.AppendPartDirectory(Global.GetUserDirectory(rootdir), Global.DirectoryPlace.Attach, dt),
                                            secondary.Item1.MsgId, name));
                                        if ((f == null) || !f.Exists || f.IsReadOnly)
                                            continue;
                                        f.MoveTo(
                                            MailMessage.GetAttachFilePath(
                                                attachdir.FullName,
                                                primary.Item1.MsgId, name));

                                    } catch (Exception ex) { Global.Instance.Log.Add(nameof(CombineMessages), ex); }
                                }
                            }
                        } catch (Exception ex) { Global.Instance.Log.Add(nameof(CombineMessages), ex); }
                    }
                    primary.Item2.Body = builder.ToMessageBody();
                    await primary.Item2.WriteToAsync(msg.FilePath).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(CombineMessages), ex); }
                finally {
                    for (int i = 0; i < listMsgs.Count; i++) {
                        Tuple<MailMessage, MimeMessage, string> msg = listMsgs[i];
                        if ((msg != null) && (msg.Item1 != null)) {
                            Global.Instance.Log.Add(
                                nameof(CombineMessages),
                                string.Format("Combine {0}: {1} + {2}/{3}",
                                    (i == 0) ? "as primary" : "and remove",
                                    listMsgs[0].Item1.Id, msg.Item1.Id, msg.Item1.MsgId));
                            if (i > 0) Delete(msg.Item1);
                            if (msg.Item2 != null)
                                msg.Item2.Dispose();
                        }
                    }
                }
                return false;
            });
        }
        #endregion

        #region Base Messages Open/Load*/Save*
        public async Task<MailMessages> Open(string email) {
            RootDirectory = Global.GetUserDirectory(email);
            _ = await Load();
            if (Items.Count == 0)
                _ = await Scan().ConfigureAwait(false);
            return this;
        }
        public async Task<bool> Load(string path = default)
        {
            if (!runOnce.Begin())
                return false;
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
                finally { isModify = false; runOnce.End(); }
                return false;
            });
        }
        public async Task<bool> Save(string path = default)
        {
            if (!runOnce.Begin())
                return false;
            return await Task.Run(() => {
                try {
                    if (string.IsNullOrWhiteSpace(path)) {
                        if (string.IsNullOrWhiteSpace(RootDirectory))
                            return false;
                        if (!Directory.Exists(RootDirectory))
                            if (Directory.CreateDirectory(RootDirectory) == default)
                                return false;

                        Items.Sort(new DateComparer());
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
                finally { isModify = false; runOnce.End(); }
                return false;
            });
        }
        #endregion

        #region Base Messages Exist/Delete
        public bool MailMessagesExist() {
            if (string.IsNullOrWhiteSpace(RootDirectory))
                return false;
            if (!Directory.Exists(RootDirectory))
                if (Directory.CreateDirectory(RootDirectory) == default)
                    return false;
            FileInfo fi = new(
                Path.Combine(RootDirectory, $"{nameof(MailMessages)}.cache"));
            return (fi != default) && fi.Exists && (fi.Length > 0L);
        }

        public void MailMessagesDelete() {
            try {
                if (string.IsNullOrWhiteSpace(RootDirectory))
                    return;
                if (!Directory.Exists(RootDirectory))
                    return;
                FileInfo fi = new(
                    Path.Combine(RootDirectory, $"{nameof(MailMessages)}.cache"));
                if ((fi != default) && fi.Exists)
                    fi.Delete();
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(MailMessagesDelete), ex); }
            finally { isModify = false; }
        }
        #endregion

        #region Scan*
        public async Task<bool> Scan()
        {
            if (!runOnce.Begin())
                return false;
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
                    if (list.Count > 0) {
                        list.Sort(new DateComparer());
                        for (int i = 0; i < list.Count; i++) {
                            list[i].Id = i + 1;
                            Add(list[i]);
                        }
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Scan), ex); }
                finally { isModify = true; runOnce.End(); }
                return Count > 0;
            });
        }
        #endregion

        #region Safe Message DeleteById/UnDeleted/ClearDeleted
        public async Task<bool> DeleteByMsgId(string msgid)
        {
            if (!runOnce.Begin())
                return false;
            return await Task.Run(() => {
                try {
                    MailMessage msg = Find(msgid);
                    if (msg == null) return false;
                    Delete(msg);
                    isModify = true;
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(ClearDeleted), ex); }
                finally { runOnce.End(); }
                return false;
            });
        }

        public async Task<bool> DeleteById(int id)
        {
            if (!runOnce.Begin())
                return false;
            return await Task.Run(() => {
                try {
                    MailMessage msg = Find(id);
                    if (msg == null) return false;
                    Delete(msg);
                    isModify = true;
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(ClearDeleted), ex); }
                finally { runOnce.End(); }
                return false;
            });
        }

        public async Task<bool> UnDeleted()
        {
            if (!runOnce.Begin())
                return false;
            return await Task.Run(() => {
                try {
                    if (ItemsDeleted.Count > 0) {
                        ItemsDeleted.Reverse();
                        foreach (MailMessage m in ItemsDeleted) Add(m);
                        lock (__lock)
                            ItemsDeleted.Clear();

                        isModify = true;
                        return true;
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(UnDeleted), ex); }
                finally { runOnce.End(); }
                return false;
            });
        }

        public async Task<bool> ClearDeleted()
        {
            if (!runOnce.Begin())
                return false;
            return await Task.Run(() => {
                try {
                    foreach (MailMessage m in ItemsDeleted) {
                        if (string.IsNullOrEmpty(m.FilePath))
                            continue;
                        FileDelete_(m.FilePath);
                    }
                    lock (__lock)
                        ItemsDeleted.Clear();

                    isModify = true;
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(ClearDeleted), ex); }
                finally { runOnce.End(); }
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
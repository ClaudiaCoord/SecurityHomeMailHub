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
        public void OnChange() { isModify = true; OnPropertyChanged(nameof(MailMessages)); }
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
        #endregion

        #region Combine Messages
        public async Task<bool> CombineMessages(List<int> list)
        {
            if (runOnce.IsRunning || (list == null) || (list.Count <= 1))
                return false;
            return await Task.Run(async () => {
                Tuple<MimeMessage, string> ti =
                    default(Tuple<MimeMessage, string>);

                try {
                    MailMessage msg = Find(list[0]);
                    if (msg == null) return false;
                    ti = await msg.MimeMessageToText()
                                  .ConfigureAwait(false);
                    if ((ti == null) || (ti.Item1 == null) || string.IsNullOrWhiteSpace(ti.Item2))
                        return false;

                    (Global.DirectoryPlace place, string rootdir, DateTimeOffset dt) = Global.GetFolderInfo(msg.FilePath);
                    if ((place == Global.DirectoryPlace.None) || string.IsNullOrWhiteSpace(rootdir) || (dt == default))
                        return false;

                    DirectoryInfo attachdir = Directory.CreateDirectory(
                        Global.AppendPartDirectory(Global.GetUserDirectory(rootdir), Global.DirectoryPlace.Attach, dt));

                    BodyBuilder builder = new();
                    builder.TextBody = ti.Item2;
                    MailMessage.CopyAttachments(builder, ti.Item1.Attachments);

                    for (int i = 1; i < list.Count; i++) {
                        MailMessage pmsg =
                            default(MailMessage);
                        Tuple<MimeMessage, string> tp =
                            default(Tuple<MimeMessage, string>);
                        bool isAdded = false;

                        try {
                            pmsg = Find(list[i]);
                            tp = await pmsg.MimeMessageToText()
                                          .ConfigureAwait(false);
                            if ((tp == null) || (tp.Item1 == null) || string.IsNullOrWhiteSpace(tp.Item2))
                                continue;

                            builder.TextBody += tp.Item2;
                            IEnumerable<MimeEntity> attachs = tp.Item1.Attachments;
                            if (attachs != null)
                                foreach (var a in attachs) {
                                    try { builder.Attachments.Add(a); } catch { }
                                    try {
                                        string name = MailMessage.GetMimeEntryName(a);
                                        if (string.IsNullOrWhiteSpace(name))
                                            continue;

                                        (Global.DirectoryPlace pplace, string prootdir, DateTimeOffset pdt) = Global.GetFolderInfo(pmsg.FilePath);
                                        if ((pplace == Global.DirectoryPlace.None) || string.IsNullOrWhiteSpace(prootdir) || (pdt == default))
                                            continue;

                                        FileInfo f = new (MailMessage.GetAttachFilePath(
                                            Global.AppendPartDirectory(Global.GetUserDirectory(rootdir), Global.DirectoryPlace.Attach, dt),
                                            pmsg.MsgId, name));
                                        if ((f == null) || !f.Exists || f.IsReadOnly)
                                            continue;
                                        f.MoveTo(
                                            MailMessage.GetAttachFilePath(
                                                attachdir.FullName,
                                                pmsg.MsgId, name));
                                    } catch { }
                                }
                            isAdded = true;
                        }
                        catch (Exception ex) { Global.Instance.Log.Add(nameof(CombineMessages), ex); }
                        finally {
                            if ((tp != null) && (tp.Item1 != null))
                                tp.Item1.Dispose();
                            if (isAdded && (pmsg != null)) {
                                Global.Instance.Log.Add(
                                    nameof(CombineMessages),
                                    $"Combine and remove: {pmsg.Id}/{pmsg.MsgId}");
                                Delete(pmsg);
                            }
                        }
                    }
                    ti.Item1.Body = builder.ToMessageBody();
                    await ti.Item1.WriteToAsync(msg.FilePath).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(CombineMessages), ex); }
                finally {
                    if ((ti != null) && (ti.Item1 != null))
                        ti.Item1.Dispose();
                }
                return false;
            });
        }
        #endregion

        #region Load* / Save* / Open / Exist / Delete
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

        #region New Message to send/msg/out*
        public async Task<bool> SpoolOutToOut(string path)
        {
            if (!runOnce.Begin())
                return false;
            return await Task.Run(async () => {
                try {
                    MailMessage msg = await new MailMessage().CreateAndDelivery(Global.DirectoryPlace.Out, path, (Items.Count + 1))
                                                             .ConfigureAwait(false);
                    if (msg != null) {
                        Add(msg);

                        isModify = true;
                        return true;
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(ClearDeleted), ex); }
                finally { runOnce.End(); }
                return false;
            });
        }

        public async Task<bool> SpoolInToMsg(string path)
        {
            if (!runOnce.Begin())
                return false;
            return await Task.Run(async () => {
                try {
                    MailMessage msg = await new MailMessage().CreateAndDelivery(Global.DirectoryPlace.Msg, path, (Items.Count + 1))
                                                             .ConfigureAwait(false);
                    if (msg != null) {
                        Add(msg);

                        isModify = true;
                        return true;
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(ClearDeleted), ex); }
                finally { runOnce.End(); }
                return false;
            });
        }
        #endregion

        #region Delete One Message*
        public async Task<bool> DeleteMessage(string msgid)
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

        public async Task<bool> DeleteMessage(int id)
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
        #endregion

        #region Safe Delete*
        public async Task<bool> UnDelete()
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
                catch (Exception ex) { Global.Instance.Log.Add(nameof(UnDelete), ex); }
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
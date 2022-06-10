/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SecyrityMail;
using SecyrityMail.Messages;
using Terminal.Gui;
using static HomeMailHub.Gui.GuiMessagesListWindow;
using GuiAttribute = Terminal.Gui.Attribute;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui.ListSources
{
    public enum TableSort : int
    {
        None = 0,
        SortUp,
        SortDown,
        SortSubj,
        SortDate,
        SortFrom
    }

    public enum ExportType : int
    {
        None = 0,
        Eml,
        Msg
    }

    internal class SubjComparer : IComparer<MailMessage> {
        private const string tag = "Re: ";
        public int Compare(MailMessage m1, MailMessage m2) {

            string x = m1.Subj,
                   y = m2.Subj;
            int ix = x.IndexOf(tag),
                iy = y.IndexOf(tag);
            if ((ix >= 0) && (iy >= 0))
                return string.Compare(x, ix, y, iy,
                    ((x.Length - ix) > (y.Length - iy)) ? (y.Length - iy) : (x.Length - ix), true);
            else if (ix >= 0)
                return string.Compare(x, ix, y, 0,
                    ((x.Length - ix) > y.Length) ? y.Length : (x.Length - ix), true);
            else if (iy >= 0)
                return string.Compare(x, 0, y, iy,
                    (x.Length > (y.Length - iy)) ? (y.Length - iy) : x.Length, true);
            else
                return string.Compare(x, y, true);
        }
    }

    internal class DateComparer : IComparer<MailMessage> {
        public int Compare(MailMessage x, MailMessage y) =>
            DateTimeOffset.Compare(x.Date, y.Date);
    }

    internal class FromComparer : IComparer<MailMessage> {
        public int Compare(MailMessage x, MailMessage y) =>
            string.Compare(x.From, y.From);
    }

    internal class MessagesDataTable : DataTable, IDisposable
    {
        private string hiddencol { get; set; }
        private WeakReference<TableView> tableWeak { get; set; } = default(WeakReference<TableView>);
        private MessagesCacheOpener cacheOpener { get; set; } = default(MessagesCacheOpener);
        private MailMessages messages { get; set; } = default(MailMessages);
        private ColorScheme UnReradColorScheme { get; set; } = default;
        private string userId { get; set; } = string.Empty;
        private TableSort sortDirections = TableSort.SortUp;
        private Global.DirectoryPlace placeFolder = Global.DirectoryPlace.Root;
        private bool headerOnce = false;

        public MessagesDataTable(string id, TableView tv) : base() { Init(id, tv); }

        public MailMessage Get(string s) => !IsEmpty ?
            (from i in messages.Items where i.MsgId == s select i).FirstOrDefault() : default;
        public bool IsEmpty => (messages == default) || (messages.Count == 0);
        public int Count => base.Rows.Count;
        public int Deleted { get; private set; } = 0;
        public TableSort SortDType => sortDirections;
        public Global.DirectoryPlace FolderType => placeFolder;
        public string FolderString => (placeFolder == Global.DirectoryPlace.Root) ?
            $"{RES.TAG_FOLDER} {RES.TAG_ALL}" : $"{RES.TAG_FOLDER} {placeFolder}";
        public MessagesDataTableMultiselect Multiselected { get; } = new();

        public void DataClear() => Clear();

        public async new void Dispose() {

            await cacheOpener.Close().ConfigureAwait(false);
            base.Dispose();
        }

        #region Load
        public async Task<bool> LoadMessages() =>
            await Task.Run(async () => {
                if (messages == default)
                    messages = await cacheOpener.Open(userId)
                                                .ConfigureAwait(false);
                else
                    messages = await cacheOpener.ReOpen(userId)
                                                .ConfigureAwait(false);
                if (messages == default)
                    return false;

                Clear();
                if (!IsEmpty) {
                    SortData(sortDirections, false);
                    BuildViewColumns();
                    TableRefresh();
                }
                return true;
            });
        #endregion

        #region Show Folder
        public void ShowFolder() => ShowFolder(placeFolder);
        public void ShowFolder(Global.DirectoryPlace place) {
            placeFolder = place;
            Clear();
            SortData(sortDirections);
        }
        #endregion

        #region Message move to Folder
        public void MoveToFolder(Global.DirectoryPlace place) {
            try {
                if (IsEmpty || Multiselected.IsEmpty ||
                   !Global.IsAccountFolderValid(place)) return;

                for (int i = 0; i < Multiselected.Count; i++) {
                    try {
                        int idx = GetId(Multiselected[i]);
                        if (idx <= 0) continue;
                        messages.MoveToFolder(place, idx);
                    } catch { }
                }
            } catch (Exception ex) { ex.StatusBarError(); }
        }
        #endregion

        #region Export messages
        public async Task<bool> ExportMessages(ExportType t, string path) =>
            await Task.Run(async () => {
                try {
                    if (IsEmpty || Multiselected.IsEmpty) return false;

                    for (int i = 0; i < Multiselected.Count; i++) {
                        try {
                            MailMessage msg = Get(Multiselected[i]);
                            if (msg == null) continue;

                            string name = Regex.Replace(msg.Subj, @"\p{C}+", string.Empty)
                                            .Replace('\\', '_')
                                            .Replace('/', '_')
                                            .Replace(':', '_')
                                            .Replace("__", "_")
                                            .Replace("\"", "")
                                            .Replace("'", "")
                                            .Replace(",", "")
                                            .Replace(".", "")
                                            .Normalize(),
                                   store = Path.Combine(path, $"{msg.Id} - {name}");
                            switch (t) {
                                case ExportType.Eml: {
                                        FileInfo f = new FileInfo(msg.FilePath);
                                        if ((f != null) && f.Exists)
                                            f.CopyTo($"{store}.eml", true);
                                        break;
                                    }
                                case ExportType.Msg: {
                                        _ = await msg.Save($"{store}.msg").ConfigureAwait(false);
                                        break;
                                    }
                            }
                        } catch { }
                    }
                } catch (Exception ex) { ex.StatusBarError(); }
                return false;
            });
        #endregion

        #region Combine Message
        public async Task<bool> CombineMessages() {
            try {
                if (IsEmpty || Multiselected.IsEmpty) return false;

                List<int> list = new();
                for (int i = 0; i < Multiselected.Count; i++) {
                    try {
                        int idx = GetId(Multiselected[i]);
                        if (idx <= 0) continue;
                        list.Add(idx);
                    } catch { }
                }
                if (list.Count > 0) {
                    Deleted += list.Count - 1;
                    return await messages.CombineMessages(list);
                }
            } catch (Exception ex) { ex.StatusBarError(); }
            return false;
        }
        #endregion

        #region Read Message
        public void SetReadMessage(int i, bool b, bool isrefresh = true) {
            try {
                MailMessage msg = Get(i);
                if (msg == null) return;
                msg.IsRead = b;
                Application.MainLoop?.Invoke(() => {
                    try {
                        base.Rows[i].AcceptChanges();
                        base.Rows[i][hiddencol] = b;
                    } catch { }
                });
                if (isrefresh) {
                    messages.OnChange();
                    TableRefresh();
                }
            } catch (Exception ex) { ex.StatusBarError(); }
        }

        public async Task<bool> SetReadMessages(bool b) =>
            await Task.Run(() => {
                try {
                    if (IsEmpty || Multiselected.IsEmpty) return false;

                    for (int i = 0; i < Multiselected.Count; i++)
                        SetReadMessage(Multiselected[i], b, false);
                    messages.OnChange();
                    TableRefresh();
                } catch (Exception ex) { ex.StatusBarError(); }
                return false;
            });

        public void SetReadAllMessage() {
            try {
                List<MailMessage> list = messages.GetFromFolder(placeFolder);
                for (int i = 0; i < list.Count; i++) list[i].IsRead = true;
                messages.OnChange();
                Clear();
                SortData();
            } catch (Exception ex) { ex.StatusBarError(); }
        }
        #endregion

        #region Safe Delete
        public async Task<bool> SafeDelete() =>
            await Task.Run(async () => {
                try {
                    if (IsEmpty || Multiselected.IsEmpty) return false;

                    for (int i = 0; i < Multiselected.Count; i++) {
                        try {
                            int idx = GetId(Multiselected[i]);
                            if (idx <= 0) continue;
                            Deleted++;
                            _ = await messages.DeleteById(idx).ConfigureAwait(false);
                        } catch (Exception ex) { ex.StatusBarError(); }
                    }
                    if ((Deleted > 0) && tableWeak.TryGetTarget(out TableView tv)) {
                        Clear();
                        SortData();
                        Application.MainLoop?.Invoke(() => tv.Table = this);
                        return true;
                    }
                } catch (Exception ex) { ex.StatusBarError(); }
                return false;
            });

        public async Task<bool> SafeDeleteAll() =>
            await Task.Run(() => {

                if (IsEmpty) return false;
                try {
                    Deleted = messages.Count;
                    for (int i = messages.Count - 1; 0 <= i; i--)
                        messages.Delete(messages[i]);
                    Clear();
                    TableRefresh();
                    return true;
                } catch { }
                return false;
            });

        public async Task<bool> UnDeleted() {
            try {
                if (messages == default) return false;
                bool b = await messages.UnDeleted().ConfigureAwait(false);
                if (b) {
                    Deleted = 0;
                    Clear();
                    SortData();
                }
                return b;
            } catch { }
            return false;
        }

        public async Task<bool> ClearDeleted() {
            try {
                if (messages == default) return false;
                Deleted = 0;
                return await messages.ClearDeleted().ConfigureAwait(false);
            } catch { }
            return false;
        }
        #endregion

        #region Scroll
        public void ScrollToStart() {
            if (!tableWeak.TryGetTarget(out TableView tv))
                return;
            Application.MainLoop?.Invoke(() => {
                tv.ChangeSelectionToStartOfTable(false);
                tv.EnsureSelectedCellIsVisible();
                tv.SetNeedsDisplay();
            });
        }
        #endregion

        #region Init
        private void Init(string id, TableView tv) {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(userId));
            if (tv == null)
                throw new ArgumentNullException(nameof(tableWeak));

            userId = id;
            tableWeak = new(tv);
            hiddencol = RES.TAG_MSGREADING;
            cacheOpener = CacheOpener.Build(this.GetType());

            Columns.Add(RES.TAG_DT_NUM);
            Columns.Add(RES.TAG_DT_SUBJ);
            Columns.Add(RES.TAG_DT_DATE);
            Columns.Add(hiddencol, typeof(bool));
            Columns.Add(RES.TAG_FROM, typeof(string));

            GuiAttribute cnnorm  = Application.Driver.MakeAttribute(Color.Gray, Color.Blue);
            GuiAttribute crnorm  = Application.Driver.MakeAttribute(Color.White, Color.Blue);
            GuiAttribute crfocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.BrightBlue);
            GuiAttribute cdis    = Application.Driver.MakeAttribute(Color.DarkGray, Color.Blue);

            UnReradColorScheme = new ColorScheme {
                Normal = crnorm,
                HotNormal = crnorm,
                Focus = crfocus,
                HotFocus = crfocus,
                Disabled = cdis
            };
            tv.ColorScheme = new ColorScheme {
                Normal = cnnorm,
                HotNormal = cnnorm,
                Focus = crfocus,
                HotFocus = crfocus,
                Disabled = cdis
            };
        }

        private void BuildViewColumns() {
            if (tableWeak == null) return;
            if (!tableWeak.TryGetTarget(out TableView tv))
                return;
            tv.Table = this;
            if (headerOnce) return;
            headerOnce = true;
            tv.Style.ColumnStyles.Add(tv.Table.Columns[RES.TAG_DT_NUM],
                new TableView.ColumnStyle() { MinWidth = 3,  MaxWidth = 3, Alignment = TextAlignment.Centered });
            tv.Style.ColumnStyles.Add(tv.Table.Columns[RES.TAG_DT_SUBJ],
                new TableView.ColumnStyle() { MinWidth = 94, MaxWidth = tv.MaxCellWidth, Alignment = TextAlignment.Left });
            tv.Style.ColumnStyles.Add(tv.Table.Columns[RES.TAG_DT_DATE],
                new TableView.ColumnStyle() { MinWidth = 18, MaxWidth = 18, Alignment = TextAlignment.Justified });
            tv.Style.ColumnStyles.Add(tv.Table.Columns[RES.TAG_MSGREADING],
                new TableView.ColumnStyle() { MinWidth = 10, MaxWidth = 10, Alignment = TextAlignment.Justified });
            tv.Style.ColumnStyles.Add(tv.Table.Columns[RES.TAG_FROM],
                new TableView.ColumnStyle() { MinWidth = 40, MaxWidth = 40, Alignment = TextAlignment.Left });

            tv.Style.RowColorGetter = (a) => IsRowRead(a.RowIndex) ? UnReradColorScheme : null;
            tv.Style.GetOrCreateColumnStyle(tv.Table.Columns[RES.TAG_MSGREADING]).RepresentationGetter =
                (k) => (bool)k ? RES.TAG_YES : RES.TAG_NO;
        }
        #endregion

        #region Show Sort
        public void ShowSort(TableSort ts)
        {
            Clear();
            SortData(ts);
        }

        private void SortData(TableSort ts = TableSort.None, bool isrefresh = true) {
            if (IsEmpty) return;
            if (ts == TableSort.None) {
                if (sortDirections == TableSort.None)
                    return;
                ts = sortDirections;
            } else
                sortDirections = ts;

            switch (ts) {
                case TableSort.SortUp: {
                        List<MailMessage> list = messages.GetFromFolder(placeFolder);
                        for (int i = list.Count - 1; 0 <= i; i--) RowsAdd(list[i]);
                        break;
                    }
                case TableSort.SortDown: {
                        List<MailMessage> list = messages.GetFromFolder(placeFolder);
                        for (int i = 0; i < list.Count; i++) RowsAdd(list[i]);
                        break;
                    }
                case TableSort.SortSubj: {
                        List<MailMessage> list = messages.GetFromFolder(placeFolder);
                        list.Sort(new SubjComparer());
                        for (int i = 0; i < list.Count; i++) RowsAdd(list[i]);
                        break;
                    }
                case TableSort.SortDate: {
                        List<MailMessage> list = messages.GetFromFolder(placeFolder);
                        list.Sort(new DateComparer());
                        for (int i = 0; i < list.Count; i++) RowsAdd(list[i]);
                        break;
                    }
                case TableSort.SortFrom: {
                        List<MailMessage> list = messages.GetFromFolder(placeFolder);
                        list.Sort(new FromComparer());
                        for (int i = 0; i < list.Count; i++) RowsAdd(list[i]);
                        break;
                    }
            }
            if (isrefresh)
                TableRefresh();
        }
        #endregion

        public MailMessage Get(int i) {
            try {
                int idx = GetId(i);
                if (idx < 0) return default;
                return (from k in messages.Items where k.Id == idx select k).FirstOrDefault();
            } catch (Exception ex) { ex.StatusBarError(); }
            return default;
        }

        private void TableRefresh() {
            if (!tableWeak.TryGetTarget(out TableView tv))
                return;
            Application.MainLoop?.Invoke(() => { tv.SetChildNeedsDisplay(); tv.SetNeedsDisplay(); });
        }

        private void RowsAdd(MailMessage msg) =>
            _ = Rows.Add(msg.Id, msg.Subj.Normalize(), $" {msg.Date:MM/dd/yy HH:mm} ", msg.IsRead, msg.From);

        private bool IsRowRead(int i) => (base.Rows[i]?[hiddencol] is bool b) && !b;

        private int GetId(int i) {
            try {
                do {
                    if (!IsEmpty && ((i < 0) || (i >= Count))) break;
                    DataRow dataRow = base.Rows[i];
                    if ((dataRow == null) || (dataRow.ItemArray == null) || (dataRow.ItemArray.Length == 0)) break;
                    if (int.TryParse(dataRow.ItemArray[0].ToString(), out int n))
                        return n;
                } while (false);
            } catch (Exception ex) { ex.StatusBarError(); }
            return -1;
        }
    }
}

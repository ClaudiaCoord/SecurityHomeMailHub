/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HomeMailHub.Gui.Dialogs;
using HomeMailHub.Gui.ListSources;
using NStack;
using SecyrityMail;
using SecyrityMail.MailAccounts;
using SecyrityMail.Messages;
using SecyrityMail.Utils;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiMessagesListWindow : Window, IGuiWindow<GuiMessagesListWindow>
    {
        public enum SelectorType : int {
            None = 0,
            MultiSelect,
            SingleSelect
        }

        private Toplevel GuiToplevel { get; set; } = default;
        private MenuBar  GuiMenu { get; set; } = default;
        private MenuBarItem undeleteMenu { get; set; } = default;
        private MenuItem [] messagesMenu { get; set; } = default;
        private MenuItem multiSelectMenu { get; set; } = default;
        private ContextMenu contextMenu { get; set; } = default;
        private TableView tableView { get; set; } = default;

        private Button buttonClose { get; set; } = default;
        private Button buttonDelete { get; set; } = default;
        private Button buttonReply { get; set; } = default;
        private Button buttonOpen { get; set; } = default;

        private Label idLabel { get; set; } = default;
        private Label msgIdLabel { get; set; } = default;
        private Label fromLabel { get; set; } = default;
        private Label folderLabel { get; set; } = default;
        private Label sizeLabel { get; set; } = default;
        private Label subjLabel { get; set; } = default;
        private Label dateLabel { get; set; } = default;

        private Label idText { get; set; } = default;
        private Label msgIdText { get; set; } = default;
        private Label fromText { get; set; } = default;
        private Label folderText { get; set; } = default;
        private Label sizeText { get; set; } = default;
        private Label subjText { get; set; } = default;
        private Label dateText { get; set; } = default;

        private Label infoNameLabel { get; set; } = default;
        private Label infoLoginLabel { get; set; } = default;
        private Label infoEmailLabel { get; set; } = default;
        private Label infoPgpLabel { get; set; } = default;
        private Label infoNameText { get; set; } = default;
        private Label infoLoginText { get; set; } = default;
        private Label infoEmailText { get; set; } = default;
        private Label infoPgpText { get; set; } = default;

        private FrameView frameMsg { get; set; } = default;
        private FrameView frameInfo { get; set; } = default;
        private RadioGroup sortTitle { get; set; } = default;
        private CheckBox readingBox { get; set; } = default;
        private GuiBusyBar waitBusyBar { get; set; } = default;

        private bool isMultiSelect = false;
        private bool IsMenuOpen { get; set; } = false;
        private string selectedName { get; set; } = string.Empty;
        private string selectedPath { get; set; } = string.Empty;
        private MessagesDataTable dataTable { get; set; } = default;
        private GuiLinearLayot linearLayot { get; } = new();
        private GuiRunOnce runOnce { get; } = new();

        public Toplevel GetTop => GuiToplevel;

        private bool IsMultiSelect {
            get => isMultiSelect;
            set {
                isMultiSelect = value;
                Application.MainLoop.Invoke(() => {
                    tableView.MultiSelect = value;
                    multiSelectMenu.Checked = value;
                });
            }
        }

        public GuiMessagesListWindow() : base(RES.GUIMESSAGE_TITLE1, 0) {
            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill() - 1;
            GuiToplevel = GuiExtensions.CreteTop();

            linearLayot.Add("en", new List<GuiLinearData> {
                new GuiLinearData(78, 2, false),
                new GuiLinearData(84, 2, false),
                new GuiLinearData(92, 2, false),
                new GuiLinearData(100, 2, false),
                new GuiLinearData(54, 4, true),
                new GuiLinearData(73, 4, true),
                new GuiLinearData(83, 4, true),
                new GuiLinearData(94, 4, true),
                new GuiLinearData(104, 4, true)
            });
            linearLayot.Add("ru", new List<GuiLinearData> {
                new GuiLinearData(78, 2, false),
                new GuiLinearData(86, 2, false),
                new GuiLinearData(94, 2, false),
                new GuiLinearData(101, 2, false),
                new GuiLinearData(52, 4, true),
                new GuiLinearData(65, 4, true),
                new GuiLinearData(77, 4, true),
                new GuiLinearData(89, 4, true),
                new GuiLinearData(102, 4, true)
            });
        }

        public new void Dispose() {

            if (tableView != null)
                tableView.KeyUp -= TableView_KeyUp;
            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Init
        public GuiMessagesListWindow Init(string s)
        {
            int idx = 0;
            selectedName = s;
            List<GuiLinearData> layout = linearLayot.GetDefault();

            #region table View
            tableView = new TableView()
            {
                X = 1,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 8,
                FullRowSelect = true,
                MultiSelect = IsMultiSelect,
                Style = new TableView.TableStyle() {
                    AlwaysShowHeaders = false,
                    ShowVerticalCellLines = true,
                    ShowHorizontalHeaderUnderline = true,
                    ShowHorizontalHeaderOverline = true
                },
            };
            tableView.CellActivated += TableView_CellActivated;
            tableView.SelectedCellChanged += TableView_SelectedCellChanged;
            tableView.KeyUp += TableView_KeyUp;
            tableView.MouseClick += (a) => {
                if (a.MouseEvent.Flags == MouseFlags.Button3Clicked) {
                    a.Handled = true;
                    contextMenu.Position = new Point(
                        (a.MouseEvent.X < 7) ? 7 : a.MouseEvent.X,
                        (a.MouseEvent.Y >= 2) ? (a.MouseEvent.Y + 2) : a.MouseEvent.Y);
                    contextMenu.Show();
                }
            };
            dataTable = new MessagesDataTable(selectedName, tableView);
            Add(tableView);
            #endregion

            Add(waitBusyBar = new GuiBusyBar()
            {
                X = 6,
                Y = 1,
                Width = 93,
                Height = 1
            });

            #region frameMsg
            frameMsg = new FrameView(RES.TAG_MESSAGE)
            {
                X = 1,
                Y = Pos.Bottom(tableView),
                Width = 117,
                Height = 8
            };
            frameMsg.Add(sortTitle = new RadioGroup(new ustring[] { $" {(char)0x2191}", $"{(char)0x2193} " })
            {
                X = 107,
                Y = 0,
                AutoSize = true,
                NoSymbol = true,
                DisplayMode = DisplayModeLayout.Horizontal,
                SelectedItem = 1
            });
            frameMsg.Add(idLabel = new Label("Id: ")
            {
                X = 1,
                Y = 1,
                AutoSize = true
            });
            frameMsg.Add(idText = new Label(string.Empty)
            {
                X = 5,
                Y = 1,
                Width = 5,
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
            });
            frameMsg.Add(msgIdLabel = new Label("MsgId: ")
            {
                X = 11,
                Y = 1,
                AutoSize = true
            });
            frameMsg.Add(msgIdText = new Label(string.Empty)
            {
                X = 18,
                Y = 1,
                Width = 58,
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
            });
            frameMsg.Add(dateLabel = new Label(RES.TAG_DATE)
            {
                X = 78,
                Y = 1,
                AutoSize = true
            });
            frameMsg.Add(dateText = new Label(string.Empty)
            {
                X = 84,
                Y = 1,
                Width = 28,
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
            });
            frameMsg.Add(fromLabel = new Label(RES.TAG_FROM)
            {
                X = 1,
                Y = 2,
                AutoSize = true
            });
            frameMsg.Add(fromText = new Label(string.Empty)
            {
                X = 11,
                Y = 2,
                Width = 65,
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
            });
            frameMsg.Add(sizeLabel = new Label(RES.TAG_SIZE)
            {
                X = layout[idx].X,
                Y = layout[idx++].Y,
                AutoSize = true
            });
            frameMsg.Add(sizeText = new Label(string.Empty)
            {
                X = layout[idx].X,
                Y = layout[idx++].Y,
                Width = 10,
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
            });
            frameMsg.Add(folderLabel = new Label(RES.TAG_FOLDER)
            {
                X = layout[idx].X,
                Y = layout[idx++].Y,
                AutoSize = true
            });
            frameMsg.Add(folderText = new Label(string.Empty)
            {
                X = layout[idx].X,
                Y = layout[idx++].Y,
                Width = 10,
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
            });
            frameMsg.Add(subjLabel = new Label(RES.TAG_SUBJECT)
            {
                X = 1,
                Y = 3,
                AutoSize = true
            });
            frameMsg.Add(subjText = new Label(string.Empty)
            {
                X = 11,
                Y = 3,
                AutoSize = true,
                ColorScheme = GuiApp.ColorDescription
            });
            frameMsg.Add(readingBox = new CheckBox(RES.TAG_MSGREADING)
            {
                X = layout[idx].X,
                Y = layout[idx++].Y,
                Width = 10,
                Height = 1,
                Checked = false
            });
            frameMsg.Add(buttonClose = new Button(10, 19, RES.BTN_CLOSE)
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
            });
            frameMsg.Add(buttonDelete = new Button(10, 19, RES.BTN_DELETE)
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
            });
            frameMsg.Add(buttonReply = new Button(10, 19, RES.BTN_REPLAY)
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
            });
            frameMsg.Add(buttonOpen = new Button(10, 19, RES.BTN_OPEN)
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
            });
            #endregion

            #region frameInfo
            frameInfo = new FrameView(RES.MENU_MAILACCOUNT.ClearText())
            {
                X = Pos.Right(frameMsg) + 1,
                Y = Pos.Bottom(tableView),
                Width = Dim.Fill() - 1,
                Height = 8
            };
            frameInfo.Add(infoNameLabel = new Label(RES.TAG_NAME)
            {
                X = 1,
                Y = 1,
                AutoSize = true
            });
            frameInfo.Add(infoNameText = new Label(string.Empty)
            {
                X = 12,
                Y = 1,
                Width = Dim.Fill() - 2,
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
            });
            frameInfo.Add(infoLoginLabel = new Label(RES.TAG_LOGIN)
            {
                X = 1,
                Y = 2,
                AutoSize = true
            });
            frameInfo.Add(infoLoginText = new Label(string.Empty)
            {
                X = 12,
                Y = 2,
                Width = Dim.Fill() - 2,
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
            });
            frameInfo.Add(infoEmailLabel = new Label(RES.TAG_EMAIL)
            {
                X = 1,
                Y = 3,
                AutoSize = true
            });
            frameInfo.Add(infoEmailText = new Label(string.Empty)
            {
                X = 12,
                Y = 3,
                Width = Dim.Fill() - 2,
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
            });
            frameInfo.Add(infoPgpLabel = new Label($"{RES.CHKBOX_PGPAUTODECRYPT.ClearText()}:")
            {
                X = 1,
                Y = 4,
                AutoSize = true
            });
            frameInfo.Add(infoPgpText = new Label(string.Empty)
            {
                X = Pos.Right(infoPgpLabel) + 1,
                Y = 4,
                Width = Dim.Fill() - 2,
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
            });
            Add(frameInfo);
            #endregion

            buttonClose.Clicked += () => {
                CloseDialog();
                Application.RequestStop();
            };
            buttonDelete.Clicked += () => {
                if (dataTable.IsEmpty) return;
                DeleteDialog();
            };
            buttonReply.Clicked += () => {
                if (dataTable.IsEmpty || string.IsNullOrEmpty(selectedPath))
                    return;
                LocalLauncher<GuiMessageWriteWindow>();
            };
            buttonOpen.Clicked += () => {
                if (dataTable.IsEmpty || string.IsNullOrEmpty(selectedPath))
                    return;
                LocalLauncher<GuiMessageReadWindow>();
            };
            sortTitle.SelectedItemChanged += SortTitle_SelectedItemChanged;
            readingBox.Toggled += ReadingBox_Toggled;
            Add(frameMsg);

            multiSelectMenu = RES.MENU_SUB_MULTISELECT.CreateCheckedMenuItem((b) => {
                if (b) IsMultiSelect = !IsMultiSelect;
                return IsMultiSelect;
            }, true);

            messagesMenu = new MenuItem[]
            {
                multiSelectMenu,
                new MenuItem (
                    RES.MENU_MSGSSELECTALL, "", async () => {
                        if (!IsMultiSelect) IsMultiSelect = true;
                        await Task.Delay(150).ConfigureAwait(false);
                        Application.MainLoop?.Invoke(() => {
                            tableView.SelectAll();
                            tableView.SetNeedsDisplay();
                        });
                    }),
                null,
                new MenuBarItem (RES.MENU_SUB_MOVEFOLDER, new MenuItem [] {
                    new MenuItem (
                        $"_{Global.DirectoryPlace.Msg}", "", async () => await MoveToFolder(Global.DirectoryPlace.Msg).ConfigureAwait(false),
                        () => FolderMoveEnable(Global.DirectoryPlace.Msg)),
                    new MenuItem (
                        $"_{Global.DirectoryPlace.Spam}", "", async () => await MoveToFolder(Global.DirectoryPlace.Spam).ConfigureAwait(false),
                        () => FolderMoveEnable(Global.DirectoryPlace.Spam)),
                    new MenuItem (
                        $"_{Global.DirectoryPlace.Error}", "", async () => await MoveToFolder(Global.DirectoryPlace.Error).ConfigureAwait(false),
                        () => FolderMoveEnable(Global.DirectoryPlace.Error)),
                    new MenuItem (
                        $"_{Global.DirectoryPlace.Bounced}", "", async () => await MoveToFolder(Global.DirectoryPlace.Bounced).ConfigureAwait(false),
                        () => FolderMoveEnable(Global.DirectoryPlace.Bounced))
                }),
                new MenuItem(RES.MENU_SUB_COMBINEMSG, "",
                    async () => await CombineMessages().ConfigureAwait(false),
                    () => MessagesCombineEnable()),
                null,
                new MenuItem(RES.MENU_SUB_OPEN, "", () => buttonOpen.OnClicked(), () => MessagesOptionsOneEnable()),
                new MenuItem(RES.MENU_SUB_REPLAY, "", () => buttonReply.OnClicked(), () => MessagesOptionsOneEnable()),
                new MenuItem(RES.MENU_SUB_DELETE, "", () => buttonDelete.OnClicked(), () => MessagesOptionsEnable()),
                new MenuBarItem (RES.MENU_EXPORT_FORMAT, new MenuItem [] {
                    new MenuItem("*.eml", "", () => ExportDialog(ExportType.Eml), () => MessagesOptionsEnable()),
                    new MenuItem("*.msg", "", () => ExportDialog(ExportType.Msg), () => MessagesOptionsEnable())
                }),
                new MenuBarItem (RES.MENU_SUB_READEDFLAG, new MenuItem [] {
                    new MenuItem (
                        RES.MENU_SUB_READEDMARK, "", async () => await SetReadMessages(true).ConfigureAwait(false),
                        () => MessagesOptionsEnable()),
                    new MenuItem (
                        RES.MENU_SUB_UNREADEDMARK, "", async () => await SetReadMessages(false).ConfigureAwait(false),
                        () => MessagesOptionsEnable())
                }),
                new MenuItem (RES.MENU_MSGSREADALL, "", () => dataTable.SetReadAllMessage(), () => MessagesOptionsOneEnable())
            };
            contextMenu = new ContextMenu(0, 0, new MenuBarItem("", messagesMenu));

            undeleteMenu = new MenuBarItem(RES.MENU_DELETEMENU, new MenuItem[] {
                new MenuItem (
                    RES.MENU_DELETEALL, "", () => DeleteAllDialog(),
                    () => !dataTable.IsEmpty)
            });

            GuiMenu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
                    new MenuItem (RES.BTN_OPEN, "", () =>
                        buttonOpen.OnClicked(), () => runOnce.IsValidId(),
                        null, Key.AltMask | Key.CursorRight),
                    new MenuItem (RES.MENU_RELOAD, "", async () => {
                        dataTable.ScrollToStart();
                        _ = await Load_().ConfigureAwait(false);
                    }, null, null, Key.AltMask | Key.R),
                    null,
                    new MenuItem (RES.MENU_CLOSE, "", () => {
                        CloseDialog();
                        Application.RequestStop();
                    }, null, null, Key.AltMask | Key.CursorLeft)
                }),
                new MenuBarItem (RES.MENU_MESSAGES, messagesMenu),
                undeleteMenu,
                new MenuBarItem (RES.MENU_SORT, new MenuItem [] {
                    new MenuItem (
                        RES.MENU_SORTUP, "", async () => await SortDialog(TableSort.SortUp).ConfigureAwait(false),
                        () => SortEnable(TableSort.SortUp)),
                    new MenuItem (
                        RES.MENU_SORTDOWN, "", async () =>  await SortDialog(TableSort.SortDown).ConfigureAwait(false),
                        () => SortEnable(TableSort.SortDown)),
                    new MenuItem (
                        RES.MENU_SORTSUBJ, "", async () =>  await SortDialog(TableSort.SortSubj).ConfigureAwait(false),
                        () => SortEnable(TableSort.SortSubj)),
                    new MenuItem (
                        RES.MENU_SORTDATE, "", async () =>  await SortDialog(TableSort.SortDate).ConfigureAwait(false),
                        () => SortEnable(TableSort.SortDate)),
                    new MenuItem (
                        RES.MENU_SORTFROM, "", async () =>  await SortDialog(TableSort.SortFrom).ConfigureAwait(false),
                        () => SortEnable(TableSort.SortFrom))
                }),
                new MenuBarItem (RES.MENU_FOLDER, new MenuItem [] {
                    new MenuItem (
                        $"{RES.TAG_FOLDER} _{RES.TAG_ALL}", "", async () => await SelectFolder(Global.DirectoryPlace.Root).ConfigureAwait(false),
                        () => FolderEnable(Global.DirectoryPlace.Root)),
                    new MenuItem (
                        $"{RES.TAG_FOLDER} _{Global.DirectoryPlace.Msg}", "", async () => await SelectFolder(Global.DirectoryPlace.Msg).ConfigureAwait(false),
                        () => FolderEnable(Global.DirectoryPlace.Msg)),
                    new MenuItem (
                        $"{RES.TAG_FOLDER} _{Global.DirectoryPlace.Spam}", "", async () => await SelectFolder(Global.DirectoryPlace.Spam).ConfigureAwait(false),
                        () => FolderEnable(Global.DirectoryPlace.Spam)),
                    new MenuItem (
                        $"{RES.TAG_FOLDER} _{Global.DirectoryPlace.Error}", "", async () => await SelectFolder(Global.DirectoryPlace.Error).ConfigureAwait(false),
                        () => FolderEnable(Global.DirectoryPlace.Error)),
                    new MenuItem (
                        $"{RES.TAG_FOLDER} _{Global.DirectoryPlace.Bounced}", "", async () => await SelectFolder(Global.DirectoryPlace.Bounced).ConfigureAwait(false),
                        () => FolderEnable(Global.DirectoryPlace.Bounced))
                })
            });

            GuiMenu.MenuOpened += (_) => IsMenuOpen = true;
            GuiToplevel.Add(GuiMenu, this);
            return this;
        }
        #endregion

        #region Load
        public async void Load() => _ = await Load_().ConfigureAwait(false);
        private async Task<bool> Load_() =>
            await Task.Run(async () => {
                try {
                    DataClear();
                    waitBusyBar.Start();
                    _ = await dataTable.LoadMessages();
                    SetTitle();
                    try {
                        UserAccount a = Global.Instance.Accounts.FindFromEmail(selectedName);
                        if ((a != null) && !a.IsEmpty) {
                            infoNameText.Text = a.Name;
                            infoLoginText.Text = a.Login;
                            infoEmailText.Text = a.Email;
                            infoPgpText.Text = a.IsPgpAutoDecrypt ? RES.TAG_YES : RES.TAG_NO;
                        }
                    } catch (Exception ex) { ex.StatusBarError(); }
                } catch (Exception ex) { ex.StatusBarError(); }
                finally { waitBusyBar.Stop(); }
                return true;
            });
        #endregion

        #region Clean
        private void DataClear() {
            Clean();
            SetTitle();
        }
        private void Clean() =>
            Application.MainLoop.Invoke(() => {
                idText.Text =
                msgIdText.Text =
                fromText.Text =
                folderText.Text =
                sizeText.Text =
                subjText.Text =
                dateText.Text =
                selectedPath = string.Empty;
                readingBox.Checked = false;
            });
        #endregion

        #region Update
        private void Update(MailMessage msg, int id) {
            if (!runOnce.Begin(id)) return;
            try {
                Application.MainLoop.Invoke(() => {
                    idText.Text = msg.Id.ToString();
                    msgIdText.Text = msg.MsgId;
                    fromText.Text = msg.From;
                    folderText.Text = msg.Folder.ToString();
                    sizeText.Text = msg.Size.Humanize();
                    subjText.Text = msg.Subj;
                    dateText.Text = msg.Date.ToString("dddd, dd MMMM yyyy");
                    readingBox.Checked = msg.IsRead;
                    selectedPath = msg.FilePath;
                });
                dataTable.Multiselected.Add(id);
            } finally { runOnce.End();  }
        }
        #endregion

        #region Gui Setter
        private void TableView_CellActivated(TableView.CellActivatedEventArgs obj) {
            if (dataTable.IsEmpty || tableView.MultiSelect)
                return;
            if ((obj.Row >= 0) && (obj.Row < dataTable.Count)) {
                MailMessage msg = dataTable.Get(obj.Row);
                if (msg == null) return;
                Update(msg, obj.Row);
                if (!string.IsNullOrEmpty(selectedPath))
                    LocalLauncher<GuiMessageReadWindow>();
            }
        }

        private void TableView_SelectedCellChanged(TableView.SelectedCellChangedEventArgs obj) {
            if (dataTable.IsEmpty || tableView.MultiSelect)
                return;
            if ((obj.NewRow >= 0) && (obj.NewRow < dataTable.Count)) {
                MailMessage msg = dataTable.Get(obj.NewRow);
                if (msg == null) return;
                Update(msg, obj.NewRow);
            }
        }

        private void TableView_KeyUp(KeyEventEventArgs obj) {
            if (obj != null) {
                switch (obj.KeyEvent.Key) {
                    case Key.Enter: {
                            if (!string.IsNullOrEmpty(selectedPath))
                                LocalLauncher<GuiMessageReadWindow>();
                            break;
                        }
                    case Key.Esc: {
                            if (!IsMenuOpen)
                                buttonClose.OnClicked();
                            IsMenuOpen = false;
                            break;
                        }
                }
            }
        }

        private async void SortTitle_SelectedItemChanged(SelectedItemChangedArgs obj) {
            if ((obj == null) || (obj.SelectedItem < 0)) return;
            Clean();
            if (obj.SelectedItem > 0)
                await SortDialog(TableSort.SortUp).ConfigureAwait(false);
            else
                await SortDialog(TableSort.SortDown).ConfigureAwait(false);
        }

        private async void ReadingBox_Toggled(bool b) {
            if (!runOnce.IsValidId()) return;
            _ = await SetReadMessages(b).ConfigureAwait(false);
        }

        private void SetTitle() =>
            Application.MainLoop.Invoke(() =>
                    base.Title = string.Format(RES.GUIMESSAGE_FMT3, selectedName, dataTable.Count, dataTable.FolderString));
        #endregion

        #region Export Dialog
        private async void ExportDialog(ExportType t) {
            try {
                GuiSaveDialog d = ((t == ExportType.Msg) ?
                                    $"{RES.MENU_EXPORT_FORMAT} *.msg".ClearText() :
                                    $"{RES.MENU_EXPORT_FORMAT} *.eml".ClearText()).GuiSaveDialogs(
                                        Global.GetRootDirectory(Global.DirectoryPlace.Export));
                Application.Run(d);
                if (!d.Canceled) {
                    string[] ss = d.GuiReturnDialog();
                    if (ss.Length > 0)
                        _ = await ExportMessages(t, ss[0]).ConfigureAwait(false);
                }
            } catch { }
        }
        #endregion

        #region Close Dialog
        private async void CloseDialog() {
            try {
                if (dataTable.Deleted > 0) {
                    if (Properties.Settings.Default.IsConfirmRestoreMessages) {
                        if (MessageBox.Query(50, 7,
                            string.Format(RES.GUIMESSAGE_FMT3, RES.TAG_DELETE, dataTable.Deleted, dataTable.FolderString),
                            string.Format(RES.GUIMESSAGE_FMT4, dataTable.Deleted), RES.TAG_NO, RES.TAG_YES) == 1)
                            _ = await dataTable.UnDeleted().ConfigureAwait(false);
                        else
                            _ = await dataTable.ClearDeleted().ConfigureAwait(false);
                    } else
                        _ = await dataTable.ClearDeleted().ConfigureAwait(false);
                }
            } catch { }
        }
        #endregion

        #region Delete Dialog
        private async void DeleteDialog() {
            try {
                if (Properties.Settings.Default.IsConfirmDeleteMessages) {
                    if (MessageBox.Query(50, 7,
                        string.Format(RES.GUIMESSAGE_FMT1, RES.TAG_DELETE),
                        string.Format(RES.GUIMESSAGE_FMT2, RES.TAG_DELETE), RES.TAG_YES, RES.TAG_NO) == 0) {
                        Clean();
                        _ = await DeleteMessages().ConfigureAwait(false);
                        if (dataTable.Deleted > 0) DeleteMenu();
                        SetTitle();
                    }
                }
            } catch { }
        }

        private async void DeleteAllDialog() {
            try {
                if (MessageBox.Query(50, 7,
                    RES.TAG_DELETE,
                    $"{RES.MENU_DELETEALL.ClearText()}?", RES.TAG_YES, RES.TAG_NO) == 0) {
                    Clean();
                    _ = await dataTable.SafeDeleteAll().ConfigureAwait(false);
                    if (dataTable.Deleted > 0) DeleteMenu();
                    SetTitle();
                }
            } catch { }
        }

        private void DeleteMenu() {
            try {
                bool b = dataTable.Deleted > 0;
                MenuItem[] items = new MenuItem[b ? 4 : 1];
                items[0] = undeleteMenu.Children[0];

                if (b) {
                    items[1] = null;
                    items[2] = new MenuItem(
                        string.Format(RES.MENU_FMT_UNDELETE, dataTable.Deleted), "",
                        async () => {
                            try {
                                waitBusyBar.Start();
                                _ = await dataTable.UnDeleted().ConfigureAwait(false);
                                DeleteMenu();
                                SetTitle();
                            } finally { waitBusyBar.Stop(); }
                        });
                    items[3] = new MenuItem(
                        string.Format(RES.MENU_FMT_DELETECLEAR, dataTable.Deleted), "",
                        async () => _ = await dataTable.ClearDeleted().ConfigureAwait(false));
                }
                Application.MainLoop.Invoke(() => undeleteMenu.Children = items);
            } catch { }
        }
        #endregion

        #region Sort Dialog
        private async Task<bool> SortDialog(TableSort ts) =>
            await Task.Run(() => {
                try {
                    waitBusyBar.Start();
                    dataTable.ShowSort(ts);
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { waitBusyBar.Stop(); }
                return true;
            });
        private bool SortEnable(TableSort ts) =>
            !dataTable.IsEmpty && (dataTable.SortDType != ts);
        #endregion

        #region Select folder
        private async Task<bool> SelectFolder(Global.DirectoryPlace place) =>
            await Task.Run(() => {
                try {
                    waitBusyBar.Start();
                    dataTable.ShowFolder(place);
                    SetTitle();
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { waitBusyBar.Stop(); }
                return true;
            });
        private bool FolderEnable(Global.DirectoryPlace place) =>
            !dataTable.IsEmpty && (dataTable.FolderType != place);
        #endregion

        #region Move to folder
        private async Task<bool> MoveToFolder(Global.DirectoryPlace place) =>
            await Task.Run(() => {
                try {
                    waitBusyBar.Start();

                    SelectorType st = MessagesSelector();
                    if (st == SelectorType.None)
                        return false;
                    else if (st == SelectorType.SingleSelect)
                        Application.MainLoop.Invoke(() => folderText.Text = place.ToString());

                    dataTable.MoveToFolder(place);
                    dataTable.ShowFolder();
                    SetTitle();
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { waitBusyBar.Stop(); }
                return true;
            });
        private bool FolderMoveEnable(Global.DirectoryPlace place) =>
            !dataTable.IsEmpty && (dataTable.FolderType != place) &&
            ((IsMultiSelect && (tableView.MultiSelectedRegions.Count > 0)) || (!IsMultiSelect && runOnce.IsValidId()));
        #endregion

        #region Export messages
        private async Task<bool> ExportMessages(ExportType t, string path) =>
            await Task.Run(async () => {
                try {
                    waitBusyBar.Start();

                    SelectorType st = MessagesSelector();
                    if (st == SelectorType.None)
                        return false;

                    _ = await dataTable.ExportMessages(t, path).ConfigureAwait(false);
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { waitBusyBar.Stop(); }
                return true;
            });
        #endregion

        #region Combine messages
        private async Task<bool> CombineMessages() =>
            await Task.Run(async () => {
                if (!tableView.MultiSelect)
                    return false;
                try {
                    waitBusyBar.Start();

                    SelectorType st = MessagesSelector(false);
                    if (st != SelectorType.MultiSelect)
                        return false;

                    _ = await dataTable.CombineMessages().ConfigureAwait(false);
                    dataTable.ShowFolder();
                    SetTitle();
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { waitBusyBar.Stop(); }
                return true;
            });
        private bool MessagesCombineEnable() =>
            !dataTable.IsEmpty && IsMultiSelect && (tableView.MultiSelectedRegions.Count > 0);
        #endregion

        #region Delete messages
        private async Task<bool> DeleteMessages() =>
            await Task.Run(async () => {
                try {
                    waitBusyBar.Start();

                    SelectorType st = MessagesSelector();
                    if (st == SelectorType.None)
                        return false;

                    _ = await dataTable.SafeDelete().ConfigureAwait(false);
                    dataTable.ShowFolder();
                    SetTitle();
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { waitBusyBar.Stop(); }
                return true;
            });
        #endregion

        #region Set read messages
        private async Task<bool> SetReadMessages(bool b) =>
            await Task.Run(async () => {
                try {
                    waitBusyBar.Start();

                    SelectorType st = MessagesSelector();
                    if (st == SelectorType.None)
                        return false;
                    else if (st == SelectorType.SingleSelect)
                        Application.MainLoop.Invoke(() => {
                            readingBox.Checked = b;
                            readingBox.SetChildNeedsDisplay();
                        });
                    _ = await dataTable.SetReadMessages(b).ConfigureAwait(false);
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { waitBusyBar.Stop(); }
                return true;
            });
        #endregion

        private bool MessagesOptionsEnable() =>
            !dataTable.IsEmpty && ((tableView.MultiSelectedRegions.Count > 0) || runOnce.IsValidId());

        private bool MessagesOptionsOneEnable() =>
            !dataTable.IsEmpty && !IsMultiSelect && (tableView.MultiSelectedRegions.Count == 0);

        #region Messages selector
        private SelectorType MessagesSelector(bool b = true) {
            if (tableView.MultiSelect) {
                if (!dataTable.Multiselected.Add(tableView.MultiSelectedRegions))
                    return SelectorType.None;
                IsMultiSelect = false;
                return SelectorType.MultiSelect;
            }
            else if (b && runOnce.IsValidId()) {
                dataTable.Multiselected.Add(runOnce.Id);
                return SelectorType.SingleSelect;
            }
            return SelectorType.None;
        }
        #endregion

        #region LocalLauncher
        private void LocalLauncher<T>() {
            Type type = typeof(T);
            tableView.KeyUp -= TableView_KeyUp;
            GuiApp.Get.LoadWindow(type, selectedPath);
            if (type == typeof(GuiMessageReadWindow))
                ReadingBox_Toggled(true);
            tableView.KeyUp += TableView_KeyUp;
        }
        #endregion
    }
}

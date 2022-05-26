﻿/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HomeMailHub.Gui.ListSources;
using NStack;
using SecyrityMail.Messages;
using SecyrityMail.Utils;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiMessagesListWindow : Window, IGuiWindow<GuiMessagesListWindow>
    {
        private Toplevel GuiToplevel { get; set; } = default;
        private MenuBar GuiMenu { get; set; } = default;
        private MenuBarItem undeleteMenu { get; set; } = default;
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

        private FrameView frameMsg { get; set; } = default;
        private RadioGroup sortTitle { get; set; } = default;
        private CheckBox readingBox { get; set; } = default;
        private ProgressBar waitLoadProgress { get; set; } = default;

        private string selectedName { get; set; } = string.Empty;
        private string selectedPath { get; set; } = string.Empty;
        private MessagesDataTable dataTable { get; set; } = default;
        private Timer systemTimer { get; set; } = default;
        private GuiLinearLayot linearLayot { get; } = new();

        public Toplevel GetTop => GuiToplevel;

        public GuiMessagesListWindow() : base(RES.GUIMESSAGE_TITLE1, 0)
        {
            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill() - 1;
            GuiToplevel = GuiExtensions.CreteTop();
            systemTimer = new Timer((a) => {
                Application.MainLoop?.Invoke(() => waitLoadProgress.Pulse());
            }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

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

            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Init
        public GuiMessagesListWindow Init(string s)
        {
            int idx = 0;
            selectedName = s;
            List<GuiLinearData> layout = linearLayot.GetDefault();

            frameMsg = new FrameView(new Rect(0, 0, 116, 8), RES.TAG_MESSAGE)
            {
                X = 1,
                Y = 18
            };
            tableView = new TableView()
            {
                X = 1,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 8,
                FullRowSelect = true,
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
            dataTable = new MessagesDataTable(selectedName, tableView);
            Add(tableView);

            waitLoadProgress = new ProgressBar()
            {
                X = 6,
                Y = 1,
                Width = 93,
                Height = 1,
                Visible = false,
                ColorScheme = Colors.Base
            };
            Add(waitLoadProgress);

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
                Width = 15,
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
                Width = 15,
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
                Width = 10,
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
            buttonClose.Clicked += () => {
                CloseDialog();
                Application.RequestStop();
            };
            buttonDelete.Clicked += () => {
                string s = msgIdText.Text.ToString();
                if (dataTable.IsEmpty || string.IsNullOrEmpty(s))
                    return;
                DeleteDialog(s);
            };
            buttonReply.Clicked += () => {
                if (dataTable.IsEmpty || string.IsNullOrEmpty(selectedPath))
                    return;
                GuiApp.Get.LoadWindow(typeof(GuiMessageWriteWindow), selectedPath);
            };
            buttonOpen.Clicked += () => {
                if (dataTable.IsEmpty || string.IsNullOrEmpty(selectedPath))
                    return;
                GuiApp.Get.LoadWindow(typeof(GuiMessageReadWindow), selectedPath);
                SetMessageReading();
            };
            sortTitle.SelectedItemChanged += SortTitle_SelectedItemChanged;
            readingBox.Toggled += ReadingBox_Toggled;
            Add(frameMsg);

            undeleteMenu = new MenuBarItem(RES.MENU_DELETEMENU, new MenuItem[] {
                new MenuItem (
                    RES.MENU_DELETEALL, "", () => DeleteAllDialog(),
                    () => !dataTable.IsEmpty)
            });

            GuiMenu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
                    new MenuItem (RES.MENU_RELOAD, "", async () => {
                        dataTable.ScrollToStart();
                        _ = await Load_().ConfigureAwait(false);
                    }, null, null, Key.AltMask | Key.R),
                    new MenuItem (RES.MENU_MSGSREADALL, "", () => {
                        dataTable.SetReadAllMessage();
                    }, null, null, Key.AltMask | Key.A),
                    null,
                    new MenuItem (RES.MENU_CLOSE, "", () => {
                        CloseDialog();
                        Application.RequestStop();
                    }, null, null, Key.AltMask | Key.Q)
                }),
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
                undeleteMenu
            });

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
                    WaitStart();
                    _ = await dataTable.LoadMessages();
                    base.Title = string.Format(RES.GUIMESSAGE_FMT3, selectedName, dataTable.Count);
                } catch (Exception ex) { ex.StatusBarError(); }
                finally { WaitStop(); }
                return true;
            });
        #endregion

        private void WaitStart() {
            Application.MainLoop?.Invoke(() => waitLoadProgress.Visible = true);
            systemTimer.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(150));
        }
        private void WaitStop() {
            systemTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            Application.MainLoop?.Invoke(() => waitLoadProgress.Visible = false);
        }

        private void DataClear() {
            Clean();
            base.Title = string.Format(RES.GUIMESSAGE_FMT3, selectedName, 0);
        }
        private void Clean() {
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
        }

        private int __lastId = -1;
        private void Update(MailMessage msg, int id) {

            if (id == __lastId)
                return;
            __lastId = id;

            idText.Text = msg.Id.ToString();
            msgIdText.Text = msg.MsgId;
            fromText.Text = msg.From;
            folderText.Text = msg.Folder.ToString();
            sizeText.Text = msg.Size.Humanize();
            subjText.Text = msg.Subj;
            dateText.Text = msg.Date.ToString("dddd, dd MMMM yyyy");
            readingBox.Checked = msg.IsRead;
            selectedPath = msg.FilePath;
        }

        private void TableView_CellActivated(TableView.CellActivatedEventArgs obj) {
            if (dataTable.IsEmpty)
                return;
            if ((obj.Row >= 0) && (obj.Row < dataTable.Count)) {
                MailMessage msg = dataTable.Get(obj.Row);
                if (msg == null) return;
                Update(msg, obj.Row);
                if (!string.IsNullOrEmpty(selectedPath)) {
                    GuiApp.Get.LoadWindow(typeof(GuiMessageReadWindow), selectedPath);
                    SetMessageReading();
                }
            }
        }

        private void TableView_SelectedCellChanged(TableView.SelectedCellChangedEventArgs obj) {
            if (dataTable.IsEmpty)
                return;
            if ((obj.NewRow >= 0) && (obj.NewRow < dataTable.Count)) {
                MailMessage msg = dataTable.Get(obj.NewRow);
                if (msg == null) return;
                Update(msg, obj.NewRow);
            }
        }

        private void TableView_KeyUp(KeyEventEventArgs obj) {
            if ((obj != null) && (obj.KeyEvent.Key == Key.Enter) && !string.IsNullOrEmpty(selectedPath)) {
                System.Diagnostics.Debug.WriteLine(obj.KeyEvent.Key);
                GuiApp.Get.LoadWindow(typeof(GuiMessageReadWindow), selectedPath);
                SetMessageReading();
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

        private void ReadingBox_Toggled(bool b) {
            if (__lastId < 0) return;
            dataTable.SetReadMessage(__lastId, !b);
        }

        private void SetMessageReading() {
            if (__lastId < 0) return;
            readingBox.Checked = true;
            readingBox.SetChildNeedsDisplay();
            ReadingBox_Toggled(false);
        }

        private async void CloseDialog() {
            try {
                if (dataTable.Deleted > 0) {
                    if (Properties.Settings.Default.IsConfirmRestoreMessages) {
                        if (MessageBox.Query(50, 7,
                            string.Format(RES.GUIMESSAGE_FMT3, RES.TAG_DELETE, dataTable.Deleted),
                            string.Format(RES.GUIMESSAGE_FMT4, dataTable.Deleted), RES.TAG_NO, RES.TAG_YES) == 1)
                            _ = await dataTable.UnDeleted().ConfigureAwait(false);
                        else
                            _ = await dataTable.ClearDeleted().ConfigureAwait(false);
                    } else
                        _ = await dataTable.ClearDeleted().ConfigureAwait(false);
                }
            } catch { }
        }

        private async void DeleteDialog(string id) {
            try {
                if (Properties.Settings.Default.IsConfirmDeleteMessages) {
                    if (MessageBox.Query(50, 7,
                        string.Format(RES.GUIMESSAGE_FMT1, RES.TAG_DELETE),
                        string.Format(RES.GUIMESSAGE_FMT2, RES.TAG_DELETE), RES.TAG_YES, RES.TAG_NO) == 0) {
                        Clean();
                        _ = await dataTable.SafeDelete(id).ConfigureAwait(false);
                        if (dataTable.Deleted > 0) DeleteMenu();
                        base.Title = string.Format(RES.GUIMESSAGE_FMT3, selectedName, dataTable.Count);
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
                    base.Title = string.Format(RES.GUIMESSAGE_FMT3, selectedName, 0);
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
                                WaitStart();
                                _ = await dataTable.UnDeleted().ConfigureAwait(false);
                                DeleteMenu();
                                Application.MainLoop.Invoke(() =>
                                    base.Title = string.Format(RES.GUIMESSAGE_FMT3, selectedName, dataTable.Count));
                            } finally { WaitStop(); }
                        });
                    items[3] = new MenuItem(
                        string.Format(RES.MENU_FMT_DELETECLEAR, dataTable.Deleted), "",
                        async () => _ = await dataTable.ClearDeleted().ConfigureAwait(false));
                }
                Application.MainLoop.Invoke(() => undeleteMenu.Children = items);
            } catch { }
        }

        private async Task<bool> SortDialog(TableSort ts) =>
            await Task.Run(() => {
                try {
                    WaitStart();
                    switch (ts) {
                        case TableSort.SortUp:   dataTable.SortUp();   break;
                        case TableSort.SortDown: dataTable.SortDown(); break;
                        case TableSort.SortSubj: dataTable.SortSubj(); break;
                        case TableSort.SortDate: dataTable.SortDate(); break;
                        case TableSort.SortFrom: dataTable.SortFrom(); break;
                    }
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { WaitStop(); }
                return true;
            });
        private bool SortEnable(TableSort ts) =>
            !dataTable.IsEmpty && (dataTable.SortDirection != ts);
    }
}
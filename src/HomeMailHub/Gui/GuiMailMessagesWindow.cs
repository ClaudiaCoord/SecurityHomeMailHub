
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NStack;
using SecyrityMail;
using SecyrityMail.Messages;
using SecyrityMail.Utils;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiMailMessagesWindow : Window, IGuiWindow<GuiMailMessagesWindow>
    {
        private Toplevel GuiToplevel { get; set; } = default;
        private MenuBar GuiMenu { get; set; } = default;
        private ListView listView { get; set; } = default;

        private Button buttonClose { get; set; } = default;
        private Button buttonDelete { get; set; } = default;
        private Button buttonReply { get; set; } = default;
        private Button buttonOpen { get; set; } = default;

        private Label idLabel { get; set; } = default;
        private Label msgIdLabel { get; set; } = default;
        private Label folderLabel { get; set; } = default;
        private Label sizeLabel { get; set; } = default;
        private Label subjLabel { get; set; } = default;
        private Label dateLabel { get; set; } = default;

        private Label idText { get; set; } = default;
        private Label msgIdText { get; set; } = default;
        private Label folderText { get; set; } = default;
        private Label sizeText { get; set; } = default;
        private Label subjText { get; set; } = default;
        private Label dateText { get; set; } = default;

        private FrameView frameList { get; set; } = default;
        private FrameView frameMsg { get; set; } = default;
        private RadioGroup sortTitle { get; set; } = default;
        private ProgressBar waitLoadProgress { get; set; } = default;

        private bool isSortDirections { get; set; } = true;
        private string selectedName { get; set; } = string.Empty;
        private string selectedPath { get; set; } = string.Empty;
        private List<string> data = new();
        private MailMessages msgs { get; set; } = default;
        private Timer systemTimer { get; set; } = default;

        public Toplevel GetTop => GuiToplevel;

        public GuiMailMessagesWindow() : base(RES.GUIMESSAGE_TITLE1, 0)
        {
            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill() - 1;
            GuiToplevel = GuiExtensions.CreteTop();
            systemTimer = new Timer((a) => {
                Application.MainLoop?.Invoke(() => waitLoadProgress.Pulse());
            }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        ~GuiMailMessagesWindow() => Dispose();

        public new void Dispose() {

            msgs = default;
            Global.Instance.MessagesManager.Close(selectedName);
            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Init
        public GuiMailMessagesWindow Init(string s)
        {
            selectedName = s;

            frameList = new FrameView(new Rect(0, 0, 116, 17), $"{s} - {RES.TAG_MESSAGE}")
            {
                X = 1,
                Y = 1
            };
            frameMsg = new FrameView(new Rect(0, 0, 116, 8), RES.TAG_MESSAGE)
            {
                X = 1,
                Y = 18
            };
            frameList.Add(sortTitle = new RadioGroup(new ustring[] { $" {(char)0x2191}", $"{(char)0x2193} " })
            {
                X = 106,
                Y = 0,
                AutoSize = true,
                NoSymbol = true,
                DisplayMode = DisplayModeLayout.Horizontal,
                SelectedItem = 1
            });
            frameList.Add(waitLoadProgress = new ProgressBar()
            {
                X = 1,
                Y = 0,
                Width = 32,
                Height = 1,
                Visible = false,
                ColorScheme = Colors.Base
            });
            listView = new ListView(data)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 4,
                Height = Dim.Fill() - 1,
                AllowsMarking = true,
                AllowsMultipleSelection = false
            };
            listView.OpenSelectedItem += ListView_OpenSelectedItem;
            listView.SelectedItemChanged += ListView_SelectedItemChanged;
            sortTitle.SelectedItemChanged += SortTitle_SelectedItemChanged;

            frameList.Add(listView);
            Add(frameList);

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
                Height = 1
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
                Height = 1
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
                Height = 1
            });
            frameMsg.Add(folderLabel = new Label(RES.TAG_FOLDER)
            {
                X = 1,
                Y = 2,
                AutoSize = true
            });
            frameMsg.Add(folderText = new Label(string.Empty)
            {
                X = 11,
                Y = 2,
                Width = 10,
                Height = 1
            });
            frameMsg.Add(sizeLabel = new Label(RES.TAG_SIZE)
            {
                X = 78,
                Y = 2,
                AutoSize = true
            });
            frameMsg.Add(sizeText = new Label(string.Empty)
            {
                X = 86,
                Y = 2,
                Width = 10,
                Height = 1
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
                AutoSize = true
            });
            frameMsg.Add(buttonClose = new Button(10, 19, RES.BTN_CLOSE)
            {
                X = 73,
                Y = 4,
                AutoSize = true
            });
            frameMsg.Add(buttonDelete = new Button(10, 19, RES.BTN_DELETE)
            {
                X = 83,
                Y = 4,
                AutoSize = true
            });
            frameMsg.Add(buttonReply = new Button(10, 19, RES.BTN_REPLAY)
            {
                X = 94,
                Y = 4,
                AutoSize = true
            });
            frameMsg.Add(buttonOpen = new Button(10, 19, RES.BTN_OPEN)
            {
                X = 104,
                Y = 4,
                AutoSize = true
            });
            buttonClose.Clicked += () => {
                Application.RequestStop();
            };
            buttonDelete.Clicked += async () => {
                string s = msgIdText.Text.ToString();
                if ((msgs == null) || string.IsNullOrEmpty(s))
                    return;
                if (MessageBox.Query(50, 7,
                    string.Format(RES.GUIMESSAGE_FMT1, RES.TAG_DELETE),
                    string.Format(RES.GUIMESSAGE_FMT2, RES.TAG_DELETE), RES.TAG_YES, RES.TAG_NO) == 0) {
                    try {
                        _ = await msgs.DeleteMessage(s).ConfigureAwait(false);
                        _ = await Load_().ConfigureAwait(false);
                    } catch (Exception ex) { ex.StatusBarError(); }
                }
            };
            buttonReply.Clicked += () => {
                if ((msgs == null) || string.IsNullOrEmpty(selectedPath))
                    return;
                GuiApp.Get.LoadWindow(typeof(GuiWriteMessageWindow), selectedPath);
            };
            buttonOpen.Clicked += () => {
                if ((msgs == null) || string.IsNullOrEmpty(selectedPath))
                    return;
                GuiApp.Get.LoadWindow(typeof(GuiReadMessageWindow), selectedPath);
            };

            Add(frameMsg);

            GuiMenu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
                    new MenuItem (RES.MENU_RELOAD, "", async () => {
                        _ = await Load_().ConfigureAwait(false);
                    }, null, null, Key.AltMask | Key.R),
                    null,
                    new MenuItem (RES.MENU_CLOSE, "", () => Application.RequestStop(), null, null, Key.AltMask | Key.Q)
                })
            });

            GuiToplevel.Add(GuiMenu, this);
            return this;
        }
        #endregion

        public async void Load() => _ = await Load_().ConfigureAwait(false);

        private async Task<bool> Load_() =>
            await Task.Run(async () => {
                WaitStart();
                DataClear();
                try {
                    if (msgs != default)
                        Global.Instance.MessagesManager.Close(selectedName);
                    msgs = await Global.Instance.MessagesManager.Open(selectedName)
                                                                .ConfigureAwait(false);
                    if (msgs == null)
                        return false;

                    data = (from i in msgs.Items
                            select $"{i.Id}) {i.Subj}").ToList();
                    data.Reverse();
                    await listView.SetSourceAsync(data).ConfigureAwait(false);
                    frameList.Title = string.Format(RES.GUIMESSAGE_FMT3, selectedName, data.Count);
                    Clean();
                } catch (Exception ex) { ex.StatusBarError(); }
                finally { WaitStop(); }
                return true;
            });

        private void WaitStart() {
            Application.MainLoop?.Invoke(() => waitLoadProgress.Visible = true);
            systemTimer.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(150));
        }
        private void WaitStop() {
            systemTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            Application.MainLoop?.Invoke(() => waitLoadProgress.Visible = false);
        }

        private void DataClear() {
            data.Clear();
            Clean();
            frameList.Title = string.Format(RES.GUIMESSAGE_FMT3, selectedName, 0);
        }
        private void Clean() {
            idText.Text =
            msgIdText.Text =
            folderText.Text =
            sizeText.Text =
            subjText.Text =
            dateText.Text =
            selectedPath = string.Empty;
        }

        private int __last = -1;
        private void Update(MailMessage msg, int id) {

            if (id == __last)
                return;
            __last = id;

            idText.Text = msg.Id.ToString();
            msgIdText.Text = msg.MsgId;
            folderText.Text = msg.Folder.ToString();
            sizeText.Text = msg.Size.Humanize();
            subjText.Text = msg.Subj;
            dateText.Text = msg.Date.ToString("dddd, dd MMMM yyyy");
            selectedPath = msg.FilePath;
        }


        private async void SortTitle_SelectedItemChanged(SelectedItemChangedArgs obj) {
            if ((obj == null) || (obj.SelectedItem < 0))
                return;
            bool b = obj.SelectedItem > 0;
            if (b == isSortDirections)
                return;
            isSortDirections = b;

            data.Reverse();
            await listView.SetSourceAsync(data).ConfigureAwait(false);
        }

        private void ListView_SelectedItemChanged(ListViewItemEventArgs obj) => SelectedItem(obj);
        private void ListView_OpenSelectedItem(ListViewItemEventArgs obj) {
            if (msgs == null)
                return;
            if ((obj.Item >= 0) && (obj.Item < msgs.Count)) {
                selectedPath = msgs.Items[ReverseIndex(obj.Item)].FilePath;
                if (!string.IsNullOrEmpty(selectedPath))
                    GuiApp.Get.LoadWindow(typeof(GuiReadMessageWindow), selectedPath);
            }
        }

        private void SelectedItem(ListViewItemEventArgs obj) {
            if ((obj == null) || (msgs == null))
                return;
            if ((obj.Item >= 0) && (obj.Item < msgs.Count))
                Update(msgs.Items[ReverseIndex(obj.Item)], obj.Item);
        }

        private int ReverseIndex(int i) => isSortDirections ? msgs.Count - i - 1 : i;
    }
}

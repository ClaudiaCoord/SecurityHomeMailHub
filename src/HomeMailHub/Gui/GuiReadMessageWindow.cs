
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SecyrityMail.Utils;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiReadMessageWindow : Window, IGuiWindow<GuiReadMessageWindow>
    {
        private Toplevel GuiToplevel { get; set; } = default;
        private MenuBar GuiMenu { get; set; } = default;

        private Button buttonClose { get; set; } = default;
        private Button buttonOpen { get; set; } = default;

        private Label msgIdLabel { get; set; } = default;
        private Label fromLabel { get; set; } = default;
        private Label sizeLabel { get; set; } = default;
        private Label subjLabel { get; set; } = default;
        private Label dateLabel { get; set; } = default;
        private Label attachLabel { get; set; } = default;

        private Label msgIdText { get; set; } = default;
        private Label fromText { get; set; } = default;
        private Label sizeText { get; set; } = default;
        private Label subjText { get; set; } = default;
        private Label dateText { get; set; } = default;
        private Label attachText { get; set; } = default;

        private FrameView frameHeader { get; set; } = default;
        private FrameView frameMsg { get; set; } = default;
        private TextView msgText { get; set; } = default;

        private bool IsViewSource { get; set; } = false;
        private string [] messageBody = new string[2] { string.Empty , string.Empty };
        private string selectedPath { get; set; } = string.Empty;

        public Toplevel GetTop => GuiToplevel;

        public GuiReadMessageWindow() : base(RES.GUIMAILREAD_TITLE1, 0)
        {
            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill() - 1;
            GuiToplevel = GuiExtensions.CreteTop();
        }
        ~GuiReadMessageWindow() => Dispose();

        public new void Dispose() {

            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Init
        public GuiReadMessageWindow Init(string s)
        {
            selectedPath = s;

            frameHeader = new FrameView(new Rect(0, 0, 116, 7), "+")
            {
                X = 1,
                Y = 1
            };
            frameMsg = new FrameView(new Rect(0, 0, 116, 18), RES.TAG_MESSAGE)
            {
                X = 1,
                Y = 8
            };
            frameHeader.Add(msgIdLabel = new Label("MsgId: ")
            {
                X = 1,
                Y = 1,
                AutoSize = true
            });
            frameHeader.Add(msgIdText = new Label(string.Empty)
            {
                X = 11,
                Y = 1,
                Width = 15,
                Height = 1
            });
            frameHeader.Add(dateLabel = new Label(RES.TAG_DATE)
            {
                X = 78,
                Y = 1,
                AutoSize = true
            });
            frameHeader.Add(dateText = new Label(string.Empty)
            {
                X = 84,
                Y = 1,
                Width = 15,
                Height = 1
            });
            frameHeader.Add(fromLabel = new Label(RES.TAG_FROM)
            {
                X = 1,
                Y = 2,
                AutoSize = true
            });
            frameHeader.Add(fromText = new Label(string.Empty)
            {
                X = 11,
                Y = 2,
                Width = 10,
                Height = 1
            });
            frameHeader.Add(sizeLabel = new Label(RES.TAG_SIZE)
            {
                X = 78,
                Y = 2,
                AutoSize = true
            });
            frameHeader.Add(sizeText = new Label(string.Empty)
            {
                X = 86,
                Y = 2,
                Width = 10,
                Height = 1
            });
            frameHeader.Add(subjLabel = new Label(RES.TAG_SUBJECT)
            {
                X = 1,
                Y = 3,
                AutoSize = true
            });
            frameHeader.Add(subjText = new Label(string.Empty)
            {
                X = 11,
                Y = 3,
                AutoSize = true
            });
            frameHeader.Add(attachLabel = new Label(RES.TAG_ATTACH)
            {
                X = 1,
                Y = 4,
                AutoSize = true,
                Visible = false
            });
            frameHeader.Add(attachText = new Label(string.Empty)
            {
                X = 11,
                Y = 4,
                AutoSize = true,
                Visible = false
            });
            frameHeader.Add(buttonClose = new Button(10, 19, RES.BTN_CLOSE)
            {
                X = 90,
                Y = 4,
                AutoSize = true
            });
            frameHeader.Add(buttonOpen = new Button(10, 19, RES.TAG_SOURCE)
            {
                X = 100,
                Y = 4,
                AutoSize = true
            });
            buttonClose.Clicked += () => {
                Application.RequestStop();
            };
            buttonOpen.Clicked += () => {
                if (string.IsNullOrEmpty(selectedPath))
                    return;
                IsViewSource = !IsViewSource;
                if (IsViewSource) {
                    msgText.Text = messageBody[1];
                    buttonOpen.Text = RES.TAG_BODY;
                } else {
                    msgText.Text = messageBody[0];
                    buttonOpen.Text = RES.TAG_SOURCE;
                }
            };
            Add(frameHeader);

            msgText = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Multiline = true,
                ReadOnly = true
            };
            frameMsg.Add(msgText);
            Add(frameMsg);

            GuiMenu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
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
                try {
                    ReadMessageData rdata = await selectedPath.ReadMessage().ConfigureAwait(false);
                    if (rdata.IsEmpty) {
                        Application.RequestStop();
                        return false;
                    }
                    MimeKit.MimeMessage mmsg = rdata.Message;
                    messageBody[1] = File.ReadAllText(rdata.Info.FullName);
                    messageBody[0] =
                        (mmsg.TextBody != null) ?
                            mmsg.TextBody : ((mmsg.HtmlBody != null) ? mmsg.HtmlBody : mmsg.ToString());
                    msgText.Text = messageBody[0];
                    subjText.Text = mmsg.Subject;
                    dateText.Text = mmsg.Date.ToString("dddd, dd MMMM yyyy");
                    sizeText.Text = rdata.Info.Length.Humanize();
                    msgIdText.Text = mmsg.MessageId;
                    fromText.Text = mmsg.From.ToString();
                    frameHeader.Title = $"{RES.TAG_TO} {mmsg.To}";

                    try {
                        if ((mmsg.Attachments != null) && (mmsg.Attachments.Count() > 0)) {

                            List<string> list = new();
                            foreach (MimeKit.MimeEntity a in mmsg.Attachments) {

                                if (a is MimeKit.MessagePart mep) {
                                    list.Add(Path.GetFileName(mep.ContentDisposition?.FileName));
                                }
                                else if (a is MimeKit.MimePart mip) {
                                    list.Add(Path.GetFileName(mip.FileName));
                                }
                            }
                            if (list.Count > 0) {
                                attachLabel.Visible = attachText.Visible = true;
                                attachText.Text = string.Join(", ", list);
                            }
                        }
                    } catch { }
                } catch (Exception ex) { ex.StatusBarError(); }
                return true;
            });
    }
}

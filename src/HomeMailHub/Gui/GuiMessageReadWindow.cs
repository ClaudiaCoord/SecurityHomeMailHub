
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MimeKit;
using SecyrityMail;
using SecyrityMail.Messages;
using SecyrityMail.Utils;
using Terminal.Gui;
using GuiAttribute = Terminal.Gui.Attribute;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiMessageReadWindow : Window, IGuiWindow<GuiMessageReadWindow>
    {
        private Toplevel GuiToplevel { get; set; } = default;
        private MenuBar GuiMenu { get; set; } = default;
        private MenuBarItem attachMenu { get; set; } = default;
        private MenuBarItem ccMenu { get; set; } = default;
        private MenuBarItem bccMenu { get; set; } = default;
        private MenuItem[] attachItemsMenu { get; set; } = default;

        private Button buttonClose { get; set; } = default;
        private Button buttonSource { get; set; } = default;

        private Label msgIdLabel { get; set; } = default;
        private Label fromLabel { get; set; } = default;
        private Label sizeLabel { get; set; } = default;
        private Label subjLabel { get; set; } = default;
        private Label dateLabel { get; set; } = default;
        private Label attachLabel { get; set; } = default;
        private Label ccLabel { get; set; } = default;
        private Label bccLabel { get; set; } = default;
        private Label textLabel { get; set; } = default;
        private Label htmlLabel { get; set; } = default;

        private Label msgIdText { get; set; } = default;
        private Label fromText { get; set; } = default;
        private Label sizeText { get; set; } = default;
        private Label subjText { get; set; } = default;
        private Label dateText { get; set; } = default;
        private Label attachText { get; set; } = default;

        private FrameView frameHeader { get; set; } = default;
        private FrameView frameMsg { get; set; } = default;
        private TextView msgText { get; set; } = default;
        private ColorScheme colorInfo { get; set; } = default;
        private ColorScheme colorEnable { get; set; } = default;
        private ColorScheme colorDisable { get; set; } = default;

        private bool IsViewSource { get; set; } = false;
        private string[] messageBody = new string[2] { string.Empty, string.Empty };
        private string selectedPath { get; set; } = string.Empty;
        private GuiLinearLayot linearLayot { get; } = new();

        public Toplevel GetTop => GuiToplevel;

        public GuiMessageReadWindow() : base(RES.GUIMAILREAD_TITLE1, 0)
        {
            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill() - 1;
            GuiToplevel = GuiExtensions.CreteTop();

            linearLayot.Add("en", new List<GuiLinearData> {
                new GuiLinearData(64, 4, true),
                new GuiLinearData(69, 4, true),
                new GuiLinearData(75, 4, true),
                new GuiLinearData(82, 4, true),
                new GuiLinearData(93, 4, true),
                new GuiLinearData(104, 4, true)
            });
            linearLayot.Add("ru", new List<GuiLinearData> {
                new GuiLinearData(64, 4, true),
                new GuiLinearData(69, 4, true),
                new GuiLinearData(75, 4, true),
                new GuiLinearData(82, 4, true),
                new GuiLinearData(89, 4, true),
                new GuiLinearData(102, 4, true)
            });

            GuiAttribute cinf = Application.Driver.MakeAttribute(Color.BrightYellow, Color.BrightBlue);
            GuiAttribute cnen = Application.Driver.MakeAttribute(Color.White, Color.BrightBlue);
            GuiAttribute cndis = Application.Driver.MakeAttribute(Color.Gray, Color.DarkGray);
            colorInfo = new ColorScheme() { Normal = cinf, Focus = cinf, HotFocus = cinf, HotNormal = cinf, Disabled = cinf };
            colorEnable = new ColorScheme() { Normal = cnen, Focus = cnen, HotFocus = cnen, HotNormal = cnen, Disabled = cnen };
            colorDisable = new ColorScheme() { Normal = cndis, Focus = cndis, HotFocus = cndis, HotNormal = cndis, Disabled = cndis };
        }
        ~GuiMessageReadWindow() => Dispose();

        public new void Dispose()
        {

            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Init
        public GuiMessageReadWindow Init(string s)
        {
            selectedPath = s;
            List<GuiLinearData> layout = linearLayot.GetDefault();

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
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
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
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
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
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
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
                Height = 1,
                ColorScheme = GuiApp.ColorDescription
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
                AutoSize = true,
                ColorScheme = GuiApp.ColorDescription
            });
            frameHeader.Add(attachLabel = new Label($"{RES.TAG_ATTACH}:")
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
                Visible = false,
                ColorScheme = GuiApp.ColorDescription
            });
            frameHeader.Add(ccLabel = new Label(" CC ")
            {
                X = layout[0].X,
                Y = layout[0].Y,
                AutoSize = layout[0].AutoSize,
                Visible = false,
                ColorScheme = colorInfo
            });
            frameHeader.Add(bccLabel = new Label(" BCC ")
            {
                X = layout[1].X,
                Y = layout[1].Y,
                AutoSize = layout[1].AutoSize,
                Visible = false,
                ColorScheme = colorInfo
            });
            frameHeader.Add(textLabel = new Label(" text ")
            {
                X = layout[2].X,
                Y = layout[2].Y,
                AutoSize = layout[2].AutoSize,
                ColorScheme = colorDisable
            });
            frameHeader.Add(htmlLabel = new Label(" html ")
            {
                X = layout[3].X,
                Y = layout[3].Y,
                AutoSize = layout[3].AutoSize,
                ColorScheme = colorDisable
            });
            frameHeader.Add(buttonSource = new Button(10, 19, RES.BTN_SOURCE)
            {
                X = layout[4].X,
                Y = layout[4].Y,
                AutoSize = layout[4].AutoSize
            });
            frameHeader.Add(buttonClose = new Button(10, 19, RES.BTN_CLOSE)
            {
                X = layout[5].X,
                Y = layout[5].Y,
                AutoSize = layout[5].AutoSize
            });
            buttonClose.Clicked += () => Application.RequestStop();
            buttonSource.Clicked += () =>
            {
                if (string.IsNullOrEmpty(selectedPath))
                    return;
                IsViewSource = !IsViewSource;
                if (IsViewSource)
                {
                    msgText.Text = messageBody[1];
                    buttonSource.Text = RES.TAG_BODY;
                }
                else
                {
                    msgText.Text = messageBody[0];
                    buttonSource.Text = RES.BTN_SOURCE;
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

            attachMenu = new MenuBarItem($"_{RES.TAG_ATTACH}", new MenuItem[0]);
            ccMenu = new MenuBarItem("_Cc", new MenuItem[0]);
            bccMenu = new MenuBarItem("_Bcc", new MenuItem[0]);
            GuiMenu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
                    new MenuItem (RES.MENU_CLOSE, "", () => Application.RequestStop(), null, null, Key.AltMask | Key.Q)
                }),
                attachMenu,
                ccMenu,
                bccMenu
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
                    if (rdata.IsEmpty)
                    {
                        Application.RequestStop();
                        return false;
                    }
                    MimeMessage mmsg = rdata.Message;
                    bool[] b = new bool[] {
                        !string.IsNullOrWhiteSpace(mmsg.TextBody),
                        !string.IsNullOrWhiteSpace(mmsg.HtmlBody)
                    };
                    if (b[0])
                        messageBody[0] = mmsg.TextBody;
                    else if (b[1])
                        messageBody[0] = new ConverterHtmlToHtml().ConvertT(mmsg.HtmlBody);
                    else
                        messageBody[0] = mmsg.ToString();

                    Application.MainLoop.Invoke(() => {

                        subjText.Text = mmsg.Subject;
                        dateText.Text = mmsg.Date.ToString("dddd, dd MMMM yyyy");
                        sizeText.Text = rdata.Info.Length.Humanize();
                        msgIdText.Text = mmsg.MessageId;
                        fromText.Text = mmsg.From.ToString();
                        frameHeader.Title = $"{RES.TAG_TO} {mmsg.To}";

                        textLabel.ColorScheme = b[0] ? colorEnable : colorDisable;
                        htmlLabel.ColorScheme = b[1] ? colorEnable : colorDisable;
                        textLabel.Redraw(textLabel.Bounds);
                        htmlLabel.Redraw(htmlLabel.Bounds);

                        msgText.Text = messageBody[0];
                        buttonClose.SetFocus();
                    });
                    try {
                        List<MailboxAddress> ccList = new();
                        AddAddresses(mmsg.Cc, ccList);
                        AddAddresses(mmsg.ResentCc, ccList);
                        AddAddresses(mmsg.ResentTo, ccList);
                        AddAddresses(mmsg.To, ccList, 1);
                        UpdateAdresses(ccList, ccMenu);

                        List <MailboxAddress> bccList = new();
                        AddAddresses(mmsg.Bcc, bccList);
                        AddAddresses(mmsg.ResentBcc, bccList);
                        UpdateAdresses(bccList, bccMenu);

                        bool[] x = new[] {
                            ccList.Count > 0,
                            bccList.Count > 0
                        };
                        if (x[0] && x[1])
                            ccLabel.Visible = bccLabel.Visible = true;
                        else if (x[0]) {
                            List<GuiLinearData> layout = linearLayot.GetDefault();
                            ccLabel.X = layout[0].X + 6;
                            ccLabel.Visible = true;
                        }
                        else if (x[1]) {
                            bccLabel.Visible = true;
                        }
                    } catch { }
                    try {
                        if ((mmsg.Attachments != null) && (mmsg.Attachments.Count() > 0)) {
                            int n = 0;
                            attachItemsMenu = new MenuItem[mmsg.Attachments.Count()];
                            for (int i = 0; i < mmsg.Attachments.Count(); i++) {

                                MimeEntity a = mmsg.Attachments.ElementAt(i);
                                if (a is MessagePart mep)
                                    attachItemsMenu[n++] = new MenuItem(
                                        Path.GetFileName(mep.ContentDisposition?.FileName).Replace('_', ' '), "",
                                        () => BrowseAttachFile(selectedPath, mmsg.MessageId, mep.ContentDisposition?.FileName));
                                else if (a is MimePart mip)
                                    attachItemsMenu[n++] = new MenuItem(
                                        Path.GetFileName(mip?.FileName).Replace('_', ' '), "",
                                        () => BrowseAttachFile(selectedPath, mmsg.MessageId, mip?.FileName));
                            }
                            if (n > 0)
                                Application.MainLoop.Invoke(() => {
                                    attachLabel.Visible = attachText.Visible = true;
                                    attachText.Text = n.ToString();
                                    attachMenu.Children = attachItemsMenu;
                                });
                        }
                    } catch { }
                    messageBody[1] = File.ReadAllText(rdata.Info.FullName);
                }
                catch (Exception ex) { ex.StatusBarError(); }
                return true;
            });

        private void AddAddresses(InternetAddressList addr, List<MailboxAddress> list, int start = 0) {
            if ((addr != null) || addr.Count == 0)
                return;
            for (int i = start; i < addr.Count; i++)
                if (addr[i] is MailboxAddress ma) list.Add(ma);
        }

        private void UpdateAdresses(List<MailboxAddress> list, MenuBarItem menu)
        {
            if (list.Count > 0) {
                int n = 0;
                MenuItem[] items = new MenuItem[list.Count];
                for (int i = 0; i < list.Count; i++)
                    if (list[i] is MailboxAddress addr)
                        items[n++] = new MenuItem(addr.ToString(), "", () => { });
                if (n > 0)
                    Application.MainLoop.Invoke(() => menu.Children = items);
            }
        }

        private void BrowseAttachFile(string path, string id, string name) {
            if (string.IsNullOrWhiteSpace(name)) return;
            MailMessage.GetAttachFilePath(
                Path.GetDirectoryName(path).Replace(@$"\{Global.DirectoryPlace.Msg}\", @$"\{Global.DirectoryPlace.Attach}\"),
                id, name).BrowseFile();
        }
    }
}

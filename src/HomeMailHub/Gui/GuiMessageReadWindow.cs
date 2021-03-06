/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MimeKit;
using MimeKit.Cryptography;
using Org.BouncyCastle.Utilities;
using SecyrityMail;
using SecyrityMail.GnuPG;
using SecyrityMail.Messages;
using SecyrityMail.Utils;
using Terminal.Gui;
using static Terminal.Gui.TabView;
using GuiAttribute = Terminal.Gui.Attribute;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiMessageReadWindow : Window, IGuiWindow<GuiMessageReadWindow>
    {
        private Toplevel GuiToplevel { get; set; } = default;
        private MenuBar GuiMenu { get; set; } = default;

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
        private Label oversizeLabel { get; set; } = default;
        private Label pgpLabel { get; set; } = default;

        private Label msgIdText { get; set; } = default;
        private Label fromText { get; set; } = default;
        private Label sizeText { get; set; } = default;
        private Label subjText { get; set; } = default;
        private Label dateText { get; set; } = default;
        private Label attachText { get; set; } = default;

        private FrameView frameHeader { get; set; } = default;
        private FrameView frameMsg { get; set; } = default;
        private FrameView frameInfo { get; set; } = default;
        private TextView msgText { get; set; } = default;
        private TextView infoText { get; set; } = default;
        private ColorScheme colorEnable { get; set; } = default;
        private ColorScheme colorDisable { get; set; } = default;
        private ColorScheme colorBageRed { get; set; } = default;
        private ColorScheme colorBageInfo { get; set; } = default;
        private ColorScheme colorBageGreen { get; set; } = default;
        private ColorScheme colorBageWarning { get; set; } = default;

        private bool IsMenuOpen { get; set; } = false;
        private bool IsViewSource { get; set; } = false;
        private string[] messageBody = new string[2] { string.Empty, string.Empty };
        private string selectedPath { get; set; } = string.Empty;
        private GuiRunOnce keyOnce = new();
        private GuiLinearLayot linearLayot { get; } = new();

        public Toplevel GetTop => GuiToplevel;

        #region Commands
        Action CommandReplay = delegate { };
        Action CommandForward = delegate { };
        Action CommandOpen = delegate { };
        Action CommandSource = delegate { };
        Action CommandClose = delegate { };
        #endregion

        #region Constructor
        public GuiMessageReadWindow() : base(RES.GUIMAILREAD_TITLE1, 0)
        {
            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill() - 1;
            GuiToplevel = GuiExtensions.CreteTop();

            linearLayot.Add("en", new List<GuiLinearData> {
                new GuiLinearData(84, 2, false),
                new GuiLinearData(54, 4, true),
                new GuiLinearData(60, 4, true),
                new GuiLinearData(68, 4, true),
                new GuiLinearData(73, 4, true),
                new GuiLinearData(79, 4, true),
                new GuiLinearData(86, 4, true),
                new GuiLinearData(93, 4, true),
                new GuiLinearData(104, 4, true)
            });
            linearLayot.Add("ru", new List<GuiLinearData> {
                new GuiLinearData(86, 2, false), // sizeText
                new GuiLinearData(50, 4, true),  // PGP
                new GuiLinearData(56, 4, true),  // SIZE
                new GuiLinearData(64, 4, true),  // CC
                new GuiLinearData(69, 4, true),  // BCC
                new GuiLinearData(75, 4, true),  // TEXT
                new GuiLinearData(82, 4, true),  // HTML
                new GuiLinearData(89, 4, true),  // btn SOURCE
                new GuiLinearData(102, 4, true)  // btn CLOSE
            });

            GuiAttribute cinfo = Application.Driver.MakeAttribute(Color.BrightYellow, Color.BrightBlue);
            GuiAttribute cwarn = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Brown);
            GuiAttribute cenable = Application.Driver.MakeAttribute(Color.White, Color.BrightBlue);
            GuiAttribute cdisable = Application.Driver.MakeAttribute(Color.Gray, Color.DarkGray);
            GuiAttribute cgreen = Application.Driver.MakeAttribute(Color.White, Color.Green);
            GuiAttribute cred = Application.Driver.MakeAttribute(Color.White, Color.Red);
            colorBageInfo = new ColorScheme() { Normal = cinfo, Focus = cinfo, HotFocus = cinfo, HotNormal = cinfo, Disabled = cinfo };
            colorEnable = new ColorScheme() { Normal = cenable, Focus = cenable, HotFocus = cenable, HotNormal = cenable, Disabled = cenable };
            colorDisable = new ColorScheme() { Normal = cdisable, Focus = cdisable, HotFocus = cdisable, HotNormal = cdisable, Disabled = cdisable };
            colorBageWarning = new ColorScheme() { Normal = cwarn, Focus = cwarn, HotFocus = cwarn, HotNormal = cwarn, Disabled = cwarn };
            colorBageGreen = new ColorScheme() { Normal = cgreen, Focus = cgreen, HotFocus = cgreen, HotNormal = cgreen, Disabled = cgreen };
            colorBageRed = new ColorScheme() { Normal = cred, Focus = cred, HotFocus = cred, HotNormal = cred, Disabled = cred };

            CommandReplay = delegate {
                msgText.SetFocus();
                GuiApp.Get.LoadWindow(typeof(GuiMessageWriteWindow), selectedPath);
            };
            CommandForward = delegate {
                GuiMessageForwardsDialog dlg = new GuiMessageForwardsDialog().Load(selectedPath);
                Application.Run(dlg);
                dlg.Dispose();
            };
            CommandSource = delegate {
                if (string.IsNullOrEmpty(selectedPath))
                    return;
                IsViewSource = !IsViewSource;
                if (IsViewSource) {
                    msgText.Text = messageBody[1];
                    buttonSource.Text = RES.TAG_BODY;
                } else {
                    msgText.Text = messageBody[0];
                    buttonSource.Text = RES.BTN_SOURCE;
                }
            };
            CommandOpen = delegate { selectedPath.BrowseFile(); };
            CommandClose = delegate { Application.RequestStop(); };
        }
        #endregion

        #region Dispose
        public new void Dispose() {

            this.KeyUp -= Window_KeyUp;
            this.GetType().IDisposableObject(this);
            base.Dispose();
        }
        #endregion

        #region Init
        public GuiMessageReadWindow Init(string s)
        {
            int idx = 0;
            selectedPath = s;
            List<GuiLinearData> layout = linearLayot.GetDefault();

            #region frameHeader
            frameHeader = new FrameView("+")
            {
                X = 1,
                Y = 1,
                Width = 117,
                Height = 7,
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
                Width = 65,
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
                Width = 28,
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
                Width = 65,
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
                X = layout[idx].X,
                Y = layout[idx++].Y,
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
            frameHeader.Add(pgpLabel = new Label(" PGP ")
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                Visible = false,
                ColorScheme = colorBageRed
            });
            frameHeader.Add(oversizeLabel = new Label(" >100k ")
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                Visible = false,
                ColorScheme = colorBageWarning
            });
            frameHeader.Add(ccLabel = new Label(" CC ")
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                Visible = false,
                ColorScheme = colorBageInfo
            });
            frameHeader.Add(bccLabel = new Label(" BCC ")
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                Visible = false,
                ColorScheme = colorBageInfo
            });
            frameHeader.Add(textLabel = new Label(" text ")
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                ColorScheme = colorDisable
            });
            frameHeader.Add(htmlLabel = new Label(" html ")
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                ColorScheme = colorDisable
            });
            frameHeader.Add(buttonSource = new Button(10, 19, RES.BTN_SOURCE)
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
            });
            frameHeader.Add(buttonClose = new Button(10, 19, RES.BTN_CLOSE)
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
            });
            Add(frameHeader);
            #endregion

            #region infoText
            frameInfo = new FrameView(RES.TAG_HEADERS)
            {
                X = Pos.Right(frameHeader) + 1,
                Y = 1,
                Width = Dim.Fill() - 1,
                Height = 7
            };
            frameInfo.Add(infoText = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Multiline = true,
                ReadOnly = true,
                WordWrap = false,
                ColorScheme = GuiApp.ColorDescription
            });
            Add(frameInfo);
            #endregion

            #region frameMsg
            frameMsg = new FrameView(RES.TAG_MESSAGE)
            {
                X = 1,
                Y = 8,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill()
            };
            frameMsg.Add(msgText = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Multiline = true,
                ReadOnly = true,
                WordWrap = true
            });
            Add(frameMsg);
            #endregion

            this.KeyUp += Window_KeyUp;

            buttonClose.Clicked += CommandClose;
            buttonSource.Clicked += CommandSource;

            GuiMenu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
                    new MenuItem (RES.BTN_REPLAY, "", CommandReplay, null, null, Key.AltMask | Key.R),
                    new MenuItem (RES.MENU_MSGFORWARDS, "", CommandForward, null, null, Key.AltMask | Key.F),
                    new MenuItem (RES.MENU_OPENMSGFROM, "", CommandOpen, null, null, Key.AltMask | Key.O),
                    null,
                    new MenuItem (RES.MENU_CLOSE, "", CommandClose, null, null, Key.AltMask | Key.CursorLeft)
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
                ReadMessageData rdata = await selectedPath.ReadMessage().ConfigureAwait(false);
                if (rdata.IsEmpty) {
                    Application.RequestStop();
                    return false;
                }

                string folder = string.Empty,
                       accfolder = string.Empty;
                DateTimeOffset msgdt = default;
                Global.DirectoryPlace placed;
                MimeMessage mmsg = rdata.Message;

                List<MailboxAddress> ccList = new();
                List<MailboxAddress> bccList = new();
                List<MenuItem> attachList = new();


                try
                {
                    (placed, accfolder, msgdt) = Global.GetFolderInfo(rdata.Info.DirectoryName);
                    folder = placed.ToString();
                } catch { }

                bool iscrypt = false,
                     isdecrypt = false;

                if (mmsg.Body is MultipartEncrypted) {
                    iscrypt = true;
                    try {
                        if (CryptGpgContext.CheckInstalled()) {
                            MailMessageCrypt crypt = new();
                            isdecrypt = await crypt.Decrypt(mmsg, (ex) => ex.StatusBarError());
                        }
                    } catch (Exception ex) {
                        Global.Instance.Log.Add(nameof(MailMessageCrypt.Decrypt), ex);
                    }
                }

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

                string subj = mmsg.Subject;
                if (!string.IsNullOrWhiteSpace(subj))
                    subj = Regex.Replace(subj, @"\p{C}+", string.Empty);

                Application.MainLoop.Invoke(() => {

                    subjText.Text = subj;
                    dateText.Text = mmsg.Date.ToString("dddd, dd MMMM yyyy");
                    sizeText.Text = rdata.Info.Length.Humanize();
                    msgIdText.Text = mmsg.MessageId;
                    fromText.Text = mmsg.From.ToString();
                    frameHeader.Title = $"{RES.TAG_FOLDER} {folder} - {RES.TAG_TO} {mmsg.To}";

                    textLabel.ColorScheme = b[0] ? colorEnable : colorDisable;
                    htmlLabel.ColorScheme = b[1] ? colorEnable : colorDisable;
                    textLabel.Redraw(textLabel.Bounds);
                    htmlLabel.Redraw(htmlLabel.Bounds);

                    msgText.Text = messageBody[0];
                });

                try {
                    #region Attach to list
                    if (((iscrypt && isdecrypt) || !iscrypt) &&
                        (mmsg.Attachments != null) && (mmsg.Attachments.Count() > 0) &&
                        (msgdt != default) && !string.IsNullOrWhiteSpace(accfolder)) {

                        string path = Global.AppendPartDirectory(
                            Global.GetUserDirectory(accfolder), Global.DirectoryPlace.Attach, msgdt);

                        for (int i = 0; i < mmsg.Attachments.Count(); i++) {
                            string name = MailMessage.GetMimeEntryName(mmsg.Attachments.ElementAt(i));
                            if (!string.IsNullOrWhiteSpace(name))
                                attachList.Add(new MenuItem(
                                        name.Replace('_', ' '), "",
                                        () => BrowseAttachFile(path, mmsg.MessageId, name)));
                        }
                    }
                    #endregion

                } catch (Exception ex) { ex.StatusBarError(); }

                    try {
                    #region CC, Bcc add to list
                    AddAddresses(mmsg.Cc, ccList);
                    AddAddresses(mmsg.ResentCc, ccList);
                    AddAddresses(mmsg.ResentTo, ccList);
                    AddAddresses(mmsg.To, ccList, 1);
                    AddAddresses(mmsg.Bcc, bccList);
                    AddAddresses(mmsg.ResentBcc, bccList);
                    #endregion

                } catch (Exception ex) { ex.StatusBarError(); }

                try {
                    #region Icon switcher
                    int idx =
                        ((ccList.Count > 0) ? 10 : 0) +
                        ((bccList.Count > 0) ? 30 : 0) +
                        ((rdata.Info.Length > 100000) ? 50 : 0) +
                        (iscrypt ? 100 : 0);

                    List<GuiLinearData> layout = default;
                    if (idx > 0)
                        layout = linearLayot.GetDefault();

                    if (iscrypt)
                        pgpLabel.ColorScheme = isdecrypt ? colorBageGreen : colorBageRed;

                    switch (idx)
                    {
                        case 10:
                            {
                                ccLabel.X = layout[3].X + 6;
                                ccLabel.Visible = true;
                                break;
                            }
                        case 30:
                            {
                                bccLabel.Visible = true;
                                break;
                            }
                        case 40:
                            {
                                ccLabel.Visible = bccLabel.Visible = true;
                                break;
                            }
                        case 50:
                            {
                                oversizeLabel.X = layout[2].X + 5 + 6;
                                oversizeLabel.Visible = true;
                                break;
                            }
                        case 60:
                            {
                                ccLabel.X = layout[3].X + 6;
                                oversizeLabel.X = layout[2].X + 6;
                                ccLabel.Visible = oversizeLabel.Visible = true;
                                break;
                            }
                        case 80:
                            {
                                oversizeLabel.X = layout[2].X + 5;
                                bccLabel.Visible = oversizeLabel.Visible = true;
                                break;
                            }
                        case 90:
                            {
                                ccLabel.Visible = bccLabel.Visible = oversizeLabel.Visible = true;
                                break;
                            }
                        case 100:
                            {
                                pgpLabel.X = layout[1].X + 19;
                                pgpLabel.Visible = true;
                                break;
                            }
                        case 110:
                            {
                                ccLabel.X = layout[3].X + 6;
                                pgpLabel.X = layout[1].X + 8 + 6;
                                ccLabel.Visible = pgpLabel.Visible = true;
                                break;
                            }
                        case 130:
                            {
                                pgpLabel.X = layout[1].X + 8 + 5;
                                bccLabel.Visible = pgpLabel.Visible = true;
                                break;
                            }
                        case 140:
                            {
                                pgpLabel.X = layout[1].X + 8;
                                ccLabel.Visible = bccLabel.Visible = pgpLabel.Visible = true;
                                break;
                            }
                        case 150:
                            {
                                oversizeLabel.X = layout[2].X + 5 + 6;
                                pgpLabel.X = layout[1].X + 5 + 6;
                                oversizeLabel.Visible = pgpLabel.Visible = true;
                                break;
                            }
                        case 160:
                            {
                                ccLabel.X = layout[3].X + 6;
                                oversizeLabel.X = layout[2].X + 6;
                                pgpLabel.X = layout[1].X + 6;
                                ccLabel.Visible = oversizeLabel.Visible = pgpLabel.Visible = true;
                                break;
                            }
                        case 180:
                            {
                                oversizeLabel.X = layout[2].X + 5;
                                pgpLabel.X = layout[1].X + 5;
                                bccLabel.Visible = oversizeLabel.Visible = pgpLabel.Visible = true;
                                break;
                            }
                        case 190:
                            {
                                ccLabel.Visible = bccLabel.Visible = oversizeLabel.Visible = pgpLabel.Visible = true;
                                break;
                            }
                        default: break;
                    }
                    #endregion

                } catch (Exception ex) { ex.StatusBarError(); }

                try {
                    #region Source message load
                    using FileStream fs = new FileStream(rdata.Info.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    long size = (rdata.Info.Length > 100000) ? 100000 : rdata.Info.Length;
                    byte[] buffer = new byte[size];
                    fs.Seek(0, SeekOrigin.Begin);
                    int idx = await fs.ReadAsync(buffer, 0, (int)size).ConfigureAwait(false);
                    if (idx > 0) {
                        messageBody[1] = Encoding.UTF8.GetString(buffer, 0, idx);
                        int x = messageBody[1].IndexOf("\r\n\r\n");
                        if (x > 0)
                            infoText.Text = messageBody[1].Substring(0, x);
                    }
                    #endregion

                } catch (Exception ex) {
                    ex.StatusBarError();
                    Global.Instance.Log.Add("Read Raw Message", ex);
                }

                try {
                    #region Context menu append
                    MenuItem[] items = new MenuItem[] {
                        null,
                        new MenuItem(RES.BTN_REPLAY, "", CommandReplay),
                        new MenuItem(RES.MENU_MSGFORWARDS, "", CommandForward),
                        new MenuItem(RES.MENU_OPENURI, "", () => {
                            try {
                                if (!msgText.IsSelecting) return;
                                msgText.Copy();
                                if (!Clipboard.Contents.IsEmpty)
                                    Clipboard.Contents.ToString().BrowseFile();

                            } catch (Exception ex) { ex.StatusBarError(); }
                        }, () => msgText.IsSelecting)
                    };
                    msgText.AddContextMenu(items);
                    #endregion

                } catch (Exception ex) { ex.StatusBarError(); }

                try {
                    #region top bar menu append
                    MenuBarItem[] list = new MenuBarItem[GuiMenu.Menus.Length];
                    GuiMenu.Menus.CopyTo(list, 0);
                    List<MenuBarItem> mainMenu = new(list);

                    if (bccList.Count > 0) {
                        MenuItem[] items = AddMenu(bccList);
                        if ((items != default) && (items.Length > 0))
                            mainMenu.Add(new MenuBarItem("_Bcc", items));
                    }
                    if (ccList.Count > 0) {
                        MenuItem[] items = AddMenu(ccList);
                        if ((items != default) && (items.Length > 0))
                            mainMenu.Add(new MenuBarItem("_Cc", items));
                    }
                    if (attachList.Count > 0)
                        mainMenu.Add(new MenuBarItem($"_{RES.TAG_ATTACH}", attachList.ToArray()));

                    mainMenu.Add(typeof(GuiMessageReadWindow).LoadMenuHotKeys());

                    Application.MainLoop.Invoke(() => {
                        GuiMenu.Menus = mainMenu.ToArray();
                        GuiMenu.SetNeedsDisplay();
                        if (attachList.Count > 0) {
                            attachLabel.Visible = attachText.Visible = true;
                            attachText.Text = attachList.Count.ToString();
                        }
                    });
                    #endregion

                } catch (Exception ex) { ex.StatusBarError(); }
            }
            catch (Exception ex) { ex.StatusBarError(); }
            finally { Application.MainLoop.Invoke(() => buttonClose.SetFocus()); }
            return true;
        });
        #endregion

        #region Window key event
        private void Window_KeyUp(KeyEventEventArgs a) {
            if (!keyOnce.Begin()) return;
            try {
                if (a != null) {
                    if (a.KeyEvent.IsAlt)
                        switch (a.KeyEvent.ParseKeyEvent()) {
                            case Key.F: CommandForward.Invoke(); break;
                            case Key.O: CommandOpen.Invoke(); break;
                            case Key.C: CommandClose.Invoke(); break;
                            case Key.R: CommandReplay.Invoke(); break;
                            case Key.S: CommandSource.Invoke(); break;
                            default: return;
                        }
                    else
                        switch (a.KeyEvent.Key) {
                            case Key.Esc: {
                                    if (!IsMenuOpen) CommandClose.Invoke();
                                    IsMenuOpen = false;
                                    break;
                                }
                            default: return;
                        }
                    a.Handled = true;
                }
            } finally { keyOnce.End(); }
        }
        #endregion

        private void AddAddresses(InternetAddressList addr, List<MailboxAddress> list, int start = 0) {
            if ((addr != null) || addr.Count == 0)
                return;
            for (int i = start; i < addr.Count; i++)
                if (addr[i] is MailboxAddress ma) list.Add(ma);
        }

        private MenuItem[] AddMenu(List<MailboxAddress> list) {
            if (list.Count > 0) {
                int n = 0;
                MenuItem[] items = new MenuItem[list.Count];
                for (int i = 0; i < list.Count; i++)
                    if (list[i] is MailboxAddress addr)
                        items[n++] = new MenuItem(addr.ToString(), "", () => { });
                if (n > 0) return items;
            }
            return default;
        }

        private void BrowseAttachFile(string path, string id, string name) {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(name))
                return;
            MailMessage.GetAttachFilePath(path, id, name).BrowseFile();
        }
    }
}

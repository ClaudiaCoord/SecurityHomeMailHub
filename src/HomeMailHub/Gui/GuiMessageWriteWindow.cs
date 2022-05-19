/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HomeMailHub.Gui.Dialogs;
using MimeKit;
using NStack;
using SecyrityMail;
using SecyrityMail.Data;
using SecyrityMail.MailAccounts;
using SecyrityMail.Servers;
using SecyrityMail.Utils;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    internal enum SendReturn : int
    {
        Ok = 0,
        None,
        ToEmpty,
        FromEmpty,
        SubjEmpty,
        BodyEmpty,
        AccEmpty,
        Resend,
        Wait
    }

    public class GuiMessageWriteWindow : Window, IGuiWindow<GuiMessageWriteWindow>
    {
        private Toplevel GuiToplevel { get; set; } = default;
        private MenuBar  GuiMenu { get; set; } = default;
        private MenuBarItem attachMenu { get; set; } = default;
        private MenuItem[] attachItemsMenu { get; set; } = default;

        private Button buttonClose { get; set; } = default;
        private Button buttonAttach { get; set; } = default;
        private Button buttonSend { get; set; } = default;

        private Label toLabel { get; set; } = default;
        private Label ccLabel { get; set; } = default;
        private Label bccLabel { get; set; } = default;
        private Label fromLabel { get; set; } = default;
        private Label subjLabel { get; set; } = default;
        private Label attachText { get; set; } = default;
        private Label attachLabel { get; set; } = default;

        private TextField toText { get; set; } = default;
        private TextField ccText { get; set; } = default;
        private TextField bccText { get; set; } = default;
        private TextField subjText { get; set; } = default;
        private ComboBox  fromText { get; set; } = default;

        private FrameView frameHeader { get; set; } = default;
        private FrameView frameMsg { get; set; } = default;
        private TextView msgText { get; set; } = default;
        private CheckBox warningBox { get; set; } = default;

        private bool [] sendWarning = new bool[] { false, false, false };
        private string selectedPath { get; set; } = string.Empty;
        private string selectedFrom { get; set; } = string.Empty;
        private SendReturn sendStatus { get; set; } = SendReturn.None;
        private GuiRunOnce runOnce = new();
        private List<string> fromList = new();
        private List<string> attachList = new();
        private GuiLinearLayot linearLayot { get; } = new();
        private int __lastTo = -1,
                    __lastCc = -1,
                    __lastBcc = -1;

        public Toplevel GetTop => GuiToplevel;

        public GuiMessageWriteWindow() : base(RES.GUIMAILWRITE_TITLE1, 0) {

            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill() - 1;
            GuiToplevel = GuiExtensions.CreteTop();

            linearLayot.Add("en", new List<GuiLinearData> {
                new GuiLinearData(82, 5, true),
                new GuiLinearData(92, 5, true),
                new GuiLinearData(103, 5, true)
            });
            linearLayot.Add("ru", new List<GuiLinearData> {
                new GuiLinearData(75, 5, true),
                new GuiLinearData(87, 5, true),
                new GuiLinearData(100, 5, true)
            });
        }
        ~GuiMessageWriteWindow() => Dispose();

        public new void Dispose() {

            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Init
        public GuiMessageWriteWindow Init(string s)
        {
            selectedPath = s;
            List<GuiLinearData> layout = linearLayot.GetDefault();

            int addrow = string.IsNullOrWhiteSpace(selectedPath) ? 2 : 0;
            frameHeader = new FrameView(new Rect(0, 0, 116, 8 + addrow))
            {
                X = 1,
                Y = 1
            };
            frameMsg = new FrameView(new Rect(0, 0, 116, 17 - addrow), RES.TAG_MESSAGE)
            {
                X = 1,
                Y = 9 + addrow
            };
            if (addrow > 0)
            {
                frameHeader.Add(fromLabel = new Label(RES.TAG_FROM)
                {
                    X = 1,
                    Y = 1,
                    AutoSize = true
                });
                frameHeader.Add(fromText = new ComboBox()
                {
                    X = 11,
                    Y = 1,
                    Width = 65,
                    Height = 6,
                    ReadOnly = true,
                    ColorScheme = GuiApp.ColorField
                });
                fromText.SetSource(fromList);
                frameHeader.Add(warningBox = new CheckBox(1, 0, RES.CHKBOX_NOWARN)
                {
                    X = 82,
                    Y = 1,
                    Width = 10,
                    Height = 1,
                    Checked = GuiApp.IsSendNoWarning
                });
                warningBox.Toggled += WarningBox_Toggled;
            }
            frameHeader.Add(toLabel = new Label(RES.TAG_TO)
            {
                X = 1,
                Y = 1 + addrow,
                AutoSize = true
            });
            frameHeader.Add(toText = new TextField(string.Empty)
            {
                X = 11,
                Y = 1 + addrow,
                Width = 30,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameHeader.Add(ccLabel = new Label("Cc: ")
            {
                X = 42,
                Y = 1 + addrow,
                AutoSize = true
            });
            frameHeader.Add(ccText = new TextField(string.Empty)
            {
                
                X = 46,
                Y = 1 + addrow,
                Width = 30,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameHeader.Add(bccLabel = new Label("Bcc: ")
            {
                X = 77,
                Y = 1 + addrow,
                AutoSize = true
            });
            frameHeader.Add(bccText = new TextField(string.Empty)
            {
                X = 82,
                Y = 1 + addrow,
                Width = 29,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameHeader.Add(subjLabel = new Label(RES.TAG_SUBJECT)
            {
                X = 1,
                Y = 3 + addrow,
                AutoSize = true
            });
            frameHeader.Add(subjText = new TextField(string.Empty)
            {
                X = 11,
                Y = 3 + addrow,
                Width = 100,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameHeader.Add(attachLabel = new Label($"{RES.TAG_ATTACH}:")
            {
                X = 1,
                Y = 5 + addrow,
                AutoSize = true,
                Visible = false
            });
            frameHeader.Add(attachText = new Label(string.Empty)
            {
                X = 11,
                Y = 5 + addrow,
                AutoSize = true,
                Visible = false,
                ColorScheme = GuiApp.ColorDescription
            });
            frameHeader.Add(buttonClose = new Button(10, 19, RES.BTN_CLOSE)
            {
                X = layout[0].X,
                Y = layout[0].Y + addrow,
                AutoSize = layout[0].AutoSize
            });
            frameHeader.Add(buttonAttach = new Button(10, 19, RES.BTN_ATTACH)
            {
                X = layout[1].X,
                Y = layout[1].Y + addrow,
                AutoSize = layout[1].AutoSize
            });
            frameHeader.Add(buttonSend = new Button(10, 19, RES.BTN_SEND)
            {
                X = layout[2].X,
                Y = layout[2].Y + addrow,
                AutoSize = layout[2].AutoSize
            });
            buttonClose.Clicked += () => {
                Application.RequestStop();
            };
            buttonAttach.Clicked += () => {
                GuiOpenDialog d = RES.GUIMAILWRITE_TEXT1.GuiOpenDialogs(true);
                Application.Run(d);
                if (!d.Canceled) {
                    try {
                        string[] ss = d.GuiReturnDialog();
                        if (ss.Length > 0) {
                            attachList.AddRange(ss);
                            attachList = attachList.Distinct().ToList();
                            BuildAttachMenu();
                        }
                    } catch (Exception ex) { ex.StatusBarError(); }
                }
            };
            buttonSend.Clicked += async () => {
                await Send_().ConfigureAwait(false);
            };

            List<string> sugg = Global.Instance.EmailAddresses.GetSuggestionsList();
            if ((sugg != null) && (sugg.Count > 0)) {

                ColorScheme cs = new ColorScheme() {
                    Normal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Gray),
                    Focus = Application.Driver.MakeAttribute(Color.White, Color.BrightBlue),
                };
                toText.Autocomplete.AllSuggestions =
                ccText.Autocomplete.AllSuggestions =
                bccText.Autocomplete.AllSuggestions = sugg;
                toText.Autocomplete.MaxWidth =
                ccText.Autocomplete.MaxWidth =
                bccText.Autocomplete.MaxWidth = 25;
                toText.Autocomplete.ColorScheme =
                ccText.Autocomplete.ColorScheme =
                bccText.Autocomplete.ColorScheme = cs;

                toText.Leave += (a) =>
                    Address_Leave(toText,  ref __lastTo);
                ccText.Leave += (a) =>
                    Address_Leave(ccText,  ref __lastCc);
                bccText.Leave += (a) =>
                    Address_Leave(bccText, ref __lastBcc);

                toText.TextChanged += (a) =>
                    Address_TextChanged(a, ref __lastTo);
                ccText.TextChanged += (a) =>
                    Address_TextChanged(a, ref __lastCc);
                bccText.TextChanged += (a) =>
                    Address_TextChanged(a, ref __lastBcc);
            }
            Add(frameHeader);

            msgText = new TextView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                Multiline = true,
                ReadOnly = false
            };
            frameMsg.Add(msgText);
            Add(frameMsg);

            attachMenu = new MenuBarItem($"_{RES.TAG_ATTACH}", new MenuItem[0]);
            GuiMenu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
                    new MenuItem (RES.MENU_SEND, "", async () => {
                        await Send_().ConfigureAwait(false);
                    },
                    null, null, Key.AltMask | Key.S),
                    null,
                    new MenuItem (RES.MENU_CLOSE, "", () => Application.RequestStop(), null, null, Key.AltMask | Key.Q)
                }),
                attachMenu
            });

            GuiToplevel.Add(GuiMenu, this);
            return this;
        }
        #endregion

        private void Address_TextChanged(ustring s, ref int status) =>
            status = ((s == null) || (s.Length == 0)) ? -1 : status;

        private void Address_Leave(TextField tf, ref int status) {

            if (tf.Text == null) {
                status = -1;
                return;
            }
            int idx = tf.Autocomplete.SelectedIdx;
            if ((status == idx) || (idx < 0))
                return;
            status = idx;
            string s = tf.Text.ToString().Trim();
            if (string.IsNullOrWhiteSpace(s))
                return;
            MailboxAddress ae;
            if ((ae = Global.Instance.EmailAddresses.Find(s)) == null)
                return;
            tf.Text = ae.ToString();
            tf.Redraw(tf.Bounds);
        }

        private void WarningBox_Toggled(bool b) =>
            GuiApp.IsSendNoWarning = !b;

        public async void Load() => _ = await Load_().ConfigureAwait(false);

        private async Task<bool> Load_() =>
            await Task.Run(async () => {

                MimeMessage mmsg = default(MimeMessage);
                try {
                    if (fromText != default) {
                        for (int i = 0; i < Global.Instance.Accounts.Count; i++)
                            fromList.Add(@$"""{Global.Instance.Accounts[i].Name}"" <{Global.Instance.Accounts[i].Email}>");
                        frameHeader.Title = RES.GUIMAILWRITE_TEXT2;
                        fromText.SetSource(fromList);
                        return true;
                    }

                    ReadMessageData rdata = await selectedPath.ReadMessage().ConfigureAwait(false);
                    if (rdata.IsEmpty)
                        return false;

                    mmsg = rdata.Message;
                    if (mmsg == null)
                        return false;

                    selectedFrom = mmsg.To.ToString();
                    toText.Text = mmsg.From.ToString();
                    subjText.Text = $"Re: {mmsg.Subject}";
                    frameHeader.Title = string.Format(RES.GUIMAILWRITE_FMT1, selectedFrom);

                    string body =
                        (mmsg.TextBody != null) ?
                            mmsg.TextBody : ((mmsg.HtmlBody != null) ? mmsg.HtmlBody : mmsg.ToString());

                    if (!string.IsNullOrWhiteSpace(body))
                        msgText.Text = $"> {body}".Replace("\n", "\n> ");
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally {
                    if (mmsg != null)
                        mmsg.Dispose();
                }
                return true;
            });

        private async Task Send_() =>
            await Task.Run(async () => {

                if (!runOnce.GoRun())
                    return;

                AutoResetEvent rauto = new AutoResetEvent(false);
                try {
                    SendReturn sr = SendReturn.None;
                    while (sr != SendReturn.Ok) {
                        sr = await Send__().ConfigureAwait(false);
                        CheckSendReturn(sr, rauto);
                        rauto.WaitOne();
                        if (sendStatus != SendReturn.Resend) break;
                    }
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally {
                    rauto.Dispose();
                    runOnce.EndRun();
                }
            });

        private async Task<SendReturn> Send__() =>
            await Task.Run(async () => {
                MimeMessage mmsg = default(MimeMessage);
                CancellationTokenSafe safe = new(TimeSpan.FromSeconds(12.0));
                try {
                    InternetAddressList fromlist, tolist;
                    sendWarning[0] = GuiApp.IsSendNoWarning;

                    if (fromText == default)
                        fromlist = selectedFrom.EmailParse();
                    else if (fromText.SelectedItem >= 0)
                        fromlist = fromList[fromText.SelectedItem].EmailParse();
                    else
                        return SendReturn.FromEmpty;

                    if (fromlist.Count == 0)
                        return SendReturn.FromEmpty;

                    tolist = toText.Text.ToString().EmailParse();
                    if (tolist.Count == 0)
                        return SendReturn.ToEmpty;

                    string subj = subjText.Text.ToString();
                    if (!sendWarning[0] && !sendWarning[1] && string.IsNullOrEmpty(subj))
                        return SendReturn.SubjEmpty;

                    string body = msgText.Text.ToString();
                    if (!sendWarning[0] && !sendWarning[2] && string.IsNullOrEmpty(body))
                        return SendReturn.BodyEmpty;

                    mmsg = new() { Subject = subj };
                    mmsg.To.AddRange(tolist);
                    mmsg.From.AddRange(fromlist);

                    BodyBuilder builder = new();
                    builder.HtmlBody = new ConverterTextToHtml().Convert(body);
                    builder.TextBody = body;

                    if (attachList.Count > 0)
                        foreach (string s in attachList)
                            builder.Attachments.Add(s, safe.Token);

                    mmsg.Body = builder.ToMessageBody();
                    UserAccount acc = Global.Instance.FindAccount(((MailboxAddress)fromlist[0]).Address);

                    if (acc == default)
                        return SendReturn.AccEmpty;

                    CredentialsRoute route = new(acc);
                    MessageStoreReturn msr = await route.MessageStore(mmsg, Global.Instance.ToMainEvent)
                                                        .ConfigureAwait(false);
                    if (msr == MessageStoreReturn.MessageDelivered)
                        return SendReturn.Ok;
                    Global.Instance.ToMainEvent(
                        MailEventId.DeliverySendMessage, msr.ToString(), null);
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally {
                    if (mmsg != null)
                        mmsg.Dispose();
                    safe.Dispose();
                }
                return SendReturn.None;
            });

        private void BuildAttachMenu() {
            try {
                bool b = attachList.Count > 0;
                attachText.Visible = attachLabel.Visible = b;
                if (!b) {
                    Application.MainLoop.Invoke(() => attachMenu.Children = new MenuItem[0]);
                    return;
                }
                attachText.Text = attachList.Count.ToString();
                attachItemsMenu = new MenuItem[attachList.Count + 2];
                int i = 0;
                for (; i < attachList.Count; i++) {
                    string path = attachList[i];
                    attachItemsMenu[i] = new MenuItem(
                            Path.GetFileName(path.Replace('_', ' ')).Trim(), "",
                            () => RemoveAttachFile(path));
                }
                attachItemsMenu[i++] = null;
                attachItemsMenu[i] = new MenuItem(
                        RES.MENU_DELALLATTACH, "", () => RemoveAllAttachFile());
                Application.MainLoop.Invoke(() => attachMenu.Children = attachItemsMenu);
            } catch (Exception ex) { ex.StatusBarError(); }
        }

        private void RemoveAttachFile(string s) {
            if (!attachList.Contains(s)) return;
            Application.MainLoop.Invoke(() => {
                if (MessageBox.Query(50, 7,
                    RES.TAG_DELETE,
                    string.Format(RES.TAG_FMT_DELATTACH, Path.GetFileName(s)), RES.TAG_YES, RES.TAG_NO) == 0) {
                    try {
                        attachList.Remove(s);
                        BuildAttachMenu();
                    } catch (Exception ex) { ex.StatusBarError(); }
                }
            });
        }

        private void RemoveAllAttachFile() {
            Application.MainLoop.Invoke(() => {
                if (MessageBox.Query(50, 7,
                    RES.TAG_DELETE,
                    RES.TAG_DELALLATTACH, RES.TAG_YES, RES.TAG_NO) == 0) {
                    try {
                        attachList.Clear();
                        BuildAttachMenu();
                    } catch (Exception ex) { ex.StatusBarError(); }
                }
            });
        }

        private void CheckSendReturn(SendReturn sr, AutoResetEvent rauto)
        {
            sendStatus = SendReturn.Wait;
            Application.MainLoop.Invoke(() => {
                switch (sr) {
                    case SendReturn.FromEmpty: {
                            _ = MessageBox.ErrorQuery(50, 7,
                                RES.GUIMAILWRITE_ERR1, $"{RES.GUIMAILWRITE_ERR1}, {RES.GUIMAILWRITE_ERR6}", RES.TAG_OK);
                            if (fromText != null)
                                fromText.SetFocus();
                            break;
                        }
                    case SendReturn.ToEmpty: {
                            _ = MessageBox.ErrorQuery(50, 7,
                                RES.GUIMAILWRITE_ERR2, $"{RES.GUIMAILWRITE_ERR2}, {RES.GUIMAILWRITE_ERR6}", RES.TAG_OK);
                            toText.SetFocus();
                            break;
                        }
                    case SendReturn.SubjEmpty: {
                            if (MessageBox.Query(50, 7,
                                RES.GUIMAILWRITE_ERR3, $"{RES.GUIMAILWRITE_ERR3}, {RES.TAG_SEND}?", RES.TAG_YES, RES.TAG_NO) == 0) {
                                sendWarning[1] = true;
                                sendStatus = SendReturn.Resend;
                            }
                            subjText.SetFocus();
                            break;
                        }
                    case SendReturn.BodyEmpty: {
                            if (MessageBox.Query(50, 7,
                                RES.GUIMAILWRITE_ERR3, $"{RES.GUIMAILWRITE_ERR4}, {RES.TAG_SEND}?", RES.TAG_YES, RES.TAG_NO) == 0) {
                                sendWarning[2] = true;
                                sendStatus = SendReturn.Resend;
                                break;
                            }
                            msgText.SetFocus();
                            break;
                        }
                    case SendReturn.AccEmpty: {
                            _ = MessageBox.ErrorQuery(50, 7,
                                RES.GUIMAILWRITE_ERR5, $"{RES.GUIMAILWRITE_ERR5}, {RES.GUIMAILWRITE_ERR6}", RES.TAG_OK);
                            break;
                        }
                    case SendReturn.Ok: {
                            Application.RequestStop();
                            sendStatus = SendReturn.Ok;
                            break;
                        }
                    default: break;
                }
                rauto.Set();
            });
        }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using NStack;
using SecyrityMail;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiServicesSettingsWindow : Window, IGuiWindow<GuiServicesSettingsWindow>
    {
        private const int labelOffset = 10;
        private Toplevel GuiToplevel { get; set; } = default;
        private MenuBar GuiMenu { get; set; } = default;
        private MenuBarItem urlmenu { get; set; } = default;

        private Button buttonPop3Action { get; set; } = default;
        private Button buttonSmtpAction { get; set; } = default;
        private Button buttonForbidenRouteAdd { get; set; } = default;
        private Button buttonForbidenRouteSort { get; set; } = default;
        private Button buttonForbidenRouteDelete { get; set; } = default;
        private Button buttonForbidenEntryAdd { get; set; } = default;
        private Button buttonForbidenEntrySort { get; set; } = default;
        private Button buttonForbidenEntryDelete { get; set; } = default;

        private Label hostPop3Label { get; set; } = default;
        private Label portPop3Label { get; set; } = default;
        private Label idlePop3Label { get; set; } = default;
        private Label hostSmtpLabel { get; set; } = default;
        private Label portSmtpLabel { get; set; } = default;
        private Label idleSmtpLabel { get; set; } = default;
        private Label pgpPassLabel { get; set; } = default;
        private Label forbidenRouteLabel { get; set; } = default;
        private Label forbidenEntryLabel { get; set; } = default;
        private Label spamCheckCountLabel { get; set; } = default;
        private Label spamClientIdleLabel { get; set; } = default;
        private Label idleClientsLabel { get; set; } = default;
        private Label checkMailClientsLabel { get; set; } = default;

        private ComboBox hostPop3Box { get; set; } = default;
        private ComboBox hostSmtpBox { get; set; } = default;

        private TextField portPop3Text { get; set; } = default;
        private TextField idlePop3Text { get; set; } = default;
        private TextField portSmtpText { get; set; } = default;
        private TextField idleSmtpText { get; set; } = default;
        private TextField pgpPassText  { get; set; } = default;
        private TextField ForbidenRouteText { get; set; } = default;
        private TextField forbidenEntryText { get; set; } = default;
        private TextField entrySelectText { get; set; } = default;
        private TextField spamCheckCountText { get; set; } = default;
        private TextField spamClientIdleText { get; set; } = default;
        private TextField idleClientsText { get; set; } = default;
        private TextField checkMailClientsText { get; set; } = default;

        private CheckBox enablePop3Box { get; set; } = default;
        private CheckBox enablePop3Log { get; set; } = default;
        private CheckBox enablePop3PgpDecrypt { get; set; } = default;
        private CheckBox enablePop3DeleteAllMessages { get; set; } = default;

        private CheckBox enableSmtpBox { get; set; } = default;
        private CheckBox enableSmtpLog { get; set; } = default;
        private CheckBox enableSmtpDeliveryLocal { get; set; } = default;
        private CheckBox enableSmtpCheckFrom { get; set; } = default;
        private CheckBox enableSmtpAllOutPgpSign { get; set; } = default;
        private CheckBox enableSmtpAllOutPgpCrypt { get; set; } = default;

        private CheckBox enableSharingSocket { get; set; } = default;
        private CheckBox enableSaveAttachments { get; set; } = default;
        private CheckBox enableAlwaysNewMessageId { get; set; } = default;
        private CheckBox enableModifyMessageDeliveredLocal { get; set; } = default;
        private CheckBox enableNewMessageSendImmediately { get; set; } = default;

        private CheckBox enableImapClientMessagePurge { get; set; } = default;
        private CheckBox enableSmtpClientFakeIp { get; set; } = default;
        private CheckBox enableReceiveOnSend { get; set; } = default;

        private TabView tabView { get; set; } = default;
        private ListView listForbidenRouteView { get; set; } = default;
        private ListView listForbidenEntryView { get; set; } = default;
        private FrameView framePOP3 { get; set; } = default;
        private FrameView framePOP3Left { get; set; } = default;
        private FrameView framePOP3Right { get; set; } = default;
        private FrameView frameSMTP { get; set; } = default;
        private FrameView frameSMTPLeft { get; set; } = default;
        private FrameView frameSMTPRight { get; set; } = default;
        private FrameView frameSecure { get; set; } = default;
        private FrameView frameSecureLeft { get; set; } = default;
        private FrameView frameSecureRight { get; set; } = default;
        private FrameView frameSecureRoute { get; set; } = default;
        private FrameView frameSecureEntry { get; set; } = default;
        private FrameView frameClients { get; set; } = default;
        private FrameView frameClientsLeft { get; set; } = default;
        private RadioGroup entrySelectType { get; set; } = default;

        private List<string> adapters = new();
        private GuiLinearLayot linearLayot { get; } = new();

        public Toplevel GetTop => GuiToplevel;

        public GuiServicesSettingsWindow() : base(RES.MENU_SERVSET.ClearText(), 0)
        {
            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill() - 1;
            GuiToplevel = GuiExtensions.CreteTop();
            adapters.Add("*");

            linearLayot.Add("en", new List<GuiLinearData> {
                new GuiLinearData(21, 14, true),
                new GuiLinearData(30, 14, true),
                new GuiLinearData(38, 14, true),
                new GuiLinearData(30, 8, true),
                new GuiLinearData(39, 8, true),
                new GuiLinearData(47, 8, true)
            });
            linearLayot.Add("ru", new List<GuiLinearData> {
                new GuiLinearData(8,  14, true),
                new GuiLinearData(24, 14, true),
                new GuiLinearData(37, 14, true),
                new GuiLinearData(17, 8, true),
                new GuiLinearData(33, 8, true),
                new GuiLinearData(46, 8, true)
            });
        }
        ~GuiServicesSettingsWindow() => Dispose();

        public new void Dispose() {

            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Init
        public GuiServicesSettingsWindow Init(string s)
        {
            List<GuiLinearData> layout = linearLayot.GetDefault();

            tabView = new TabView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            /* POP3 */
            #region POP3
            framePOP3 = new FrameView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 1,
                Border = new Border { BorderStyle = BorderStyle.None }
            };
            #region POP3 Left
            framePOP3Left = new FrameView(new Rect(0, 0, 52, 7), RES.TAG_NETWORK)
            {
                X = 1,
                Y = 0
            };
            framePOP3Left.Add(hostPop3Label = new Label(RES.TAG_HOST)
            {
                X = 1,
                Y = 1,
                AutoSize = true
            });
            framePOP3Left.Add(hostPop3Box = new ComboBox()
            {
                X = labelOffset,
                Y = 1,
                Width = 37,
                Height = 5,
                TabIndex = 3,
                ColorScheme = GuiApp.ColorField
            });
            framePOP3Left.Add(portPop3Label = new Label(RES.TAG_PORT)
            {
                X = 1,
                Y = 3,
                AutoSize = true
            });
            framePOP3Left.Add(portPop3Text = new TextField(Global.Instance.Config.Pop3ServicePort.ToString())
            {
                X = labelOffset,
                Y = 3,
                Width = 8,
                Height = 1,
                TabIndex = 1,
                ColorScheme = GuiApp.ColorField
            });
            framePOP3Left.Add(idlePop3Label = new Label(RES.TAG_IDLE)
            {
                X = 19,
                Y = 3,
                AutoSize = true
            });
            framePOP3Left.Add(idlePop3Text = new TextField(Global.Instance.Config.Pop3ClientIdle.ToString())
            {
                X = 26,
                Y = 3,
                Width = 8,
                Height = 1,
                TabIndex = 2,
                ColorScheme = GuiApp.ColorField
            });
            framePOP3Left.Add(enablePop3Box = new CheckBox(1, 0, RES.TAG_ENABLE)
            {
                X = 37,
                Y = 3,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsPop3Enable || Global.Instance.IsPop3Run
            });
            hostPop3Box.SetSource(adapters);
            hostPop3Box.SelectedItemChanged += HostBox_SelectedItemChanged;
            portPop3Text.TextChanged += PortPop3Text_TextChanged;
            idlePop3Text.TextChanged += IdlePop3Text_TextChanged;
            enablePop3Box.Toggled += EnablePop3Box_Toggled;
            framePOP3.Add(framePOP3Left);
            #endregion

            #region POP3 Right
            framePOP3Right = new FrameView(new Rect(0, 0, 61, 7), RES.TAG_OPTION)
            {
                X = 54,
                Y = 0
            };
            framePOP3Right.Add(enablePop3PgpDecrypt = new CheckBox(1, 0, RES.CHKBOX_POP3PGP)
            {
                X = 1,
                Y = 1,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsIncommingPgpDecrypt
            });
            framePOP3Right.Add(enablePop3DeleteAllMessages = new CheckBox(1, 0, RES.CHKBOX_POP3DELALL)
            {
                X = 1,
                Y = 2,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsPop3DeleteAllMessages
            });
            framePOP3Right.Add(enablePop3Log = new CheckBox(1, 0, RES.CHKBOX_SESSIONLOG)
            {
                X = 1,
                Y = 3,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsPop3Log
            });
            enablePop3Log.Toggled += EnablePop3Log_Toggled;
            enablePop3PgpDecrypt.Toggled += EnablePop3PgpDecrypt_Toggled;
            enablePop3DeleteAllMessages.Toggled += EnablePop3DeleteAllMessages_Toggled;
            framePOP3.Add(framePOP3Right);
            framePOP3.Add(buttonPop3Action = new Button(10, 19, Global.Instance.IsPop3Run ? RES.BTN_STOP : RES.BTN_START)
            {
                X = 43,
                Y = 7,
                AutoSize = true,
                ColorScheme = Global.Instance.IsPop3Run ? GuiApp.ColorGreen : GuiApp.ColorRed
            });
            buttonPop3Action.Clicked += ButtonPop3Action_Clicked;
            #endregion

            #endregion

            /* SMTP */
            #region SMTP
            frameSMTP = new FrameView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 1,
                Border = new Border { BorderStyle = BorderStyle.None }
            };
            #region SMTP Left
            frameSMTPLeft = new FrameView(new Rect(0, 0, 52, 7), RES.TAG_NETWORK)
            {
                X = 1,
                Y = 0
            };
            frameSMTPLeft.Add(hostSmtpLabel = new Label(RES.TAG_HOST)
            {
                X = 1,
                Y = 1,
                AutoSize = true
            });
            frameSMTPLeft.Add(hostSmtpBox = new ComboBox()
            {
                X = labelOffset,
                Y = 1,
                Width = 37,
                Height = 5,
                TabIndex = 3,
                ColorScheme = GuiApp.ColorField
            });
            frameSMTPLeft.Add(portSmtpLabel = new Label(RES.TAG_PORT)
            {
                X = 1,
                Y = 3,
                AutoSize = true
            });
            frameSMTPLeft.Add(portSmtpText = new TextField(Global.Instance.Config.SmtpServicePort.ToString())
            {
                X = labelOffset,
                Y = 3,
                Width = 8,
                Height = 1,
                TabIndex = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameSMTPLeft.Add(idleSmtpLabel = new Label(RES.TAG_IDLE)
            {
                X = 19,
                Y = 3,
                AutoSize = true
            });
            frameSMTPLeft.Add(idleSmtpText = new TextField(Global.Instance.Config.SmtpClientIdle.ToString())
            {
                X = 26,
                Y = 3,
                Width = 8,
                Height = 1,
                TabIndex = 2,
                ColorScheme = GuiApp.ColorField
            });
            frameSMTPLeft.Add(enableSmtpBox = new CheckBox(1, 0, RES.TAG_ENABLE)
            {
                X = 37,
                Y = 3,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsSmtpEnable || Global.Instance.IsSmtpRun
            });
            hostSmtpBox.SetSource(adapters);
            hostSmtpBox.SelectedItemChanged += HostBox_SelectedItemChanged;
            portSmtpText.TextChanged += PortSmtpText_TextChanged;
            idleSmtpText.TextChanged += IdleSmtpText_TextChanged;
            enableSmtpBox.Toggled += EnableSmtpBox_Toggled;
            frameSMTP.Add(frameSMTPLeft);
            #endregion

            #region SMTP Right
            frameSMTPRight = new FrameView(new Rect(0, 0, 61, 9), RES.TAG_OPTION)
            {
                X = 54,
                Y = 0
            };
            frameSMTPRight.Add(enableSmtpAllOutPgpSign = new CheckBox(1, 0, RES.CHKBOX_SMTPPGPSIGN)
            {
                X = 1,
                Y = 1,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsSmtpAllOutPgpSign
            });
            frameSMTPRight.Add(enableSmtpAllOutPgpCrypt = new CheckBox(1, 0, RES.CHKBOX_SMTPPGPCRYPT)
            {
                X = 1,
                Y = 2,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsSmtpAllOutPgpCrypt
            });
            frameSMTPRight.Add(enableSmtpDeliveryLocal = new CheckBox(1, 0, RES.CHKBOX_DELIVERYLOC)
            {
                X = 1,
                Y = 3,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsSmtpDeliveryLocal
            });
            frameSMTPRight.Add(enableSmtpCheckFrom = new CheckBox(1, 0, RES.CHKBOX_CHECKFROM)
            {
                X = 1,
                Y = 4,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsSmtpCheckFrom
            });
            frameSMTPRight.Add(enableSmtpLog = new CheckBox(1, 0, RES.CHKBOX_SESSIONLOG)
            {
                X = 1,
                Y = 5,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsSmtpLog
            });
            enableSmtpLog.Toggled += EnableSmtpLog_Toggled;
            enableSmtpAllOutPgpSign.Toggled += EnableSmtpAllOutPgpSign_Toggled;
            enableSmtpAllOutPgpCrypt.Toggled += EnableSmtpAllOutPgpCrypt_Toggled;
            enableSmtpDeliveryLocal.Toggled += EnableSmtpDeliveryLocal_Toggled;
            enableSmtpCheckFrom.Toggled += EnableSmtpCheckFrom_Toggled;
            frameSMTP.Add(frameSMTPRight);
            frameSMTP.Add(buttonSmtpAction = new Button(10, 19, Global.Instance.IsSmtpRun ? RES.BTN_STOP : RES.BTN_START)
            {
                X = 43,
                Y = 7,
                AutoSize = true,
                ColorScheme = Global.Instance.IsSmtpRun ? GuiApp.ColorGreen : GuiApp.ColorRed
            });
            buttonSmtpAction.Clicked += ButtonSmtpAction_Clicked;
            #endregion

            #endregion

            /* Secure */
            #region Secure
            frameSecure = new FrameView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill(),
                Border = new Border { BorderStyle = BorderStyle.None }
            };
            #region Secure Left
            frameSecureLeft = new FrameView(new Rect(0, 0, 52, 5), "PGP")
            {
                X = 1,
                Y = 0
            };
            frameSecureLeft.Add(pgpPassLabel = new Label(RES.TAG_PASSWORD)
            {
                X = 1,
                Y = 1,
                AutoSize = true
            });
            frameSecureLeft.Add(pgpPassText = new TextField(Global.Instance.Config.PgpPassword)
            {
                X = labelOffset,
                Y = 1,
                Width = 38,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            pgpPassText.TextChanged += PgpPassText_TextChanged;
            frameSecure.Add(frameSecureLeft);
            #endregion

            #region Secure Right
            frameSecureRight = new FrameView(new Rect(0, 0, 61, 11), RES.TAG_OPTION)
            {
                X = 54,
                Y = 0
            };
            frameSecureRight.Add(enableSaveAttachments = new CheckBox(1, 0, RES.CHKBOX_SAVEATTACH)
            {
                X = 1,
                Y = 1,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsSaveAttachments
            });
            frameSecureRight.Add(enableSharingSocket = new CheckBox(1, 0, RES.CHKBOX_SHARINGSOCKET)
            {
                X = 1,
                Y = 2,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsSharingSocket
            });
            frameSecureRight.Add(enableAlwaysNewMessageId = new CheckBox(1, 0, RES.CHKBOX_NEWMSGID)
            {
                X = 1,
                Y = 3,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsAlwaysNewMessageId
            });
            frameSecureRight.Add(enableNewMessageSendImmediately = new CheckBox(1, 0, RES.CHKBOX_NEWMSGIMD)
            {
                X = 1,
                Y = 4,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsNewMessageSendImmediately
            });
            frameSecureRight.Add(enableModifyMessageDeliveredLocal = new CheckBox(1, 0, RES.CHKBOX_DELIVERYMSGMOD)
            {
                X = 1,
                Y = 5,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsModifyMessageDeliveredLocal
            });
            frameSecureRight.Add(spamCheckCountText = new TextField(Global.Instance.Config.SpamCheckCount.ToString())
            {
                X = 1,
                Y = 6,
                Width = 2,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameSecureRight.Add(spamCheckCountLabel = new Label(RES.TAG_SPAMCOUNT)
            {
                X = 4,
                Y = 6,
                AutoSize = true
            });
            frameSecureRight.Add(spamClientIdleText = new TextField(Global.Instance.Config.SpamClientIdle.ToString())
            {
                X = 1,
                Y = 7,
                Width = 2,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameSecureRight.Add(spamClientIdleLabel = new Label(RES.TAG_SPAMTIMEOUT)
            {
                X = 4,
                Y = 7,
                AutoSize = true
            });
            enableSharingSocket.Toggled += EnableSharingSocket_Toggled;
            enableSaveAttachments.Toggled += EnableSaveAttachments_Toggled;
            enableAlwaysNewMessageId.Toggled += EnableAlwaysNewMessageId_Toggled;
            enableNewMessageSendImmediately.Toggled += EnableNewMessageSendImmediately_Toggled;
            enableModifyMessageDeliveredLocal.Toggled += EnableModifyMessageDeliveredLocal_Toggled;
            spamCheckCountText.TextChanged += SpamCheckCountText_TextChanged;
            spamClientIdleText.TextChanged += SpamClientIdleText_TextChanged;
            frameSecure.Add(frameSecureRight);
            #endregion

            #region Secure Forbiden Entry
            frameSecureEntry = new FrameView(new Rect(0, 0, 52, 17), RES.TAG_BANNEDIPLIST)
            {
                X = 1,
                Y = 5
            };
            frameSecureEntry.Add(entrySelectType = new RadioGroup(new ustring[] { $" {RES.TAG_FORBIDDEN} ", $" {RES.TAG_ALLOWED} " })
            {
                X = 1,
                Y = 1,
                Width = 35,
                Height = 1,
                DisplayMode = DisplayModeLayout.Horizontal,
                SelectedItem = Global.Instance.Config.IsAccessIpWhiteList ? 1 : 0,
                NoSymbol = true
            });
            frameSecureEntry.Add(entrySelectText = new TextField(EntryType()) 
            {
                X = 37,
                Y = 1,
                Width = 11,
                Height = 1,
                ColorScheme = EntryTypeColor()
            });
            frameSecureEntry.Add(listForbidenEntryView = new ListView(Global.Instance.Config.ForbidenEntryList)
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill() - 2,
                Height = 8,
                AllowsMarking = true,
                AllowsMultipleSelection = false
            });
            frameSecureEntry.Add(forbidenEntryLabel = new Label(RES.TAG_HOST)
            {
                X = 1,
                Y = 12,
                AutoSize = true
            });
            frameSecureEntry.Add(forbidenEntryText = new TextField(string.Empty)
            {
                X = 6,
                Y = 12,
                Width = 42,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameSecureEntry.Add(buttonForbidenEntrySort = new Button(10, 19, RES.BTN_SORT)
            {
                X = layout[0].X,
                Y = layout[0].Y,
                AutoSize = layout[0].AutoSize
            });
            frameSecureEntry.Add(buttonForbidenEntryAdd = new Button(10, 19, RES.BTN_ADD)
            {
                X = layout[1].X,
                Y = layout[1].Y,
                AutoSize = layout[1].AutoSize
            });
            frameSecureEntry.Add(buttonForbidenEntryDelete = new Button(10, 19, RES.BTN_DELETE)
            {
                X = layout[2].X,
                Y = layout[2].Y,
                AutoSize = layout[2].AutoSize
            });
            listForbidenEntryView.OpenSelectedItem += ListForbidenEntryView_OpenSelectedItem;
            listForbidenEntryView.SelectedItemChanged += ListForbidenEntryView_SelectedItemChanged;

            forbidenEntryText.KeyUp += ForbidenEntryText_KeyUp;
            buttonForbidenEntryAdd.Clicked += () => ForbidenEntryIp(true);
            buttonForbidenEntrySort.Clicked += () => ForbidenListSort(Global.Instance.Config.ForbidenEntryList, listForbidenEntryView);
            buttonForbidenEntryDelete.Clicked += () => ForbidenEntryIp(false);
            entrySelectType.SelectedItemChanged += EntrySelectType_SelectedItemChanged;
            frameSecure.Add(frameSecureEntry);
            #endregion

            #region Secure Forbiden Route
            frameSecureRoute = new FrameView(new Rect(0, 0, 61, 11), RES.TAG_STOPROUTELIST)
            {
                X = 54,
                Y = 11
            };
            frameSecureRoute.Add(listForbidenRouteView = new ListView(Global.Instance.Config.ForbidenRouteList)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 2,
                Height = 4,
                AllowsMarking = true,
                AllowsMultipleSelection = false
            });
            frameSecureRoute.Add(forbidenRouteLabel = new Label(RES.TAG_HOST)
            {
                X = 1,
                Y = 6,
                AutoSize = true
            });
            frameSecureRoute.Add(ForbidenRouteText = new TextField(string.Empty)
            {
                X = 6,
                Y = 6,
                Width = 51,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameSecureRoute.Add(buttonForbidenRouteSort = new Button(10, 19, RES.BTN_SORT)
            {
                X = layout[3].X,
                Y = layout[3].Y,
                AutoSize = layout[3].AutoSize
            });
            frameSecureRoute.Add(buttonForbidenRouteAdd = new Button(10, 19, RES.BTN_ADD)
            {
                X = layout[4].X,
                Y = layout[4].Y,
                AutoSize = layout[4].AutoSize
            });
            frameSecureRoute.Add(buttonForbidenRouteDelete = new Button(10, 19, RES.BTN_DELETE)
            {
                X = layout[5].X,
                Y = layout[5].Y,
                AutoSize = layout[5].AutoSize
            });
            listForbidenRouteView.OpenSelectedItem += ListForbidenRrouteView_OpenSelectedItem;
            listForbidenRouteView.SelectedItemChanged += ListForbidenRrouteView_SelectedItemChanged;

            ForbidenRouteText.KeyUp += ForbidenRouteText_KeyUp;
            buttonForbidenRouteAdd.Clicked += () => ForbidenRouteIp(true);
            buttonForbidenRouteSort.Clicked += () => ForbidenListSort(Global.Instance.Config.ForbidenRouteList, listForbidenRouteView);
            buttonForbidenRouteDelete.Clicked += () => ForbidenRouteIp(false);
            frameSecure.Add(frameSecureRoute);
            #endregion
            #endregion

            /* Mail clients */
            #region Mail clients
            frameClients = new FrameView()
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 1,
                Border = new Border { BorderStyle = BorderStyle.None }
            };
            #region Clients Left
            frameClientsLeft = new FrameView(RES.TAG_OPTION)
            {
                X = 1,
                Y = 0,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 8
            };
            frameClientsLeft.Add(idleClientsLabel = new Label(RES.TAG_CLIENTSTIMEOUT)
            {
                X = 1,
                Y = 1,
                AutoSize = true
            });
            frameClientsLeft.Add(idleClientsText = new TextField()
            {
                X = 1,
                Y = 2,
                Width = 45,
                Height = 1,
                TabIndex = 1,
                Text = Global.Instance.Config.ClientTimeout.ToString(),
                ColorScheme = GuiApp.ColorField
            });
            frameClientsLeft.Add(checkMailClientsLabel = new Label(RES.TAG_CLIENTSMAILPERIOD)
            {
                X = 1,
                Y = 4,
                AutoSize = true
            });
            frameClientsLeft.Add(checkMailClientsText = new TextField()
            {
                X = 1,
                Y = 5,
                Width = 45,
                Height = 1,
                TabIndex = 2,
                ColorScheme = GuiApp.ColorField,
                Text = (Global.Instance.Config.CheckMailPeriod == Timeout.InfiniteTimeSpan) ?
                    RES.TAG_NOMAILCHECK : Global.Instance.Config.CheckMailPeriod.TotalMinutes.ToString()
            });
            frameClientsLeft.Add(enableImapClientMessagePurge = new CheckBox(1, 0, RES.CHKBOX_IMAPPURGE)
            {
                X = 1,
                Y = 7,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsImapClientMessagePurge
            });
            frameClientsLeft.Add(enableSmtpClientFakeIp = new CheckBox(1, 0, RES.CHKBOX_SMTPFAKEIP)
            {
                X = 1,
                Y = 8,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsSmtpClientFakeIp
            });
            frameClientsLeft.Add(enableReceiveOnSend = new CheckBox(1, 0, RES.CHKBOX_RECEIVEONSEND)
            {
                X = 1,
                Y = 9,
                Width = 10,
                Height = 1,
                Checked = Global.Instance.Config.IsReceiveOnSendOnly
            });
            idleClientsText.TextChanged += IdleClientsText_TextChanged;
            checkMailClientsText.TextChanged += CheckMailClientsText_TextChanged;
            enableReceiveOnSend.Toggled += EnableReceiveOnSend_Toggled;
            enableSmtpClientFakeIp.Toggled += EnableSmtpClientFakeIp_Toggled;
            enableImapClientMessagePurge.Toggled += EnableImapClientMessagePurge_Toggled;
            #endregion

            frameClients.Add(frameClientsLeft);
            #endregion

            tabView.AddTab(new TabView.Tab(" POP3 ", framePOP3), true);
            tabView.AddTab(new TabView.Tab(" SMTP ", frameSMTP), false);
            tabView.AddTab(new TabView.Tab($" {RES.TAG_SECURITY} ", frameSecure), false);
            tabView.AddTab(new TabView.Tab($" {RES.TAG_CLIENTS} ", frameClients), false);
            Add(tabView);

            urlmenu = new MenuBarItem("_Url", new MenuItem[0]);
            GuiMenu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
                    new MenuItem (RES.MENU_ACCESSRELOAD, "", async () => {
                        try {
                            await Global.Instance.ForbidenAccessIp.Reload()
                                                                  .ConfigureAwait(false);
                            string.Format(
                                RES.TAG_FMT_ACCESSRELOAD, Global.Instance.Config.ForbidenEntryList.Count)
                                    .StatusBarText();
                        } catch (Exception ex) { ex.StatusBarError(); }
                    }, null, null, Key.AltMask | Key.R),
                    new MenuItem (RES.MENU_SAVE, "", async () => {
                        try {
                            await new ConfigurationSave().Save();
                            RES.TAG_SAVE.StatusBarText();
                        } catch (Exception ex) { ex.StatusBarError(); }
                    }, null, null, Key.AltMask | Key.S),
                    null,
                    new MenuItem (RES.MENU_CLOSE, "", () => Application.RequestStop(), null, null, Key.AltMask | Key.Q)
                }),
                urlmenu
            });

            GuiToplevel.Add(GuiMenu, this);
            return this;
        }
        #endregion

        #region Configuration setters
        private void IdleSmtpText_TextChanged(ustring s) {
            if (double.TryParse(s.ToString(), out double idle))
                Global.Instance.Config.SmtpClientIdle = idle;
        }

        private void IdlePop3Text_TextChanged(ustring s) {
            if (double.TryParse(s.ToString(), out double idle))
                Global.Instance.Config.Pop3ClientIdle = idle;
        }

        private void PortSmtpText_TextChanged(ustring s) {
            if (int.TryParse(s.ToString(), out int port))
                Global.Instance.Config.SmtpServicePort = port;
        }

        private void PortPop3Text_TextChanged(ustring s) {
            if (int.TryParse(s.ToString(), out int port))
                Global.Instance.Config.Pop3ServicePort = port;
        }

        private void SpamClientIdleText_TextChanged(ustring s) {
            if (double.TryParse(s.ToString(), out double idle))
                Global.Instance.Config.SpamClientIdle = idle;
        }

        private void SpamCheckCountText_TextChanged(ustring s) {
            if (int.TryParse(s.ToString(), out int count))
                Global.Instance.Config.SpamCheckCount = count;
        }

        private void CheckMailClientsText_TextChanged(ustring s) {
            if (double.TryParse(s.ToString(), out double idle))
                Global.Instance.Config.CheckMailPeriod = (idle > 0.0) ?
                    TimeSpan.FromMinutes(idle) : Timeout.InfiniteTimeSpan;
        }

        private void IdleClientsText_TextChanged(ustring s) {
            if (int.TryParse(s.ToString(), out int count))
                Global.Instance.Config.ClientTimeout = count;
        }

        private void PgpPassText_TextChanged(ustring s) =>
            Global.Instance.Config.PgpPassword = s.ToString();

        private void HostBox_SelectedItemChanged(ListViewItemEventArgs a) {
            if ((a != null) && (a.Item > 0) && !string.IsNullOrWhiteSpace(a.Value as string))
                Global.Instance.Config.ServicesInterfaceName = a.Value.ToString();
        }

        private void ButtonPop3Action_Clicked()
        {
            if (!enablePop3Box.Checked)
                return;

            if (Global.Instance.IsPop3Run) {
                Global.Instance.StopPop3Service();
                buttonPop3Action.ColorScheme = GuiApp.ColorRed;
                buttonPop3Action.Text = RES.BTN_START;
            } else {
                Global.Instance.StartPop3Service();
                buttonPop3Action.ColorScheme = GuiApp.ColorGreen;
                buttonPop3Action.Text = RES.BTN_STOP;
            }
        }

        private void ButtonSmtpAction_Clicked()
        {
            if (!enableSmtpBox.Checked)
                return;

            if (Global.Instance.IsSmtpRun) {
                Global.Instance.StopSmtpService();
                buttonSmtpAction.ColorScheme = GuiApp.ColorRed;
                buttonSmtpAction.Text = RES.BTN_START;
            } else {
                Global.Instance.StartSmtpService();
                buttonSmtpAction.ColorScheme = GuiApp.ColorGreen;
                buttonSmtpAction.Text = RES.BTN_STOP;
            }
        }

        private void EnableSmtpBox_Toggled(bool b) {
            Toggled(b, Global.Instance.IsSmtpRun, Global.Instance.StopSmtpService);
            Global.Instance.Config.IsSmtpEnable = !b;
        }

        private void EnablePop3Box_Toggled(bool b) {
            Toggled(b, Global.Instance.IsPop3Run, Global.Instance.StopPop3Service);
            Global.Instance.Config.IsPop3Enable = !b;
        }

        private void EnablePop3PgpDecrypt_Toggled(bool b) =>
            Global.Instance.Config.IsIncommingPgpDecrypt = !b;

        private void EnablePop3DeleteAllMessages_Toggled(bool b) =>
            Global.Instance.Config.IsPop3DeleteAllMessages = !b;

        private void EnablePop3Log_Toggled(bool b) =>
            Global.Instance.Config.IsPop3Log = !b;

        private void EnableSharingSocket_Toggled(bool b) =>
            Global.Instance.Config.IsSharingSocket = !b;

        private void EnableSmtpAllOutPgpCrypt_Toggled(bool b) =>
            Global.Instance.Config.IsSmtpAllOutPgpCrypt = !b;

        private void EnableSmtpAllOutPgpSign_Toggled(bool b) =>
            Global.Instance.Config.IsSmtpAllOutPgpSign = !b;

        private void EnableSmtpLog_Toggled(bool b) =>
            Global.Instance.Config.IsSmtpLog = !b;

        private void EnableSmtpCheckFrom_Toggled(bool b) =>
            Global.Instance.Config.IsSmtpCheckFrom = !b;

        private void EnableSmtpDeliveryLocal_Toggled(bool b) =>
            Global.Instance.Config.IsSmtpDeliveryLocal = !b;

        private void EnableSaveAttachments_Toggled(bool b) =>
            Global.Instance.Config.IsSaveAttachments = !b;

        private void EnableAlwaysNewMessageId_Toggled(bool b) =>
            Global.Instance.Config.IsAlwaysNewMessageId = !b;

        private void EnableNewMessageSendImmediately_Toggled(bool b) =>
            Global.Instance.Config.IsAlwaysNewMessageId = !b;

        private void EnableModifyMessageDeliveredLocal_Toggled(bool b) =>
            Global.Instance.Config.IsModifyMessageDeliveredLocal = !b;

        private void EnableReceiveOnSend_Toggled(bool b) =>
            Global.Instance.Config.IsReceiveOnSendOnly = !b;

        private void EnableSmtpClientFakeIp_Toggled(bool b) =>
            Global.Instance.Config.IsSmtpClientFakeIp = !b;

        private void EnableImapClientMessagePurge_Toggled(bool b) =>
            Global.Instance.Config.IsImapClientMessagePurge = !b;

        private void Toggled(bool b, bool state, Action act)
        {
            if (state)
                act.Invoke();

            buttonPop3Action.Enabled = !b;
            buttonPop3Action.ColorScheme = b ? Colors.Base : (state ? GuiApp.ColorGreen : GuiApp.ColorRed);
            buttonPop3Action.Text = state ? RES.BTN_STOP : RES.BTN_START;
            System.Diagnostics.Debug.WriteLine($"\t\t{b} - {state}");
        }
        #endregion

        #region Load
        public async void Load() => _ = await Load_().ConfigureAwait(false);

        private async Task<bool> Load_() =>
            await Task.Run(async () => {
            try {
                int idx = -1;
                NetworkInterface[] a = NetworkInterface.GetAllNetworkInterfaces();
                adapters.AddRange(a.Select(x => x.Name));
                    if ((adapters.Count > 0) &&
                        !string.IsNullOrWhiteSpace(Global.Instance.Config.ServicesInterfaceName))
                        idx = adapters.IndexOf(Global.Instance.Config.ServicesInterfaceName);

                    Application.MainLoop.Invoke(() => {
                        hostPop3Box.SetSource(adapters);
                        hostSmtpBox.SetSource(adapters);
                        hostPop3Box.SelectedItem =
                        hostSmtpBox.SelectedItem = (idx >= 0) ? idx : 0;
                    });
                } catch (Exception ex) { ex.StatusBarError(); }
                try {
                    MenuItem[] mitems = await nameof(GuiServicesSettingsWindow).LoadMenuUrls().ConfigureAwait(false);
                    Application.MainLoop.Invoke(() => urlmenu.Children = mitems);
                } catch { }
                return true;
            });
        #endregion

        #region Forbiden Route List
        private void ForbidenRouteText_KeyUp(KeyEventEventArgs obj) {
            if ((obj != null) && (obj.KeyEvent.Key == Key.Enter))
                ForbidenRouteIp(true,true);
        }
        private void ForbidenRouteIp(bool b, bool auto = false) =>
                ForbidenList(
                    b, ForbidenRouteText,
                    listForbidenRouteView,
                    Global.Instance.Config.ForbidenRouteList, auto);

        private void ListForbidenRrouteView_SelectedItemChanged(ListViewItemEventArgs obj) =>
            SelectedListForbidenRroute(obj.Item);

        private void ListForbidenRrouteView_OpenSelectedItem(ListViewItemEventArgs obj) =>
            SelectedListForbidenRroute(obj.Item);

        private int __last_forbidenroute = -1;
        private void SelectedListForbidenRroute(int idx) {
            if ((__last_forbidenroute != idx) && (idx >= 0) && (idx < Global.Instance.Config.ForbidenRouteList.Count)) {
                ForbidenRouteText.Text = Global.Instance.Config.ForbidenRouteList[idx];
                __last_forbidenroute = idx;
            }
        }
        #endregion

        #region Forbiden Entry List
        private void ForbidenEntryText_KeyUp(KeyEventEventArgs obj) {
            if ((obj != null) && (obj.KeyEvent.Key == Key.Enter))
                ForbidenEntryIp(true, true);
        }
        private void ForbidenEntryIp(bool b, bool auto = false) =>
                ForbidenList(
                    b, forbidenEntryText,
                    listForbidenEntryView,
                    Global.Instance.Config.ForbidenEntryList, auto);

        private void ListForbidenEntryView_SelectedItemChanged(ListViewItemEventArgs obj) =>
            SelectedListForbidenEntry(obj.Item);

        private void ListForbidenEntryView_OpenSelectedItem(ListViewItemEventArgs obj) =>
            SelectedListForbidenEntry(obj.Item);

        private void EntrySelectType_SelectedItemChanged(SelectedItemChangedArgs obj) {
            if ((obj == null) || (obj.SelectedItem < 0) || (obj.SelectedItem > 1))
                return;
            Global.Instance.Config.IsAccessIpWhiteList = obj.SelectedItem > 0;
            entrySelectText.Text = EntryType();
            entrySelectText.ColorScheme = EntryTypeColor();
            EntryDefaultIP(Global.Instance.Config.IsAccessIpWhiteList);
        }

        private int __last_forbidenentry = -1;
        private void SelectedListForbidenEntry(int idx) {
            if ((__last_forbidenentry != idx) && (idx >= 0) && (idx < Global.Instance.Config.ForbidenEntryList.Count)) {
                forbidenEntryText.Text = Global.Instance.Config.ForbidenEntryList[idx];
                __last_forbidenentry = idx;
            }
        }

        private ustring EntryType() =>
            Global.Instance.Config.IsAccessIpWhiteList ? $" {RES.TAG_ALLOWED} " : $" {RES.TAG_FORBIDDEN} ";

        private ColorScheme EntryTypeColor() =>
            Global.Instance.Config.IsAccessIpWhiteList? GuiApp.ColorGreen : GuiApp.ColorRed;

        private void EntryDefaultIP(bool b) {
            string[] nets = new string [] { "10.0.0.0/8", "127.0.0.1/32", "172.16.0.0/12", "192.168.0.0/16" };
            foreach (string net in nets) {
                if (!b && Global.Instance.Config.ForbidenEntryList.Contains(net))
                    Global.Instance.Config.ForbidenEntryList.Remove(net);
                else if (b && !Global.Instance.Config.ForbidenEntryList.Contains(net))
                    Global.Instance.Config.ForbidenEntryList.Add(net);
            }
            listForbidenEntryView.SetSource(Global.Instance.Config.ForbidenEntryList);
            __last_forbidenentry = -1;
        }
        #endregion

        #region Forbiden List
        private void ForbidenList(bool b, TextField field, ListView view, List<string> list, bool auto) {

            string s = field.Text.ToString();
            if (string.IsNullOrWhiteSpace(s))
                return;

            do {
                bool isRemove = list.Contains(s);
                if (auto) {
                    if (!isRemove)
                        list.Add(s);
                    else {
                        list.Remove(s);
                        field.Text = string.Empty;
                    }
                    break;
                }
                if (b && !isRemove)
                    list.Add(s);
                else if (!b && isRemove) {
                    list.Remove(s);
                    field.Text = string.Empty;
                }

            } while (false);
            __last_forbidenroute =
            __last_forbidenentry = -1;
            view.SetSource(list);
        }

        private void ForbidenListSort(List<string> list, ListView view) {
            list.Sort();
            view.SetSource(list);
        }
        #endregion
    }
}

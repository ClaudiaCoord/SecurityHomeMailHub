/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeMailHub.Gui.ListSources;
using SecyrityMail;
using SecyrityMail.Vpn;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiVpnAccountWindow : WindowListManagerBase<VpnAccount>, IGuiWindow<GuiVpnAccountWindow>
    {
        private const int labelOffset = 14;
        private MenuBar GuiMenu { get; set; } = default;
        private MenuBarItem urlmenu { get; set; } = default;

        private Label pubkeyLabel { get; set; } = default;
        private Label privkeyLabel { get; set; } = default;
        private Label prekeyLabel { get; set; } = default;
        private Label addrLabel { get; set; } = default;
        private Label dnsLabel { get; set; } = default;
        private Label hostLabel { get; set; } = default;
        private Label ipsLabel { get; set; } = default;
        private Label aliveLabel { get; set; } = default;
        private Label mtuLabel { get; set; } = default;
        private Label expireLabel { get; set; } = default;
        private Label helpText { get; set; } = default;

        private TextField pubkeyText { get; set; } = default;
        private TextField privkeyText { get; set; } = default;
        private TextField prekeyText { get; set; } = default;
        private TextField addrText { get; set; } = default;
        private TextField dnsText { get; set; } = default;
        private TextField hostText { get; set; } = default;
        private TextField ipsText { get; set; } = default;
        private TextField aliveText { get; set; } = default;
        private TextField mtuText { get; set; } = default;

        private CheckBox expireBox { get; set; } = default;
        private CheckBox enableBox { get; set; } = default;
        private DateField expireDate { get; set; } = default;
        private FrameView frameInterface { get; set; } = default;
        private FrameView framePeer { get; set; } = default;
        private FrameView frameHelp { get; set; } = default;

        private bool isNotExpire { get; set; } = true;
        private DateTime expireStore { get; set; } = DateTime.MinValue;
        private GuiLinearLayot linearLayot { get; } = new();

        public GuiVpnAccountWindow() : base(RES.GUIVPN_TITLE1, "VPN", new string[] { ".conf", ".cnf", ".xml" })
        {
            linearLayot.Add("en", new List<GuiLinearData> {
                new GuiLinearData(2,  1, true),
                new GuiLinearData(10, 1, true),
                new GuiLinearData(23, 1, true),
                new GuiLinearData(37, 1, true),
                new GuiLinearData(47, 1, true),
                new GuiLinearData(2,  3, true),
                new GuiLinearData(11, 3, true),
                new GuiLinearData(21, 3, true),
                new GuiLinearData(32, 3, true),
                new GuiLinearData(43, 3, true)
            });
            linearLayot.Add("ru", new List<GuiLinearData> {
                new GuiLinearData(2,  1, true),
                new GuiLinearData(13, 1, true),
                new GuiLinearData(27, 1, true),
                new GuiLinearData(41, 1, true),
                new GuiLinearData(53, 1, true),
                new GuiLinearData(2,  3, true),
                new GuiLinearData(16, 3, true),
                new GuiLinearData(29, 3, true),
                new GuiLinearData(41, 3, true),
                new GuiLinearData(52, 3, true)
            });
        }

        public new void Dispose() {

            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Init
        public GuiVpnAccountWindow Init(string __)
        {
            int idx = 0;
            List<GuiLinearData> layout = linearLayot.GetDefault();
            Pos posright = Pos.Right(base.frameList),
                posbottom;

            #region frameList
            /* see WindowListManagerBase<T1> */
            #endregion

            #region frameInterface
            frameInterface = new FrameView(RES.TAG_INTERFACE)
            {
                X = Pos.Right(base.frameList) + 1,
                Y = 1,
                Width = 80,
                Height = 9
            };
            frameInterface.Add(privkeyLabel = new Label("Private Key: ")
            {
                X = 1,
                Y = 1,
                AutoSize = true
            });
            frameInterface.Add(privkeyText = new TextField(string.Empty)
            {
                X = labelOffset,
                Y = 1,
                Width = 50,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameInterface.Add(prekeyLabel = new Label("Shared Key: ")
            {
                X = 1,
                Y = 3,
                AutoSize = true
            });
            frameInterface.Add(prekeyText = new TextField(string.Empty)
            {
                X = labelOffset,
                Y = 3,
                Width = 50,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            frameInterface.Add(addrLabel = new Label("Address: ")
            {
                X = 1,
                Y = 5,
                AutoSize = true
            });
            frameInterface.Add(addrText = new TextField(string.Empty)
            {
                X = labelOffset,
                Y = 5,
                Width = 50,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            privkeyText.KeyUp += (_) => base.ButtonsEnable(false, !IsEmptyForm);
            Add(frameInterface);
            #endregion

            #region framePeer
            framePeer = new FrameView(RES.TAG_PEER)
            {
                X = posright + 1,
                Y = Pos.Bottom(frameInterface),
                Width = 80,
                Height = 11
            };
            framePeer.Add(pubkeyLabel = new Label("Public Key: ")
            {
                X = 1,
                Y = 1,
                AutoSize = true
            });
            framePeer.Add(pubkeyText = new TextField(string.Empty)
            {
                X = labelOffset,
                Y = 1,
                Width = 50,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            framePeer.Add(hostLabel = new Label("Endpoint: ")
            {
                X = 1,
                Y = 3,
                AutoSize = true
            });
            framePeer.Add(hostText = new TextField(string.Empty)
            {
                X = labelOffset,
                Y = 3,
                Width = 50,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            framePeer.Add(ipsLabel = new Label("Allowed IP: ")
            {
                X = 1,
                Y = 5,
                AutoSize = true
            });
            framePeer.Add(ipsText = new TextField(string.Empty)
            {
                X = labelOffset,
                Y = 5,
                Width = 50,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            framePeer.Add(aliveLabel = new Label("Keepalive: ")
            {
                X = 1,
                Y = 7,
                AutoSize = true
            });
            framePeer.Add(aliveText = new TextField(string.Empty)
            {
                X = labelOffset,
                Y = 7,
                Width = 5,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            framePeer.Add(mtuLabel = new Label("MTU: ")
            {
                X = labelOffset + 6,
                Y = 7,
                AutoSize = true
            });
            framePeer.Add(mtuText = new TextField(string.Empty)
            {
                X = labelOffset + 11,
                Y = 7,
                Width = 5,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            framePeer.Add(dnsLabel = new Label("DNS: ")
            {
                X = labelOffset + 17,
                Y = 7,
                AutoSize = true
            });
            framePeer.Add(dnsText = new TextField(string.Empty)
            {
                X = labelOffset + 22,
                Y = 7,
                Width = 28,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            Add(framePeer);
            posbottom = Pos.Bottom(framePeer);
            #endregion

            #region frameHelp
            frameHelp = new FrameView(RES.TAG_HELP)
            {
                X = Pos.Right(frameInterface) + 1,
                Y = 1,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill()
            };
            frameHelp.Add(helpText = new Label()
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill() - 1,
                ColorScheme = GuiApp.ColorDescription
            });
            Add(frameHelp);
            #endregion

            #region main Windows
            Add(expireLabel = new Label(RES.TAG_EXPIRE)
            {
                X = posright + layout[idx].X,
                Y = posbottom + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
            });
            Add(expireDate = new DateField(DateTime.Now)
            {
                X = posright + layout[idx].X,
                Y = posbottom + layout[idx++].Y,
                Width = 12,
                Height = 1,
                Enabled = !isNotExpire,
                ColorScheme = GuiApp.ColorField
            });
            Add(expireBox = new CheckBox(RES.CHKBOX_EXPIRE)
            {
                X = posright + layout[idx].X,
                Y = posbottom + layout[idx++].Y,
                Width = 10,
                Height = 1,
                Checked = isNotExpire
            });
            Add(enableBox = new CheckBox(RES.TAG_ENABLE)
            {
                X = posright + layout[idx].X,
                Y = posbottom + layout[idx++].Y,
                Width = 10,
                Height = 1,
                Checked = true
            });
            expireBox.Toggled += IsexpireBox_Toggled;
            enableBox.Toggled += EnableBox_Toggled;

            #region Buttons
            buttonPaste.SetLinearLayout(layout[idx++],  posright, posbottom);
            buttonSave.SetLinearLayout(layout[idx++],   posright, posbottom);
            buttonClear.SetLinearLayout(layout[idx++],  posright, posbottom);
            buttonDelete.SetLinearLayout(layout[idx++], posright, posbottom);
            buttonImport.SetLinearLayout(layout[idx++], posright, posbottom);
            buttonExport.SetLinearLayout(layout[idx++], posright, posbottom);

            Add(buttonPaste, buttonSave, buttonClear, buttonDelete, buttonImport, buttonExport);
            #endregion

            #endregion

            pubkeyText.KeyUp += (_) => base.ButtonsEnable(false, !IsEmptyForm);
            addrText.KeyUp += (_) => base.ButtonsEnable(false, !IsEmptyForm);
            hostText.KeyUp += (_) => base.ButtonsEnable(false, !IsEmptyForm);

            urlmenu = new MenuBarItem("_Url", new MenuItem[0]);
            GuiMenu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
                    new MenuItem (RES.MENU_RELOAD, "", async () => {
                        _ = await Global.Instance.VpnAccounts.Load().ConfigureAwait(false);
                        _ = await Global.Instance.VpnAccounts.RandomSelect().ConfigureAwait(false);
                        _ = await Loading().ConfigureAwait(false);
                    }, null, null, Key.AltMask | Key.R),
                    null,
                    new MenuItem (RES.MENU_CLOSE, "", () => Application.RequestStop(), null, null, Key.AltMask | Key.CursorLeft)
                }),
                new MenuBarItem (RES.MENU_ACTION, new MenuItem [] {
                    new MenuItem (string.Format(RES.GUIACCOUNT_FMT1, RES.TAG_ON, tag), string.Empty, async () => {
                        if (MessageBox.Query (50, 7,
                            string.Format(RES.GUIACCOUNT_FMT2, RES.TAG_ON),
                            string.Format(RES.GUIACCOUNT_FMT4, RES.TAG_ON, tag), RES.TAG_YES, RES.TAG_NO) == 0) {
                            try {
                                foreach(VpnAccount a in Global.Instance.VpnAccounts.Items)
                                    a.Enable = true;
                                _ = await Global.Instance.VpnAccounts.Save().ConfigureAwait(false);
                            } catch (Exception ex) { ex.StatusBarError(); }
                        }
                    }),
                    new MenuItem (string.Format(RES.GUIACCOUNT_FMT1, RES.TAG_OFF, tag), string.Empty, async () => {
                        if (MessageBox.Query (50, 7,
                            string.Format(RES.GUIACCOUNT_FMT2, RES.TAG_OFF),
                            string.Format(RES.GUIACCOUNT_FMT4, RES.TAG_OFF, tag), RES.TAG_YES, RES.TAG_NO) == 0) {
                            try {
                                foreach(VpnAccount a in Global.Instance.VpnAccounts.Items)
                                    a.Enable = false;
                                _ = await Global.Instance.VpnAccounts.Save().ConfigureAwait(false);
                            } catch (Exception ex) { ex.StatusBarError(); }
                        }
                    }),
                    new MenuItem (string.Format(RES.GUIACCOUNT_FMT1, RES.TAG_DELETE, tag), string.Empty, async () => {
                        if (MessageBox.Query (50, 7,
                            string.Format(RES.GUIACCOUNT_FMT2, RES.TAG_DELETE),
                            string.Format(RES.GUIACCOUNT_FMT4, RES.TAG_DELETE, tag), RES.TAG_YES, RES.TAG_NO) == 0) {
                            try {
                                base.DataClear();
                                Global.Instance.VpnAccounts.Clear();
                                _ = await Global.Instance.VpnAccounts.Save().ConfigureAwait(false);
                            } catch (Exception ex) { ex.StatusBarError(); }
                        }
                    })
                }),
                urlmenu
            });
            GuiToplevel.Add(GuiMenu, this);
            return this;
        }
        #endregion

        #region Load
        public async void Load() => _ = await Loading().ConfigureAwait(false);
        private async Task<bool> Loading() =>
            await Task.Run(async () => {
                if (!base.runOnce.Begin())
                    return false;
                try {
                    _ = await base.LoadAccounts(Global.Instance.VpnAccounts.Items).ConfigureAwait(false);
                    try {
                        MenuItem[] mitems = await nameof(GuiVpnAccountWindow).LoadMenuUrls().ConfigureAwait(false);
                        Application.MainLoop.Invoke(() => urlmenu.Children = mitems);
                    }
                    catch (Exception ex) { ex.StatusBarError(); }
                    Application.MainLoop.Invoke(() => helpText.Text = RES.GuiVpnAccountWindowHelp);
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { base.runOnce.End(); }
                return true;
            });
        #endregion

        #region Virtual override
        protected override bool IsEmptyForm =>
            string.IsNullOrWhiteSpace(pubkeyText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(privkeyText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(addrText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(hostText.Text.ToString());

        protected override void VirtualEnableToggled(bool b) {
            prekeyText.Enabled =
            privkeyText.Enabled =
            pubkeyText.Enabled =
            addrText.Enabled =
            dnsText.Enabled =
            hostText.Enabled =
            ipsText.Enabled =
            aliveText.Enabled =
            mtuText.Enabled =
            expireDate.Enabled = b;
        }

        protected override void VirtualClean() {
            prekeyText.Text =
            pubkeyText.Text =
            privkeyText.Text =
            addrText.Text =
            hostText.Text =
            aliveText.Text =
            mtuText.Text = string.Empty;

            dnsText.Text = VpnInterface.DnsDefault;
            ipsText.Text = VpnPeer.AllowedIPsDefaultBlock;

            enableBox.Checked = true;
            expireDate.Date = DateTime.Now.AddDays(7.0);
            isNotExpire = false;
            expireDate.Enabled = !isNotExpire;
            expireBox.Checked = isNotExpire;
            expireLabel.ColorScheme = Colors.Base;
            frameInterface.Title = RES.TAG_INTERFACE;
        }

        protected override async Task<bool> VirtualSaveAll() =>
            await Global.Instance.VpnAccounts.Save().ConfigureAwait(false);

        protected override void VirtualAddItem(VpnAccount acc) =>
            Global.Instance.VpnAccounts.Add(acc);

        protected override VpnAccount VirtualGetItem(string s) =>
            (from i in Global.Instance.VpnAccounts.Items
             where i.Name.Equals(s)
             select i).FirstOrDefault();

        protected override VpnAccount VirtualNewItem() {
            string host = hostText.Text.ToString();
            if (string.IsNullOrEmpty(host))
                return default;
            int idx = host.IndexOf(':');
            if (idx > 0)
                host = host.Substring(0, idx);
            VpnAccount a = new();
            a.Name = host;
            return a;
        }

        protected override void VirtualBuildItem(VpnAccount acc) {
            acc.Enable = enableBox.Checked;
            acc.Expired = expireDate.Date;

            acc.Interface.PrivateKey = privkeyText.Text.ToString();
            acc.Interface.Address = addrText.Text.ToString();
            acc.Interface.DNS = dnsText.Text.ToString();
            acc.Interface.MTU = mtuText.Text.ToString();

            acc.Peer.PresharedKey = prekeyText.Text.ToString();
            acc.Peer.PublicKey = pubkeyText.Text.ToString();
            acc.Peer.Endpoint = hostText.Text.ToString();
            acc.Peer.AllowedIPs = ipsText.Text.ToString();
            if (short.TryParse(aliveText.Text.ToString(), out short keepalive))
                acc.Peer.PersistentKeepalive = keepalive;
        }

        protected override void VirtualSelectItem(VpnAccount acc) {

            runOnce.ChangeId(acc.Name);

            Application.MainLoop.Invoke(() => {

                privkeyText.Text = acc.Interface.PrivateKey;
                prekeyText.Text = acc.Peer.PresharedKey;
                addrText.Text = acc.Interface.Address;
                dnsText.Text = acc.Interface.DNS;
                pubkeyText.Text = acc.Peer.PublicKey;
                hostText.Text = acc.Peer.Endpoint;
                ipsText.Text = acc.Peer.AllowedIPs;
                aliveText.Text = acc.Peer.PersistentKeepalive.ToString();
                mtuText.Text = acc.Interface.MTU.ToString();

                if (acc.IsExpired)
                    expireLabel.ColorScheme = GuiApp.ColorWarning;
                else
                    expireLabel.ColorScheme = Colors.Base;

                frameInterface.Title = string.IsNullOrWhiteSpace(acc.Name) ?
                    RES.TAG_INTERFACE : $"{RES.TAG_INTERFACE} :: {RES.TAG_ACCOUNT} {acc.Name}";


                enableBox.Checked = acc.Enable;
                base.EnableBox_Toggled(acc.Enable);
                expireStore = acc.Expired;
                expireDate.Date = expireStore;
                expireBox.Checked = acc.Expired == DateTime.MinValue;
                expireDate.Enabled = acc.Enable && !expireBox.Checked;
                ButtonsEnable(true, acc.Enable);
            });
        }

        protected override void VirtualDeleteItem(VpnAccount acc) =>
            Global.Instance.VpnAccounts.Items.Remove(acc);

        protected override async Task<VpnAccount> VirtualImportClipBoard(string s) {
            VpnAccount a = new();
            bool b = await a.ImportFromString(s)
                            .ConfigureAwait(false);
            return b ? a : null;
        }

        protected override async Task<VpnAccount> VirtualImportFile(string s) {
            VpnAccount a = new();
            bool b = await a.Import(s).ConfigureAwait(false);
            return b ? a : null;
        }

        protected override async Task<string> VirtualExport(VpnAccount acc, string s) =>
            await acc.Export(s).ConfigureAwait(false);
        #endregion

        #region Is expire box toggled
        private void IsexpireBox_Toggled(bool b) {
            isNotExpire = b;
            expireDate.Enabled = !b;
            if (b) {
                expireStore = expireDate.Date;
                expireDate.Date = DateTime.MinValue;
            } else {
                if (expireStore == DateTime.MinValue)
                    expireStore = DateTime.Now.AddDays(5.0);
                expireDate.Date = expireStore;
            }
        }
        #endregion
    }
}

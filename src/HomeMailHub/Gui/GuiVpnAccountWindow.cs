﻿/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeMailHub.Gui.Dialogs;
using SecyrityMail;
using SecyrityMail.Vpn;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiVpnAccountWindow : Window, IGuiWindow<GuiVpnAccountWindow>
    {
        private const int labelOffset = 14;
        private const string tag = "VPN";
        private static readonly string[] extension = new string[] { ".conf", ".cnf", ".xml" };

        private Toplevel GuiToplevel { get; set; } = default;
        private MenuBar GuiMenu { get; set; } = default;
        private MenuBarItem urlmenu { get; set; } = default;
        private ListView listView { get; set; } = default;
        private Button buttonSave { get; set; } = default;
        private Button buttonClear { get; set; } = default;
        private Button buttonDelete { get; set; } = default;
        private Button buttonImport { get; set; } = default;
        private Button buttonExport { get; set; } = default;
        private Button buttonPaste { get; set; } = default;

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
        private FrameView frameList { get; set; } = default;
        private FrameView frameInterface { get; set; } = default;
        private FrameView framePeer { get; set; } = default;
        private FrameView frameHelp { get; set; } = default;

        private bool isNotExpire { get; set; } = true;
        private DateTime expireStore { get; set; } = DateTime.MinValue;
        private string selectedName { get; set; } = string.Empty;
        private VpnAccount account { get; set; } = default;
        private GuiRunOnce runOnce = new();
        private List<string> data = new();
        private GuiLinearLayot linearLayot { get; } = new();

        public Toplevel GetTop => GuiToplevel;

        private bool IsEmptyForm =>
            string.IsNullOrWhiteSpace(pubkeyText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(privkeyText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(addrText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(hostText.Text.ToString());

        private void ButtonsEnable(bool b) =>
            buttonSave.Enabled = buttonClear.Enabled = buttonDelete.Enabled = buttonExport.Enabled = b;

        public GuiVpnAccountWindow() : base(RES.GUIVPN_TITLE1, 0)
        {
            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill() - 1;
            GuiToplevel = GuiExtensions.CreteTop();

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
                new GuiLinearData(43, 3, true),
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
                new GuiLinearData(42, 3, true),
                new GuiLinearData(53, 3, true),
            });
        }

        public new void Dispose() {

            account = default;
            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Init
        public GuiVpnAccountWindow Init(string __)
        {
            int idx = 0;
            List<GuiLinearData> layout = linearLayot.GetDefault();

            frameList = new FrameView(RES.TAG_ACCOUNTS)
            {
                X = 1,
                Y = 1,
                Width = 35,
                Height = Dim.Fill()
            };
            frameInterface = new FrameView("Interface")
            {
                X = Pos.Right(frameList) + 1,
                Y = 1,
                Width = 80,
                Height = 9
            };
            framePeer = new FrameView("Peer")
            {
                X = Pos.Right(frameList) + 1,
                Y = Pos.Bottom(frameInterface),
                Width = 80,
                Height = 11
            };
            frameHelp = new FrameView(RES.TAG_HELP)
            {
                X = Pos.Right(frameInterface) + 1,
                Y = 1,
                Width = Dim.Fill() - 1,
                Height = Dim.Fill()
            };

            #region frameList
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

            frameList.Add(listView);
            Add(frameList);
            #endregion

            #region frameInterface
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
            privkeyText.KeyUp += (_) => ButtonsEnable(!IsEmptyForm);
            Add(frameInterface);
            #endregion

            #region framePeer
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
            #endregion

            #region frameHelp
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
                X = Pos.Right(frameList) + layout[idx].X,
                Y = Pos.Bottom(framePeer) + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
            });
            Add(expireDate = new DateField(DateTime.Now)
            {
                X = Pos.Right(frameList) + layout[idx].X,
                Y = Pos.Bottom(framePeer) + layout[idx++].Y,
                Width = 12,
                Height = 1,
                Enabled = !isNotExpire,
                ColorScheme = GuiApp.ColorField
            });
            Add(expireBox = new CheckBox(RES.CHKBOX_EXPIRE)
            {
                X = Pos.Right(frameList) + layout[idx].X,
                Y = Pos.Bottom(framePeer) + layout[idx++].Y,
                Width = 10,
                Height = 1,
                Checked = isNotExpire
            });
            Add(enableBox = new CheckBox(RES.TAG_ENABLE)
            {
                X = Pos.Right(frameList) + layout[idx].X,
                Y = Pos.Bottom(framePeer) + layout[idx++].Y,
                Width = 10,
                Height = 1,
                Checked = true
            });
            Add(buttonPaste = new Button(RES.BTN_PASTE)
            {
                X = Pos.Right(frameList) + layout[idx].X,
                Y = Pos.Bottom(framePeer) + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
            });
            expireBox.Toggled += IsexpireBox_Toggled;
            enableBox.Toggled += EnableBox_Toggled;

            Add(buttonSave = new Button(RES.BTN_SAVE)
            {
                X = Pos.Right(frameList) + layout[idx].X,
                Y = Pos.Bottom(framePeer) + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                TabIndex = 13
            });
            Add(buttonClear = new Button(RES.BTN_CLEAR)
            {
                X = Pos.Right(frameList) + layout[idx].X,
                Y = Pos.Bottom(framePeer) + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                Enabled = false,
                TabIndex = 14
            });
            Add(buttonDelete = new Button(RES.BTN_DELETE)
            {
                X = Pos.Right(frameList) + layout[idx].X,
                Y = Pos.Bottom(framePeer) + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                Enabled = false,
                TabIndex = 15
            });
            Add(buttonImport = new Button(RES.BTN_IMPORT)
            {
                X = Pos.Right(frameList) + layout[idx].X,
                Y = Pos.Bottom(framePeer) + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                TabIndex = 16
            });
            Add(buttonExport = new Button(RES.BTN_EXPORT)
            {
                X = Pos.Right(frameList) + layout[idx].X,
                Y = Pos.Bottom(framePeer) + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                Enabled = false,
                TabIndex = 17
            });
            #endregion

            buttonSave.Clicked += () => SaveItem();
            buttonClear.Clicked += () => Clean();
            buttonDelete.Clicked += () => Delete();
            buttonPaste.Clicked += async () => await FromClipBoard().ConfigureAwait(false);
            buttonImport.Clicked += async () =>
            {
                GuiOpenDialog d = string.Format(RES.GUIACCOUNT_FMT6, RES.TAG_OPEN_IMPORT, tag).GuiOpenDialogs(true, extension);
                Application.Run(d);
                if (!d.Canceled) {
                    try {
                        string[] ss = d.GuiReturnDialog();
                        if (ss.Length > 0) {
                            foreach (string s in ss) {
                                try {
                                    VpnAccount a = new();
                                    bool b = await a.Import(s).ConfigureAwait(false);
                                    AddItem(a, b);
                                } catch (Exception ex) { ex.StatusBarError(); }
                            }
                            _ = await Global.Instance.VpnAccounts.Save().ConfigureAwait(false);
                        }
                    } catch (Exception ex) { ex.StatusBarError(); }
                }
            };
            buttonExport.Clicked += async () =>
            {
                if (account == default) {
                    if (string.IsNullOrEmpty(selectedName))
                        return;

                    account = (from i in Global.Instance.VpnAccounts.Items
                               where i.Name.Equals(selectedName)
                               select i).FirstOrDefault();
                    if (account == default)
                        return;
                }
                GuiSaveDialog d = string.Format(RES.GUIACCOUNT_FMT6, RES.TAG_SAVE_EXPORT, selectedName).GuiSaveDialogs(extension);
                Application.Run(d);
                if (!d.Canceled) {
                    try {
                        string[] ss = d.GuiReturnDialog();
                        if (ss.Length > 0)
                            _ = await account.Export(ss[0]).ConfigureAwait(false);
                    } catch (Exception ex) { ex.StatusBarError(); }
                }
            };
            pubkeyText.KeyUp += (_) => ButtonsEnable(!IsEmptyForm);
            addrText.KeyUp += (_) => ButtonsEnable(!IsEmptyForm);
            hostText.KeyUp += (_) => ButtonsEnable(!IsEmptyForm);
            Add(framePeer);

            urlmenu = new MenuBarItem("_Url", new MenuItem[0]);
            GuiMenu = new MenuBar(new MenuBarItem[] {
                new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
                    new MenuItem (RES.MENU_RELOAD, "", async () => {
                        _ = await Global.Instance.VpnAccounts.Load().ConfigureAwait(false);
                        _ = await Global.Instance.VpnAccounts.RandomSelect().ConfigureAwait(false);
                        _ = await Load_().ConfigureAwait(false);
                    }, null, null, Key.AltMask | Key.R),
                    null,
                    new MenuItem (RES.MENU_CLOSE, "", () => Application.RequestStop(), null, null, Key.AltMask | Key.Q)
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
                                DataClear();
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
        public async void Load() => _ = await Load_().ConfigureAwait(false);
        private async Task<bool> Load_() =>
            await Task.Run(async () => {
                if (!runOnce.GoRun())
                    return false;
                try {
                    _ = await LoadVpnAccounts_().ConfigureAwait(false);
                    try {
                        MenuItem[] mitems = await nameof(GuiVpnAccountWindow).LoadMenuUrls().ConfigureAwait(false);
                        Application.MainLoop.Invoke(() => urlmenu.Children = mitems);
                    } catch { }
                }
                finally { runOnce.EndRun(); }
                return true;
            });

        private async Task<bool> LoadVpnAccounts_() =>
            await Task.Run(async () => {
                try {
                    DataClear();
                    foreach (VpnAccount a in Global.Instance.VpnAccounts.Items)
                        data.Add(a.Name);
                    await listView.SetSourceAsync(data).ConfigureAwait(false);
                    Application.MainLoop.Invoke(() =>
                        frameList.Title = (data.Count == 0) ?
                            string.Empty.GetListTitle(0) : selectedName.GetListTitle(data.Count)
                    );
                }
                catch (Exception ex) { ex.StatusBarError(); }
                return true;
            });

        private async Task<bool> FromClipBoard() =>
            await Task.Run(async () => {
                try {

                    if (Clipboard.Contents.IsEmpty)
                        return false;

                    Clean();
                    VpnAccount acc = new();
                    bool b = await acc.ImportFromString(Clipboard.Contents.ToString())
                                      .ConfigureAwait(false);
                    if (b) {
                        SelectItem(acc);
                        return true;
                    }
                }
                catch (Exception ex) { ex.StatusBarError(); }
                return false;
            });
        #endregion

        #region Delete
        private async void Delete() {

            if (!runOnce.IsRange(data.Count) || !runOnce.GoRun())
                return;

            try {
                string s = data[runOnce.LastId];
                if (string.IsNullOrWhiteSpace(s))
                    return;

                if (MessageBox.Query(50, 7,
                    string.Format(RES.GUIACCOUNT_FMT5, RES.TAG_DELETE, s),
                    string.Format(RES.GUIACCOUNT_FMT3, RES.TAG_DELETE, s), RES.TAG_YES, RES.TAG_NO) == 0) {
                    try {
                        VpnAccount a = (from i in Global.Instance.VpnAccounts.Items
                                        where i.Name.Equals(s)
                                        select i).FirstOrDefault();
                        if (a == default)
                            return;

                        DataRemove(a.Name);
                        Global.Instance.VpnAccounts.Items.Remove(a);
                        _ = await Global.Instance.VpnAccounts.Save().ConfigureAwait(false);
                        _ = await LoadVpnAccounts_().ConfigureAwait(false);
                    }
                    catch (Exception ex) { ex.StatusBarError(); }
                }
            }
            catch (Exception ex) { ex.StatusBarError(); }
            finally { runOnce.EndRun(); }
        }
        #endregion

        private void DataClear() {
            data.Clear();
            Clean();
            Application.MainLoop.Invoke(() => {
                frameList.Title = string.Empty.GetListTitle(0);
                listView.SetNeedsDisplay();
            });
        }
        private void DataRemove(string s) {
            data.Remove(s);
            Clean();
            Application.MainLoop.Invoke(() => {
                frameList.Title = string.Empty.GetListTitle(data.Count);
                listView.SetNeedsDisplay();
            });
        }
        private void Clean() {
            Application.MainLoop.Invoke(() =>
            {
                prekeyText.Text =
                pubkeyText.Text =
                privkeyText.Text =
                addrText.Text =
                hostText.Text =
                aliveText.Text =
                mtuText.Text = string.Empty;

                if (account != default) {
                    account.Interface.SetDefault();
                    account.Peer.SetDefault();
                    dnsText.Text = account.Interface.DNS;
                    ipsText.Text = account.Peer.AllowedIPs;
                } else {
                    dnsText.Text =
                    ipsText.Text = string.Empty;
                }
                enableBox.Checked = true;
                expireDate.Date = DateTime.Now.AddDays(7.0);
                isNotExpire = false;
                expireDate.Enabled = !isNotExpire;
                expireBox.Checked = isNotExpire;
                expireLabel.ColorScheme = Colors.Base;
                ButtonsEnable(false);
            });
            runOnce.ResetId();
        }

        private void EnableBox_Toggled(bool b) =>
            Application.MainLoop.Invoke(() => {
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
                buttonClear.Enabled =
                buttonDelete.Enabled =
                buttonExport.Enabled = !IsEmptyForm && b;
                buttonImport.Enabled =
                buttonSave.Enabled = true;
            });

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

        private void ListView_OpenSelectedItem(ListViewItemEventArgs obj) => SelectedListItem(obj);
        private void ListView_SelectedItemChanged(ListViewItemEventArgs obj) => SelectedListItem(obj);
        private void SelectedListItem(ListViewItemEventArgs obj) {
            if (obj == null)
                return;
            if ((obj.Item >= 0) && (obj.Item < data.Count))
                SelectItem(data[obj.Item], obj.Item);
        }

        private void SelectItem(string s, int id) {
            if (string.IsNullOrEmpty(s) || !runOnce.GoRun(id))
                return;

            try {
                VpnAccount acc = (from i in Global.Instance.VpnAccounts.Items
                                where i.Name.Equals(s)
                                select i).FirstOrDefault();
                if (acc == default) return;
                selectedName = s;
                SelectItem(acc);
            } finally { runOnce.EndRun(); }
        }

        private void SelectItem(VpnAccount acc) {
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

                enableBox.Checked = acc.Enable;
                EnableBox_Toggled(acc.Enable);
                expireStore = acc.Expired;
                expireDate.Date = expireStore;
                expireBox.Checked = acc.Expired == DateTime.MinValue;
                expireDate.Enabled = acc.Enable && !expireBox.Checked;
                account = acc;
                ButtonsEnable(true);
            });
        }

        private async void SaveItem() {

            if (!runOnce.GoRun())
                return;

            try {
                bool b = string.IsNullOrEmpty(selectedName);
                if (b) {
                    VpnAccount a = NewItem();
                    if (a == default)
                        return;
                    BuildItem(a);
                    AddItem(a, b);
                    account = a;
                    await Global.Instance.VpnAccounts.Save().ConfigureAwait(false);
                } else {
                    VpnAccount a = (from i in Global.Instance.VpnAccounts.Items
                                    where i.Name.Equals(selectedName)
                                    select i).FirstOrDefault();
                    if (a == default) {
                        b = true;
                        a = NewItem();
                        if (a == default)
                            return;
                    }
                    BuildItem(a);
                    AddItem(a, b);
                    account = a;
                    await Global.Instance.VpnAccounts.Save().ConfigureAwait(false);
                }
            } finally { runOnce.EndRun(); }
        }

        private VpnAccount NewItem() {

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

        private void BuildItem(VpnAccount a)
        {
            a.Enable = enableBox.Checked;
            a.Expired = expireDate.Date;

            a.Interface.PrivateKey = privkeyText.Text.ToString();
            a.Interface.Address = addrText.Text.ToString();
            a.Interface.DNS = dnsText.Text.ToString();
            a.Interface.MTU = mtuText.Text.ToString();

            a.Peer.PresharedKey = prekeyText.Text.ToString();
            a.Peer.PublicKey = pubkeyText.Text.ToString();
            a.Peer.Endpoint = hostText.Text.ToString();
            a.Peer.AllowedIPs = ipsText.Text.ToString();
            if (short.TryParse(aliveText.Text.ToString(), out short keepalive))
                a.Peer.PersistentKeepalive = keepalive;
        }

        private async void AddItem(VpnAccount a, bool b) {
            if (!b) return;
            try {
                Global.Instance.VpnAccounts.Add(a);
                data.Add(a.Name);
                await listView.SetSourceAsync(data).ConfigureAwait(false);
            } catch (Exception ex) { ex.StatusBarError(); }
        }
    }
}

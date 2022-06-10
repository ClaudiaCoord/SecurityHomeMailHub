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
using NStack;
using SecyrityMail;
using SecyrityMail.Proxy;
using SecyrityMail.Proxy.SshProxy;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiSshAccountWindow : WindowListManagerBase<SshAccount>, IGuiWindow<GuiSshAccountWindow>
    {
		private const int labelOffset = 12;
		private static readonly ustring[] proxyopt = new ustring[] { "None", "SshSock4", "SshSock5" };
		private static ustring GetInTitle(ProxyType type = ProxyType.None) =>
			(type == ProxyType.SshSock4) ? proxyopt[1] :
				((type == ProxyType.SshSock5) ? proxyopt[2] : proxyopt[0]);

		private MenuBar GuiMenu { get; set; } = default;
		private MenuBarItem urlmenu { get; set; } = default;
		private FrameView frameForm { get; set; } = default;
        private FrameView frameHelp { get; set; } = default;

		private Label nameLabel { get; set; } = default;
		private Label loginLabel { get; set; } = default;
		private Label passLabel { get; set; } = default;
		private Label hostLabel { get; set; } = default;
		private Label portLabel { get; set; } = default;
		private Label proxyLabel { get; set; } = default;
		private Label expireLabel { get; set; } = default;
        private Label helpText { get; set; } = default;

        private TextField nameText { get; set; } = default;
		private TextField loginText { get; set; } = default;
		private TextField passText { get; set; } = default;
		private TextField hostText { get; set; } = default;
		private TextField portText { get; set; } = default;
		private ComboBox  proxyType { get; set; } = default;

		private DateField expireDate { get; set; } = default;
		private CheckBox  expireBox { get; set; } = default;
		private CheckBox  enableBox { get; set; } = default;
        private ColorScheme ColorWarning { get; set; } = default;

        private bool isNotExpire { get; set; } = true;
		private DateTime expireStore { get; set; } = DateTime.MinValue;
        private GuiLinearLayot linearLayot { get; } = new();

        public GuiSshAccountWindow () : base(RES.GUISSH_TITLE1, "SSH", new string[] { ".conf", ".cnf", ".xml" })
		{
            linearLayot.Add("en", new List<GuiLinearData> {
                new GuiLinearData(53, 7, true),

                new GuiLinearData(2, 1, true),
                new GuiLinearData(12, 1, true),
                new GuiLinearData(27, 1, true),
                new GuiLinearData(41, 1, true),

                new GuiLinearData(2,  3, true),
                new GuiLinearData(11, 3, true),
                new GuiLinearData(21, 3, true),
                new GuiLinearData(32, 3, true),
                new GuiLinearData(43, 3, true),
            });
            linearLayot.Add("ru", new List<GuiLinearData> {
                new GuiLinearData(50, 7, true),

                new GuiLinearData(2, 1, true),
                new GuiLinearData(12, 1, true),
                new GuiLinearData(27, 1, true),
                new GuiLinearData(41, 1, true),

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
		public GuiSshAccountWindow Init(string __)
		{
			int idx = 0;
            List<GuiLinearData> layout = linearLayot.GetDefault();
            Pos posright = Pos.Right(base.frameList),
                posbottom;

            #region frameList
            /* see WindowListManagerBase<T1> */
            #endregion

            #region frameForm
            frameForm = new FrameView($"{RES.TAG_ACCOUNT} {GetInTitle()}")
            {
                X = Pos.Right(frameList) + 1,
                Y = 1,
				Width = 80,
				Height = 15
            };
			posbottom = Pos.Bottom(frameForm);
            frameForm.Add (loginLabel = new Label (RES.TAG_LOGIN) {
				X = 1,
				Y = 1,
				AutoSize = true
			});
			frameForm.Add (loginText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 1,
				Width = 50,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameForm.Add (passLabel = new Label (RES.TAG_PASSWORD) {
				X = 1,
				Y = 3,
				AutoSize = true
			});
			frameForm.Add (passText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 3,
				Width = 50,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameForm.Add (hostLabel = new Label (RES.TAG_HOST) {
				X = 1,
				Y = 5,
				AutoSize = true
			});
			frameForm.Add (hostText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 5,
				Width = 50,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameForm.Add (portLabel = new Label (RES.TAG_PORT) {
				X = 1,
				Y = 7,
				AutoSize = true
			});
			frameForm.Add (portText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 7,
				Width = 10,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameForm.Add(proxyLabel = new Label(RES.TAG_TYPE)
			{
				X = 1,
				Y = 9,
				AutoSize = true
			});
			frameForm.Add(proxyType = new ComboBox()
			{
				X = labelOffset,
				Y = 9,
				Width = 50,
				Height = 4,
				ReadOnly = true,
				ColorScheme = GuiApp.ColorField
			});
			proxyType.SetSource(proxyopt.ToList());
			frameForm.Add(nameLabel = new Label(RES.TAG_NAME)
			{
				X = 1,
				Y = 11,
				AutoSize = true
			});
			frameForm.Add(nameText = new TextField(string.Empty)
			{
				X = labelOffset,
				Y = 11,
				Width = 50,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});

            nameText.KeyUp += (_) => base.ButtonsEnable(false, !IsEmptyForm);
            loginText.KeyUp += (_) => base.ButtonsEnable(false, !IsEmptyForm);
            passText.KeyUp += (_) => base.ButtonsEnable(false, !IsEmptyForm);

            buttonPaste.SetLinearLayout(layout[idx++], 0, 0);
            frameForm.Add(buttonPaste);
            Add(frameForm);
            #endregion

            #region Main window
            Add(expireLabel = new Label(RES.TAG_EXPIRE)
            {
                X = posright + layout[idx].X,
                Y = posbottom + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
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
            expireBox.Toggled += IsExpireBox_Toggled;
            enableBox.Toggled += EnableBox_Toggled;

            buttonSave.SetLinearLayout(layout[idx++],   posright, posbottom);
            buttonClear.SetLinearLayout(layout[idx++],  posright, posbottom);
            buttonDelete.SetLinearLayout(layout[idx++], posright, posbottom);
            buttonImport.SetLinearLayout(layout[idx++], posright, posbottom);
            buttonExport.SetLinearLayout(layout[idx++], posright, posbottom);

            Add(buttonSave, buttonClear, buttonDelete, buttonImport, buttonExport);
            #endregion

            #region frameHelp
            frameHelp = new FrameView(RES.TAG_HELP)
            {
                X = Pos.Right(frameForm) + 1,
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

			urlmenu = new MenuBarItem("_Url", new MenuItem[0]);
			GuiMenu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
					new MenuItem (RES.MENU_RELOAD, "", async () => {
						_ = await Global.Instance.SshProxy.Load().ConfigureAwait(false);
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
								foreach(SshAccount a in Global.Instance.SshProxy.Items)
									a.Enable = true;
								_ = await Global.Instance.SshProxy.Save().ConfigureAwait(false);
							} catch (Exception ex) { ex.StatusBarError(); }
						}
					}),
					new MenuItem (string.Format(RES.GUIACCOUNT_FMT1, RES.TAG_OFF, tag), string.Empty, async () => {
						if (MessageBox.Query (50, 7,
							string.Format(RES.GUIACCOUNT_FMT2, RES.TAG_OFF),
							string.Format(RES.GUIACCOUNT_FMT4, RES.TAG_OFF, tag), RES.TAG_YES, RES.TAG_NO) == 0) {
							try {
								foreach(SshAccount a in Global.Instance.SshProxy.Items)
									a.Enable = false;
								_ = await Global.Instance.SshProxy.Save().ConfigureAwait(false);
							} catch (Exception ex) { ex.StatusBarError(); }
						}
					}),
					new MenuItem (string.Format(RES.GUIACCOUNT_FMT1, RES.MENU_DELETE, tag), string.Empty, async () => {
						if (MessageBox.Query (50, 7,
							string.Format(RES.GUIACCOUNT_FMT2, RES.TAG_DELETE),
							string.Format(RES.GUIACCOUNT_FMT4, RES.TAG_DELETE, tag), RES.TAG_YES, RES.TAG_NO) == 0) {
							try {
                                DataClear();
                                Global.Instance.SshProxy.Clear();
								 _ = await Global.Instance.SshProxy.Save().ConfigureAwait(false);
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
                    _ = await base.LoadAccounts(Global.Instance.SshProxy.Items).ConfigureAwait(false);
                    try {
                        MenuItem[] mitems = await nameof(GuiSshAccountWindow).LoadMenuUrls().ConfigureAwait(false);
                        Application.MainLoop.Invoke(() => urlmenu.Children = mitems);
                    } catch (Exception ex) { ex.StatusBarError(); }
                    Application.MainLoop.Invoke(() => helpText.Text = RES.GuiSshAccountWindowHelp);
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { base.runOnce.End(); }
                return true;
			});
        #endregion

        #region Virtual override
        protected override bool IsEmptyForm =>
            string.IsNullOrWhiteSpace(loginText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(passText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(hostText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(nameText.Text.ToString());

        protected override void VirtualEnableToggled(bool b) {
            loginText.Enabled =
            passText.Enabled =
            hostText.Enabled =
            portText.Enabled =
            nameText.Enabled =
            expireDate.Enabled = b;
        }

        protected override void VirtualClean() {
            nameText.Text =
            passText.Text =
            portText.Text =
            hostText.Text =
            loginText.Text = string.Empty;
            proxyType.SelectedItem = 0;
            enableBox.Checked = false;

            isNotExpire = false;
            expireDate.Date = DateTime.Now.AddDays(7.0);
            expireDate.Enabled = !isNotExpire;
            expireBox.Checked = isNotExpire;
            expireLabel.ColorScheme = Colors.Base;
            frameForm.Title = RES.TAG_ACCOUNT;
        }

        protected override async Task<bool> VirtualSaveAll() =>
            await Global.Instance.SshProxy.Save().ConfigureAwait(false);

        protected override void VirtualAddItem(SshAccount acc) =>
            Global.Instance.SshProxy.Add(acc);

        protected override SshAccount VirtualGetItem(string s) =>
            (from i in Global.Instance.SshProxy.Items
             where i.Name.Equals(s)
             select i).FirstOrDefault();

        protected override SshAccount VirtualNewItem() {
            string host = loginText.Text.ToString();
            if (string.IsNullOrEmpty(host))
                return default;
            int idx = host.IndexOf(':');
            if (idx > 0)
                host = host.Substring(0, idx);
            SshAccount a = new();
            a.Name = host;
            return a;
        }

        protected override void VirtualBuildItem(SshAccount acc) =>
            Application.MainLoop.Invoke(() => {
                acc.Enable = enableBox.Checked;
                acc.Expired = expireDate.Date;

                acc.Login = loginText.Text.ToString();
                acc.Pass = passText.Text.ToString();
                acc.Host = hostText.Text.ToString();
                acc.Name = nameText.Text.ToString();
                acc.Type = ProxyTypeSelect(proxyType.SelectedItem);
                if (int.TryParse(portText.Text.ToString(), out int port))
                    acc.Port = port;
            });

        protected override void VirtualSelectItem(SshAccount acc) {

            runOnce.ChangeId(acc.Name);

            Application.MainLoop.Invoke(() => {

                nameText.Text = acc.Name;
                loginText.Text = acc.Login;
                passText.Text = acc.Pass;
                hostText.Text = acc.Host;
                portText.Text = acc.Port.ToString();
                proxyType.SelectedItem = ProxyTypeSelect(acc.Type);
                frameForm.Title = string.IsNullOrWhiteSpace(acc.Name) ?
                    $"{RES.TAG_ACCOUNT} {GetInTitle(acc.Type)}" : $"{RES.TAG_ACCOUNT} {GetInTitle(acc.Type)} - {acc.Name}";

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
                ButtonsEnable(true, acc.Enable);
            });
        }

        protected override void VirtualDeleteItem(SshAccount acc) =>
            Global.Instance.SshProxy.Items.Remove(acc);

        protected override async Task<SshAccount> VirtualImportClipBoard(string s) =>
            await Task.Run(() => {
                SshAccountConverter converter = new(s);
                return converter.Convert() ? converter.Account : null;
            });

        protected override async Task<SshAccount> VirtualImportFile(string s) {
            SshAccount a = new();
            bool b = await a.Load(s).ConfigureAwait(false);
            return b ? a : null;
        }

		protected override async Task<string> VirtualExport(SshAccount acc, string s) {
			bool b = await acc.Save(s).ConfigureAwait(false);
			if (!b) return "Export error..";
			return string.Empty;
		}
        #endregion

        #region Is expire box toggled
        private void IsExpireBox_Toggled(bool b) =>
			Application.MainLoop.Invoke(() => {
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
			});
        #endregion

        #region Proxy type select
        private int ProxyTypeSelect(ProxyType opt) =>
			opt switch
			{
				ProxyType.None => 0,
				ProxyType.SshSock4 => 1,
				ProxyType.SshSock5 => 2,
				_ => 0
			};

		private ProxyType ProxyTypeSelect(int idx) =>
			idx switch
			{
				0 => ProxyType.None,
				1 => ProxyType.SshSock4,
				2 => ProxyType.SshSock5,
				_ => ProxyType.None
			};
        #endregion
    }
}

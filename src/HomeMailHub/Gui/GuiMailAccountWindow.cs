/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeMailHub.Gui.Dialogs;
using HomeMailHub.Gui.ListSources;
using MailKit.Security;
using NStack;
using SecyrityMail;
using SecyrityMail.GnuPG;
using SecyrityMail.MailAccounts;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public enum InMailType : int {
		None = 0,
		IMAP,
		POP3
    }

	public class GuiMailAccountWindow : WindowListManagerBase<UserAccount>, IGuiWindow<GuiMailAccountWindow>
    {
		private const int labelOffset = 10;
        private readonly object __pgpLock = new();
        private static readonly string [] extpgp = new string [] { ".key", ".public", ".private", ".asc" };
		private static readonly ustring [] ssltlsopt = new ustring [] { "None", "SslOnConnect", "StartTls", "Auto" };
		private static readonly ustring [] typeopt = new ustring [] { "_POP3", "_IMAP" };
		private static ustring GetInTitle(InMailType type = InMailType.None) =>
			((type == InMailType.None) ? $"{typeopt[0]}/{typeopt[1]}" :
				((type == InMailType.IMAP) ? typeopt[1] : typeopt[0])).Replace("_", "");

		private MenuBar  GuiMenu { get; set; } = default;
        private MenuBarItem urlmenu { get; set; } = default;

        private Button buttonPgpImport { get; set; } = default;
        private Button buttonPgpExport { get; set; } = default;
        private Button buttonPgpCreate { get; set; } = default;

        private Label hostInLabel { get; set; } = default;
		private Label portInLabel { get; set; } = default;
		private Label hostOutLabel { get; set; } = default;
		private Label portOutLabel { get; set; } = default;

		private TextField hostInText { get; set; } = default;
		private TextField portInText { get; set; } = default;
		private TextField hostOutText { get; set; } = default;
		private TextField portOutText { get; set; } = default;

		private Label loginLabel { get; set; } = default;
		private Label passLabel { get; set; } = default;
		private Label emailLabel { get; set; } = default;
		private Label nameLabel { get; set; } = default;

		private TextField loginText { get; set; } = default;
		private TextField passText { get; set; } = default;
		private TextField emailText { get; set; } = default;
		private TextField nameText { get; set; } = default;

		private FrameView frameIn { get; set; } = default;
		private FrameView frameOut { get; set; } = default;
        private FrameView framePgp { get; set; } = default;
        private FrameView frameUser { get; set; } = default;
        private FrameView frameHelp { get; set; } = default;

        private Label tlsInLabel { get; set; } = default;
		private Label tlsOutLabel { get; set; } = default;
        private Label helpText { get; set; } = default;

        private Label pgpKeyCryptLabel { get; set; } = default;
        private Label pgpKeyDecryptLabel { get; set; } = default;
        private Label pgpKeySignLabel { get; set; } = default;
        private Label pgpKeyCountLabel { get; set; } = default;
        private Label pgpKeyIdLabel { get; set; } = default;

        private Label pgpKeyCryptText { get; set; } = default;
        private Label pgpKeyDecryptText { get; set; } = default;
        private Label pgpKeySignText { get; set; } = default;
        private Label pgpKeyCountText { get; set; } = default;
        private Label pgpKeyIdText { get; set; } = default;

        private ComboBox tlsInText { get; set; } = default;
		private ComboBox tlsOutText { get; set; } = default;
		private CheckBox enableBox { get; set; } = default;
        private CheckBox pgpAutoBox { get; set; } = default;
        private RadioGroup mailInType { get; set; } = default;

		private InMailType inMailType { get; set; } = InMailType.None;
        private GuiLinearLayot linearLayot { get; } = new();
		private AccountGpgKeys accountGpg { get; set; } = default;

        public GuiMailAccountWindow() : base(RES.GUIMAIL_TITLE1, "User", new string[] { ".conf", ".cnf", ".xml" })
		{
            linearLayot.Add("en", new List<GuiLinearData> {
                new GuiLinearData(1,  7, true),
                new GuiLinearData(12, 7, true),
                new GuiLinearData(2,  1, true),
                new GuiLinearData(14, 1, true),
                new GuiLinearData(2,  3, true),
                new GuiLinearData(11, 3, true),
                new GuiLinearData(21, 3, true),
                new GuiLinearData(32, 3, true),
                new GuiLinearData(43, 3, true)
            });
            linearLayot.Add("ru", new List<GuiLinearData> {
                new GuiLinearData(1,  7, true),
                new GuiLinearData(12, 7, true),
                new GuiLinearData(2,  1, true),
                new GuiLinearData(14, 1, true),
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
		public GuiMailAccountWindow Init(string __)
		{
			int idx = 0;
            List<GuiLinearData> layout = linearLayot.GetDefault();
            Pos posright = Pos.Right(base.frameList),
                posbottom;

            #region frameList
            /* see WindowListManagerBase<T1> */
            #endregion

            #region frameIn
            frameIn = new FrameView(GetInTitle())
            {
                X = posright + 1,
                Y = 1,
                Width = 40,
                Height = 9
            };
            frameIn.Add (hostInLabel = new Label (RES.TAG_HOST) {
				X = 1,
				Y = 1,
				AutoSize = true
			});
			frameIn.Add (hostInText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 1,
				Width = 27,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameIn.Add (portInLabel = new Label (RES.TAG_PORT) {
				X = 1,
				Y = 3,
				AutoSize = true
			});
			frameIn.Add (portInText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 3,
				Width = 8,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameIn.Add (mailInType = new RadioGroup (typeopt) {
				X = labelOffset + 10,
				Y = 3,
				Width = 15,
				DisplayMode = DisplayModeLayout.Horizontal,
				SelectedItem = 1,
			});
			frameIn.Add (tlsInLabel = new Label ("SSL/TLS: ") {
				X = 1,
				Y = 5,
				AutoSize = true
			});
			frameIn.Add (tlsInText = new ComboBox () {
				X = labelOffset,
				Y = 5,
				Width = 27,
				Height = 4,
				ReadOnly = true,
				ColorScheme = GuiApp.ColorField
			});
			tlsInText.SetSource (ssltlsopt.ToList());
			mailInType.SelectedItemChanged += MailType_SelectedItemChanged;
			Add (frameIn);
            #endregion

            #region frameOut
            frameOut = new FrameView("SMTP")
            {
                X = Pos.Right(frameIn) + 1,
                Y = 1,
                Width = 40,
                Height = 9
            };
            frameOut.Add (hostOutLabel = new Label (RES.TAG_HOST) {
				X = 1,
				Y = 1,
				AutoSize = true
			});
			frameOut.Add (hostOutText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 1,
				Width = 27,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameOut.Add (portOutLabel = new Label (RES.TAG_PORT) {
				X = 1,
				Y = 3,
				AutoSize = true
			});
			frameOut.Add (portOutText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 3,
				Width = 8,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameOut.Add (tlsOutLabel = new Label ("SSL/TLS: ") {
				X = 1,
				Y = 5,
				AutoSize = true
			});
			frameOut.Add (tlsOutText = new ComboBox () {
				X = labelOffset,
				Y = 5,
				Width = 27,
				Height = 4,
				ReadOnly = true,
				ColorScheme = GuiApp.ColorField
			});
			tlsOutText.SetSource (ssltlsopt.ToList ());
			Add (frameOut);
            #endregion

            #region frameUser
            frameUser = new FrameView(RES.TAG_ACCOUNT)
            {
                X = posright + 1,
                Y = 10,
                Width = 50,
                Height = Dim.Fill() - 5
            };
            posbottom = Pos.Bottom(frameUser);
            frameUser.Add (loginLabel = new Label (RES.TAG_LOGIN) {
				X = 1,
				Y = 1,
				AutoSize = true
			});
			frameUser.Add (loginText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 1,
				Width = 37,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameUser.Add (passLabel = new Label (RES.TAG_PASSWORD) {
				X = 1,
				Y = 3,
				AutoSize = true
			});
			frameUser.Add (passText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 3,
				Width = 37,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameUser.Add (emailLabel = new Label ("Email: ") {
				X = 1,
				Y = 5,
				AutoSize = true
			});
			frameUser.Add (emailText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 5,
				Width = 37,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameUser.Add (nameLabel = new Label (RES.TAG_NAME) {
				X = 1,
				Y = 7,
				AutoSize = true
			});
			frameUser.Add (nameText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 7,
				Width = 37,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
            loginText.KeyUp += (_) => ButtonsEnable(!IsEmptyForm, !IsEmptyForm);
            passText.KeyUp += (_) => ButtonsEnable(!IsEmptyForm, !IsEmptyForm);
            Add(frameUser);
            #endregion

            #region framePgp
            framePgp = new FrameView(RES.MENU_PGPKEYS.ClearText())
            {
                X = Pos.Right(frameUser) + 1,
                Y = 10,
                Width = 30,
                Height = Dim.Fill() - 5
            };
            framePgp.Add(pgpKeyIdLabel = new Label(RES.TAG_PGPKEY_ID)
            {
                X = 1,
                Y = 1,
                AutoSize = true
            });
            framePgp.Add(pgpKeyIdText = new Label(string.Empty)
            {
                X = 7,
                Y = 1,
                AutoSize = true,
				ColorScheme = GuiApp.ColorDescription
            });
            framePgp.Add(pgpKeySignLabel = new Label(RES.TAG_PGPKEY_SIGN)
            {
                X = 1,
                Y = 2,
                AutoSize = true
            });
            framePgp.Add(pgpKeySignText = new Label(string.Empty)
            {
                X = 24,
                Y = 2,
                AutoSize = true,
                ColorScheme = GuiApp.ColorDescription
            });
            framePgp.Add(pgpKeyCryptLabel = new Label(RES.TAG_PGPKEY_CRYPT)
            {
                X = 1,
                Y = 3,
                AutoSize = true
            });
            framePgp.Add(pgpKeyCryptText = new Label(string.Empty)
            {
                X = 24,
                Y = 3,
                AutoSize = true,
                ColorScheme = GuiApp.ColorDescription
            });
            framePgp.Add(pgpKeyDecryptLabel = new Label(RES.TAG_PGPKEY_DECRYPT)
            {
                X = 1,
                Y = 4,
                AutoSize = true
            });
            framePgp.Add(pgpKeyDecryptText = new Label(string.Empty)
            {
                X = 24,
                Y = 4,
                AutoSize = true,
                ColorScheme = GuiApp.ColorDescription
            });
            framePgp.Add(pgpKeyCountLabel = new Label(RES.TAG_PGPKEY_CRYPT_COUNT)
            {
                X = 1,
                Y = 5,
                AutoSize = true
            });
            framePgp.Add(pgpKeyCountText = new Label(string.Empty)
            {
                X = 24,
                Y = 5,
                AutoSize = true,
                ColorScheme = GuiApp.ColorDescription
            });
            framePgp.Add(buttonPgpImport = new Button(RES.BTN_IMPORT)
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
				Enabled = false
            });
            framePgp.Add(buttonPgpExport = new Button(RES.BTN_EXPORT)
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx].AutoSize,
                Enabled = false
            });
            framePgp.Add(buttonPgpCreate = new Button(RES.BTN_CREATE)
            {
                X = layout[idx].X,
                Y = layout[idx].Y,
                AutoSize = layout[idx++].AutoSize,
                Enabled = false,
				Visible = false
            });
            Add(framePgp);
            #endregion

            #region frameHelp
            frameHelp = new FrameView(RES.TAG_HELP)
            {
                X = Pos.Right(frameOut) + 1,
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
            Add(enableBox = new CheckBox(RES.TAG_ENABLE)
            {
                X = posright + layout[idx].X,
                Y = posbottom + layout[idx++].Y,
                Width = 10,
                Height = 1,
                Checked = true
            });
            Add(pgpAutoBox = new CheckBox(RES.CHKBOX_PGPAUTODECRYPT)
            {
                X = posright + layout[idx].X,
                Y = posbottom + layout[idx++].Y,
                Width = 10,
                Height = 1,
                Checked = false,
                Enabled = false
            });
            enableBox.Toggled += EnableBox_Toggled;
            #endregion

            #region Buttons
            {
                Button btn = buttonPaste;
                buttonPaste = default;
                if (btn != default)
                    btn.Dispose();
            }
            buttonSave.SetLinearLayout(layout[idx++], posright, posbottom);
            buttonClear.SetLinearLayout(layout[idx++], posright, posbottom);
            buttonDelete.SetLinearLayout(layout[idx++], posright, posbottom);
            buttonImport.SetLinearLayout(layout[idx++], posright, posbottom);
            buttonExport.SetLinearLayout(layout[idx++], posright, posbottom);

            Add(buttonSave, buttonClear, buttonDelete, buttonImport, buttonExport);
            #endregion

            #region Buttons command
            buttonPgpImport.Clicked += async () => {
                try {
                    if ((accountGpg == null) || !accountGpg.CanImport) {
                        Application.MainLoop.Invoke(() => buttonPgpImport.Enabled = false);
						return;
                    }
                    GuiOpenDialog d = $"{RES.MENU_PGPKEY_IMPORT.ClearText()} - {runOnce.Ids}"
															   .GuiOpenDialogs(true, extpgp);
                    Application.Run(d);
                    if (!d.Canceled) {
                        string[] ss = d.GuiReturnDialog();
						if (ss.Length > 0)
							_ = await accountGpg.ImportAsync(ss[0]).ContinueWith(async (t) => {
                                _ = await ControlPgp().ConfigureAwait(false);
                            }).ConfigureAwait(false);
                    }
                } catch (Exception ex) { ex.StatusBarError(); }
            };
            buttonPgpExport.Clicked += async () => {
                try {
                    if ((accountGpg == null) || !accountGpg.CanExport) {
                        Application.MainLoop.Invoke(() => buttonPgpExport.Enabled = false);
                        return;
                    }
                    GuiSaveDialog d = $"{RES.MENU_PGPKEY_EXPORT.ClearText()} - {runOnce.Ids}"
															   .GuiSaveDialogs(
																	Global.GetRootDirectory(Global.DirectoryPlace.Export),
																	extpgp);
                    Application.Run(d);
                    if (!d.Canceled) {
                        string[] ss = d.GuiReturnDialog();
                        if (ss.Length > 0)
                            _ = await accountGpg.ExportAsync(ss[0]).ConfigureAwait(false);
                    }
                } catch (Exception ex) { ex.StatusBarError(); }
            };
            buttonPgpCreate.Clicked += async () => {
                try {
                    if ((accountGpg == null) || !accountGpg.CanCreate) {
                        Application.MainLoop.Invoke(() => buttonPgpCreate.Enabled = false);
                        return;
                    }
                    if (MessageBox.Query(50, 7,
                        RES.MENU_PGPKEYS.ClearText(),
                        $"{RES.MENU_PGPKEY_CREATE.ClearText()} - {runOnce.Ids} ?", RES.TAG_YES, RES.TAG_NO) == 0) {
                        _ = await accountGpg.GenerateKeyPairAsync().ContinueWith(async (t) => {
                            _ = await ControlPgp().ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }
                } catch (Exception ex) { ex.StatusBarError(); }
            };
            #endregion

            urlmenu = new MenuBarItem("_Url", new MenuItem[0]);
			GuiMenu = new MenuBar(new MenuBarItem[] {
				new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
					new MenuItem (RES.MENU_RELOAD, "", async () => {
						_ = await Global.Instance.Accounts.Load().ConfigureAwait(false);
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
								foreach(UserAccount a in Global.Instance.Accounts.Items)
									a.Enable = true;
								_ = await Global.Instance.Accounts.Save().ConfigureAwait(false);
							} catch (Exception ex) { ex.StatusBarError(); }
						}
					}),
					new MenuItem (string.Format(RES.GUIACCOUNT_FMT1, RES.TAG_OFF, tag), string.Empty, async () => {
						if (MessageBox.Query (50, 7,
							string.Format(RES.GUIACCOUNT_FMT2, RES.TAG_OFF),
							string.Format(RES.GUIACCOUNT_FMT4, RES.TAG_OFF, tag), RES.TAG_YES, RES.TAG_NO) == 0) {
							try {
								foreach(UserAccount a in Global.Instance.Accounts.Items)
									a.Enable = false;
								_ = await Global.Instance.Accounts.Save().ConfigureAwait(false);
							} catch (Exception ex) { ex.StatusBarError(); }
						}
					}),
					new MenuItem (string.Format(RES.GUIACCOUNT_FMT1, RES.MENU_DELETE, tag), string.Empty, async () => {
						if (MessageBox.Query (50, 7,
							string.Format(RES.GUIACCOUNT_FMT2, RES.TAG_DELETE),
							string.Format(RES.GUIACCOUNT_FMT4, RES.TAG_DELETE, tag), RES.TAG_YES, RES.TAG_NO) == 0) {
							try {
								DataClear();
                                Global.Instance.Accounts.Clear();
								_ = await Global.Instance.Accounts.Save().ConfigureAwait(false);
							} catch (Exception ex) { ex.StatusBarError(); }
						}
					})
				}),
				new MenuBarItem (RES.MENU_PGPKEYS, new MenuItem [] {
					new MenuItem (RES.MENU_PGPKEY_CREATE, "",
						() => buttonPgpCreate.OnClicked(),
						() => (accountGpg != null) && accountGpg.CanCreate),
                    new MenuItem (RES.MENU_PGPKEY_IMPORT, "",
						() => buttonPgpImport.OnClicked(),
						() => (accountGpg != null) && accountGpg.CanImport),
					null,
                    new MenuItem (RES.MENU_PGPKEY_EXPORT, "",
                        () => buttonPgpExport.OnClicked(),
                        () => (accountGpg != null) && accountGpg.CanExport),
                    new MenuItem (RES.MENU_GPGEXPORT, "", async () => {
                        try {
                            _ = await CryptGpgContext.ExportAccountToGpg(Properties.Settings.Default.PgpBinPath)
                                                     .ConfigureAwait(false);
                        } catch (Exception ex) { ex.StatusBarError(); }
                    }, () => !string.IsNullOrWhiteSpace(Properties.Settings.Default.PgpBinPath) &&
                             !string.IsNullOrWhiteSpace(Global.Instance.Config.PgpPassword))
                }),
                urlmenu
            });
			GuiToplevel.Add (GuiMenu, this);
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
                    _ = await base.LoadAccounts(Global.Instance.Accounts.Items).ConfigureAwait(false);
                    try {
                        MenuItem[] mitems = await nameof(GuiMailAccountWindow).LoadMenuUrls().ConfigureAwait(false);
                        Application.MainLoop.Invoke(() => urlmenu.Children = mitems);
                    }
                    catch (Exception ex) { ex.StatusBarError(); }
                    Application.MainLoop.Invoke(() => helpText.Text = RES.GuiMailAccountWindowHelp);
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { base.runOnce.End(); }
                return true;
            });
        #endregion

        #region Virtual override
        protected override bool IsEmptyForm =>
            string.IsNullOrWhiteSpace(loginText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(passText.Text.ToString());

        protected override void VirtualEnableToggled(bool b) {
            hostInText.Enabled =
            portInText.Enabled =
            hostOutText.Enabled =
            portOutText.Enabled =
            tlsInText.Enabled =
            tlsOutText.Enabled =
            loginText.Enabled =
            passText.Enabled =
            emailText.Enabled =
            nameText.Enabled =
            pgpAutoBox.Enabled = b;
        }

        protected override void VirtualClean() {
            hostInText.Text =
            hostOutText.Text =
            loginText.Text =
            passText.Text =
            emailText.Text =
            nameText.Text = string.Empty;
            portInText.Text = "110";
            portOutText.Text = "25";

            pgpKeyIdText.Text =
            pgpKeySignText.Text =
            pgpKeyCountText.Text =
            pgpKeyCryptText.Text =
            pgpKeyDecryptText.Text = string.Empty;

            buttonPgpCreate.Enabled =
            buttonPgpImport.Enabled =
            buttonPgpExport.Enabled =
            buttonPgpExport.Visible = false;
            buttonPgpCreate.Visible = true;

            enableBox.Checked = false;
            pgpAutoBox.Checked = false;
            enableBox.Enabled = false;
            pgpAutoBox.Enabled = false;
            tlsInText.SelectedItem = 0;
            tlsOutText.SelectedItem = 0;
            enableBox.Checked = true;
            inMailType = InMailType.None;
            frameIn.Title = GetInTitle();
            frameUser.Title = RES.TAG_ACCOUNT;
        }

        protected override async Task<bool> VirtualSaveAll() =>
            await Global.Instance.Accounts.Save().ConfigureAwait(false);

        protected override void VirtualAddItem(UserAccount acc) =>
            Global.Instance.Accounts.Add(acc);

        protected override UserAccount VirtualGetItem(string s) =>
            Global.Instance.FindFromEmail(s);

        protected override UserAccount VirtualNewItem() {
            string email = emailText.Text.ToString();
            if (string.IsNullOrEmpty(email))
                return default;
            return new UserAccount();
        }

        protected override void VirtualBuildItem(UserAccount acc) {
            string s = nameText.Text.ToString();
            if (string.IsNullOrEmpty(s)) {
                int idx = s.IndexOf('@');
                if (idx > 0)
                    s = s.Substring(0, idx);
                acc.Name = s;
            } else {
                acc.Name = s;
            }

            acc.Login = loginText.Text.ToString();
            acc.Pass = passText.Text.ToString();
            acc.EmailAddress = emailText.Text.ToString();
            acc.Enable = enableBox.Checked;
            acc.IsPgpAutoDecrypt = pgpAutoBox.Checked;

            acc.SmtpAddr = hostOutText.Text.ToString();
            if (int.TryParse(portOutText.Text.ToString(), out int oport))
                acc.SmtpPort = oport;
            acc.SmtpSecure = SecureOptionsSelect(tlsOutText.SelectedItem);

            if (inMailType == InMailType.IMAP) {
                acc.ImapAddr = hostInText.Text.ToString();
                if (int.TryParse(portInText.Text.ToString(), out int iport))
                    acc.ImapPort = iport;
                acc.ImapSecure = SecureOptionsSelect(tlsInText.SelectedItem);
            }
            else if (inMailType == InMailType.POP3) {
                acc.Pop3Addr = hostInText.Text.ToString();
                if (int.TryParse(portInText.Text.ToString(), out int iport))
                    acc.Pop3Port = iport;
                acc.Pop3Secure = SecureOptionsSelect(tlsInText.SelectedItem);
            }
        }

        protected override async void VirtualSelectItem(UserAccount acc) {

            runOnce.ChangeId(acc.Email);

            Application.MainLoop.Invoke(() => {

                if (!acc.IsEmptyImapReceive) {
                    hostInText.Text = acc.ImapAddr;
                    portInText.Text = acc.ImapPort.ToString();
                    tlsInText.SelectedItem = SecureOptionsSelect(acc.ImapSecure);
                    inMailType = InMailType.IMAP;
                    frameIn.Title = GetInTitle(inMailType);
                } else if (!acc.IsEmptyPop3Receive) {
                    hostInText.Text = acc.Pop3Addr;
                    portInText.Text = acc.Pop3Port.ToString();
                    tlsInText.SelectedItem = SecureOptionsSelect(acc.Pop3Secure);
                    inMailType = InMailType.POP3;
                    frameIn.Title = GetInTitle(inMailType);
                } else {
                    hostInText.Text = string.Empty;
                    portInText.Text = string.Empty;
                    tlsInText.SelectedItem = 0;
                    inMailType = InMailType.None;
                    frameIn.Title = GetInTitle(inMailType);
                }

                if (!acc.IsEmptySend) {
                    hostOutText.Text = acc.SmtpAddr;
                    portOutText.Text = acc.SmtpPort.ToString();
                    tlsOutText.SelectedItem = SecureOptionsSelect(acc.SmtpSecure);
                } else {
                    hostOutText.Text = string.Empty;
                    portOutText.Text = string.Empty;
                    tlsOutText.SelectedItem = 0;
                }

                frameUser.Title = string.IsNullOrWhiteSpace(acc.Name) ?
                    $"{RES.TAG_ACCOUNT} :: {acc.Email}" : $"{RES.TAG_ACCOUNT} :: {acc.Name} - {acc.Email}";

                loginText.Text = acc.Login;
                passText.Text = acc.Pass;
                emailText.Text = acc.Email;
                nameText.Text = acc.Name;
                pgpAutoBox.Checked = acc.IsPgpAutoDecrypt;
                pgpAutoBox.Enabled = true;
                enableBox.Enabled = true;

                enableBox.Checked = acc.Enable;
                EnableBox_Toggled(acc.Enable);
                ButtonsEnable(true, acc.Enable);
            });
            _ = await ControlPgp().ConfigureAwait(false);
        }

        protected override void VirtualDeleteItem(UserAccount acc) =>
            Global.Instance.Accounts.Items.Remove(acc);

        protected override async Task<UserAccount> VirtualImportFile(string s) {
            UserAccount a = new();
            bool b = await a.Load(s).ConfigureAwait(false);
            return b ? a : null;
        }

        protected override async Task<string> VirtualExport(UserAccount acc, string s) {
            bool b = await acc.Save(s).ConfigureAwait(false);
            if (!b) return "Export error..";
            return string.Empty;
        }
        #endregion

        private void MailType_SelectedItemChanged (SelectedItemChangedArgs obj) {
			if ((obj == null) || (obj.SelectedItem < 0) || (obj.SelectedItem >= typeopt.Length))
				return;
			ustring s = typeopt[obj.SelectedItem].ClearText();
			if (Enum.TryParse(s.ToString(), out InMailType mt))
				inMailType = mt;
			frameIn.Title = s;
		}

        #region PGP control
		private async Task<bool> ControlPgp() =>
			await Task.Run(() => {
                if (!runOnce.IsValidIds())
                    return false;

                lock (__pgpLock) {
                    if (accountGpg == null)
                        accountGpg = new(runOnce.Ids);
                    else if (!runOnce.Ids.Equals(accountGpg.EmailAddress.Address)) {
                        DisposeControlPgpInternal();
                        accountGpg = new(runOnce.Ids);
                    }
                    accountGpg.Build();
                }
                Application.MainLoop.Invoke(() => {
                    pgpKeySignText.Text = ControlPgpAnswer(accountGpg.IsSigningKey);
                    pgpKeyCryptText.Text = ControlPgpAnswer(accountGpg.IsPublicKey);
                    pgpKeyDecryptText.Text = ControlPgpAnswer(accountGpg.IsSecretKey);
                    pgpKeyCountText.Text = accountGpg.PublicKeyCount.ToString();
                    pgpKeyIdText.Text = (accountGpg.KeyId > 0) ? accountGpg.KeyId.ToString() : string.Empty;

					bool isnew = !accountGpg.IsSigningKey && !accountGpg.IsPublicKey && !accountGpg.IsSigningKey;
                    buttonPgpExport.Visible = !isnew;
                    buttonPgpExport.Enabled = accountGpg.IsPublicKey;
                    buttonPgpImport.Enabled =
                    buttonPgpCreate.Enabled =
                    buttonPgpCreate.Visible = isnew;
                });
				return true;
			});

		private string ControlPgpAnswer(bool b) =>
			b ? RES.TAG_OK : RES.TAG_NO;

        private void DisposeControlPgp() {
			lock (__pgpLock)
                DisposeControlPgpInternal();
        }
        private void DisposeControlPgpInternal() {
            AccountGpgKeys acc = accountGpg;
            accountGpg = null;
            if (acc != null)
                acc.Dispose();
        }
        #endregion

        private int SecureOptionsSelect(SecureSocketOptions opt) =>
			opt switch {
				SecureSocketOptions.None => 0,
				SecureSocketOptions.SslOnConnect => 1,
				SecureSocketOptions.StartTls => 2,
				SecureSocketOptions.Auto => 3,
				_ => 0
			};

		private SecureSocketOptions SecureOptionsSelect(int idx) =>
			idx switch
			{
				0 => SecureSocketOptions.None,
				1 => SecureSocketOptions.SslOnConnect,
				2 => SecureSocketOptions.StartTls,
				3 => SecureSocketOptions.Auto,
				_ => SecureSocketOptions.None
			};
	}
}

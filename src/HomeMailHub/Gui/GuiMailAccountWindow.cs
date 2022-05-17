
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HomeMailHub.Gui.Dialogs;
using MailKit.Security;
using NStack;
using SecyrityMail;
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

	public class GuiMailAccountWindow : Window, IGuiWindow<GuiMailAccountWindow> {
		private const int labelOffset = 10;
		private const string tag = "Mail";
		private static readonly string [] extension = new string [] { ".conf", ".cnf", ".xml" };
		private static readonly ustring [] ssltlsopt = new ustring [] { "None", "SslOnConnect", "StartTls", "Auto" };
		private static readonly ustring [] typeopt = new ustring [] { "_POP3", "_IMAP" };
		private static ustring GetInTitle(InMailType type = InMailType.None) =>
			((type == InMailType.None) ? $"{typeopt[0]}/{typeopt[1]}" :
				((type == InMailType.IMAP) ? typeopt[1] : typeopt[0])).Replace("_", "");

		private Toplevel GuiToplevel { get; set; } = default;
		private MenuBar  GuiMenu { get; set; } = default;
        private MenuBarItem urlmenu { get; set; } = default;
        private ListView listView { get; set; } = default;

		private Button buttonSave { get; set; } = default;
		private Button buttonClear { get; set; } = default;
		private Button buttonDelete { get; set; } = default;
		private Button buttonImport { get; set; } = default;
		private Button buttonExport { get; set; } = default;

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
		private FrameView frameList { get; set; } = default;
		private FrameView frameUser { get; set; } = default;

		private Label tlsInLabel { get; set; } = default;
		private Label tlsOutLabel { get; set; } = default;

		private ComboBox tlsInText { get; set; } = default;
		private ComboBox tlsOutText { get; set; } = default;
		private CheckBox enableBox { get; set; } = default;
		private RadioGroup mailInType { get; set; } = default;

		public Toplevel GetTop => GuiToplevel;

		private string selectedName { get; set; } = string.Empty;
		private UserAccount account { get; set; } = default;
		private InMailType inMailType { get; set; } = InMailType.None;
		private GuiRunOnce runOnce = new();
		private List<string> data = new();
        private GuiLinearLayot linearLayot { get; } = new();

        private bool IsEmptyForm =>
            string.IsNullOrWhiteSpace(loginText.Text.ToString()) ||
            string.IsNullOrWhiteSpace(passText.Text.ToString());

        private void ButtonsEnable(bool b) =>
            buttonSave.Enabled = buttonClear.Enabled = buttonDelete.Enabled = buttonExport.Enabled = b;

        public GuiMailAccountWindow() : base (RES.GUIMAIL_TITLE1, 0)
		{
			X = 0;
			Y = 1;
			Width = Dim.Fill ();
			Height = Dim.Fill () - 1;
			GuiToplevel = GuiExtensions.CreteTop ();

            linearLayot.Add("en", new List<GuiLinearData> {
                new GuiLinearData(10, 12, true),
                new GuiLinearData(19, 12, true),
                new GuiLinearData(29, 12, true),
                new GuiLinearData(40, 12, true),
                new GuiLinearData(51, 12, true)
            });
            linearLayot.Add("ru", new List<GuiLinearData> {
                new GuiLinearData(10, 12, true),
                new GuiLinearData(24, 12, true),
                new GuiLinearData(37, 12, true),
                new GuiLinearData(49, 12, true),
                new GuiLinearData(60, 12, true)
            });
        }

        public new void Dispose() {

			account = default;
			this.GetType().IDisposableObject(this);
			base.Dispose();
		}

		#region Init
		public GuiMailAccountWindow Init(string __)
		{
            List<GuiLinearData> layout = linearLayot.GetDefault();

            frameList = new FrameView (new Rect (0, 0, 35, 25), RES.TAG_ACCOUNTS) {
				X = 1,
				Y = 1
			};
			frameIn = new FrameView (new Rect (0, 0, 40, 9), GetInTitle()) {
				X = 37,
				Y = 1
			};
			frameOut = new FrameView (new Rect (0, 0, 40, 9), "SMTP") {
				X = 77,
				Y = 1
			};
			frameUser = new FrameView (new Rect (0, 0, 80, 16), RES.TAG_ACCOUNT) {
				X = 37,
				Y = 10
			};
			listView = new ListView (data) {
				X = 1,
				Y = 1,
				Width = Dim.Fill () - 4,
				Height = Dim.Fill () - 1,
				AllowsMarking = true,
				AllowsMultipleSelection = false
			};
			listView.OpenSelectedItem += ListView_OpenSelectedItem;
			listView.SelectedItemChanged += ListView_SelectedItemChanged;

			frameList.Add (listView);
			Add (frameList);

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

			frameUser.Add (loginLabel = new Label (RES.TAG_LOGIN) {
				X = 1,
				Y = 1,
				AutoSize = true
			});
			frameUser.Add (loginText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 1,
				Width = 30,
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
				Width = 30,
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
				Width = 30,
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
				Width = 30,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameUser.Add (enableBox = new CheckBox (1, 0, RES.TAG_ENABLE) {
				X = labelOffset,
				Y = 10,
				Width = 10,
				Height = 1,
				Checked = true
			});
			enableBox.Toggled += EnableBox_Toggled;

			frameUser.Add (buttonSave = new Button (10, 19, RES.BTN_SAVE) {
                X = layout[0].X,
                Y = layout[0].Y,
                AutoSize = layout[0].AutoSize,
                TabIndex = 13
			});
			frameUser.Add (buttonClear = new Button (10, 19, RES.BTN_CLEAR) {
                X = layout[1].X,
                Y = layout[1].Y,
                AutoSize = layout[1].AutoSize,
                TabIndex = 14
			});
			frameUser.Add (buttonDelete = new Button (10, 19, RES.BTN_DELETE) {
                X = layout[2].X,
                Y = layout[2].Y,
                AutoSize = layout[2].AutoSize,
                TabIndex = 15
			});
			frameUser.Add (buttonImport = new Button (10, 19, RES.BTN_IMPORT) {
                X = layout[3].X,
                Y = layout[3].Y,
                AutoSize = layout[3].AutoSize,
                TabIndex = 16
			});
			frameUser.Add (buttonExport = new Button (10, 19, RES.BTN_EXPORT) {
                X = layout[4].X,
                Y = layout[4].Y,
                AutoSize = layout[4].AutoSize,
                TabIndex = 17
			});
            loginText.KeyUp += (_) => ButtonsEnable(!IsEmptyForm);
            passText.KeyUp += (_) => ButtonsEnable(!IsEmptyForm);
            Add(frameUser);

			buttonSave.Clicked += () => SaveItem();
			buttonClear.Clicked += () => Clean();
			buttonDelete.Clicked += () => Delete();
			buttonImport.Clicked += async () => {
				GuiOpenDialog d = string.Format(RES.GUIACCOUNT_FMT6, RES.TAG_OPEN_IMPORT, tag).GuiOpenDialogs(true, extension);
				Application.Run(d);
				if (!d.Canceled) {
					try {
						string[] ss = d.GuiReturnDialog();
						if (ss.Length > 0) {
							foreach (string s in ss) {
								try {
									UserAccount a = new ();
									bool b = await a.Load(s).ConfigureAwait(false);
									AddItem(a, b);
								}
								catch (Exception ex) { ex.StatusBarError(); }
							}
							_ = await Global.Instance.Accounts.Save().ConfigureAwait(false);
						}
					} catch (Exception ex) { ex.StatusBarError(); }
				}
			};
			buttonExport.Clicked += async () => {
				if (account == default) {
					if (string.IsNullOrEmpty(selectedName))
						return;

					account = (from i in Global.Instance.Accounts.Items
							   where i.Email.Equals(selectedName)
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
							_ = await account.Save(ss[0]).ConfigureAwait(false);
					} catch (Exception ex) { ex.StatusBarError(); }
				}
			};
            urlmenu = new MenuBarItem("_Url", new MenuItem[0]);
            GuiMenu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
					new MenuItem (RES.MENU_RELOAD, "", async () => {
						_ = await Global.Instance.Accounts.Load().ConfigureAwait(false);
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
								Global.Instance.Accounts.Clear();
								_ = await Global.Instance.Accounts.Save().ConfigureAwait(false);
							} catch (Exception ex) { ex.StatusBarError(); }
						}
					})
				}),
                urlmenu
            });
			GuiToplevel.Add (GuiMenu, this);
			return this;
		}
        #endregion

        #region Load
        public async void Load() => _ = await Load_().ConfigureAwait(false);
		private async Task<bool> Load_() =>
			await Task.Run(async () => {
				DataClear();
				try {
					foreach (UserAccount a in Global.Instance.Accounts.Items)
						data.Add(a.Email);
					await listView.SetSourceAsync(data).ConfigureAwait(false);
					frameList.Title = selectedName.GetListTitle(data.Count);
					Clean();
				} catch (Exception ex) { ex.StatusBarError(); }
                try {
                    MenuItem[] mitems = await nameof(GuiMailAccountWindow).LoadMenuUrls().ConfigureAwait(false);
                    Application.MainLoop.Invoke(() => urlmenu.Children = mitems);
                }
                catch { }
                return true;
			});
        #endregion

        private void MailType_SelectedItemChanged (SelectedItemChangedArgs obj) {
			if ((obj == null) || (obj.SelectedItem < 0) || (obj.SelectedItem >= typeopt.Length))
				return;
			ustring s = typeopt[obj.SelectedItem].ClearText();
			if (Enum.TryParse(s.ToString(), out InMailType mt))
				inMailType = mt;
			frameIn.Title = s;
		}

		private void DataClear() {
			data.Clear();
			Clean();
            Application.MainLoop.Invoke(() => frameList.Title = selectedName.GetListTitle(0));
		}
		private void Clean() =>
			Application.MainLoop.Invoke(() => {
                hostInText.Text =
                hostOutText.Text =
                loginText.Text =
                passText.Text =
                emailText.Text =
                nameText.Text = string.Empty;
                portInText.Text = "110";
                portOutText.Text = "25";

                enableBox.Checked = true;
                tlsInText.SelectedItem = 0;
                tlsOutText.SelectedItem = 0;
                enableBox.Checked = true;
                inMailType = InMailType.None;
                frameIn.Title = GetInTitle();
                ButtonsEnable(false);
            });

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
						UserAccount a = (from i in Global.Instance.Accounts.Items
										 where i.Email.Equals(s)
										 select i).FirstOrDefault();
						if (a == default)
							return;

						Global.Instance.Accounts.Items.Remove(a);
						_ = await Global.Instance.Accounts.Save().ConfigureAwait(false);
					}
					catch (Exception ex) { ex.StatusBarError(); }
				}
			} finally { runOnce.EndRun(); }
		}

		private void EnableBox_Toggled(bool b) =>
			Application.MainLoop.Invoke(() => {
                hostInText.Enabled =
				portInText.Enabled =
				hostOutText.Enabled =
				portOutText.Enabled =
				tlsInText.Enabled =
				tlsOutText.Enabled =
				loginText.Enabled =
				passText.Enabled =
				emailText.Enabled =
				nameText.Enabled = !b;
                ButtonsEnable(!b);
            });

		private void ListView_OpenSelectedItem(ListViewItemEventArgs obj) => SelectedListItem(obj);
		private void ListView_SelectedItemChanged(ListViewItemEventArgs obj) => SelectedListItem(obj);

		private void SelectedListItem(ListViewItemEventArgs obj) {
			if (obj == null)
				return;
			if ((obj.Item >= 0) && (obj.Item < data.Count))
				SelectItem(data[obj.Item], obj.Item);
		}

		private void SelectItem(string s, int id) {

			if (!runOnce.GoRun(id))
				return;

			try {
				if (string.IsNullOrEmpty(s))
					return;
				UserAccount a = (from i in Global.Instance.Accounts.Items
								 where i.Email.Equals(s)
								 select i).FirstOrDefault();
				if (a == default)
					return;

				selectedName = s;

				if (!a.IsEmptyImapReceive) {
					hostInText.Text = a.ImapAddr;
					portInText.Text = a.ImapPort.ToString();
					tlsInText.SelectedItem = SecureOptionsSelect(a.ImapSecure);
					inMailType = InMailType.IMAP;
					frameIn.Title = GetInTitle(inMailType);
				}
				else if (!a.IsEmptyPop3Receive) {
					hostInText.Text = a.Pop3Addr;
					portInText.Text = a.Pop3Port.ToString();
					tlsInText.SelectedItem = SecureOptionsSelect(a.Pop3Secure);
					inMailType = InMailType.POP3;
					frameIn.Title = GetInTitle(inMailType);
				}
				else {
					hostInText.Text = string.Empty;
					portInText.Text = string.Empty;
					tlsInText.SelectedItem = 0;
					inMailType = InMailType.None;
					frameIn.Title = GetInTitle(inMailType);
				}
				if (!a.IsEmptySend) {
					hostOutText.Text = a.SmtpAddr;
					portOutText.Text = a.SmtpPort.ToString();
					tlsOutText.SelectedItem = SecureOptionsSelect(a.SmtpSecure);
				}
				else {
					hostOutText.Text = string.Empty;
					portOutText.Text = string.Empty;
					tlsOutText.SelectedItem = 0;
				}

				loginText.Text = a.Login;
				passText.Text = a.Pass;
				emailText.Text = a.Email;
				nameText.Text = a.Name;

				enableBox.Checked = a.Enable;
				EnableBox_Toggled(!a.Enable);
				account = a;
                ButtonsEnable(true);

            } finally { runOnce.EndRun(); }
		}

		private async void SaveItem() {

			if (!runOnce.GoRun())
				return;

			try {
				bool b = string.IsNullOrEmpty(selectedName);
				if (b) {
					UserAccount a = NewItem();
					if (a == default)
						return;
					BuildItem(a);
					AddItem(a, b);
					account = a;
					await Global.Instance.Accounts.Save().ConfigureAwait(false);
				} else {
					UserAccount a = (from i in Global.Instance.Accounts.Items
									 where i.Email.Equals(selectedName)
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
					await Global.Instance.Accounts.Save().ConfigureAwait(false);
				}
			} finally { runOnce.EndRun(); }
		}
		private UserAccount NewItem() {

			string email = emailText.Text.ToString();
			if (string.IsNullOrEmpty(email))
				return default;
			
			return new UserAccount();
		}
		private void BuildItem(UserAccount a)
		{
			string s = nameText.Text.ToString();
			if (string.IsNullOrEmpty(s)) {
				int idx = s.IndexOf('@');
				if (idx > 0)
					s = s.Substring(0, idx);
				a.Name = s;
			}
			else {
				a.Name = s;
			}

			a.Login = loginText.Text.ToString();
			a.Pass = passText.Text.ToString();
			a.EmailAddress = emailText.Text.ToString();
			a.Enable = enableBox.Checked;

			a.SmtpAddr = hostOutText.Text.ToString();
			if (int.TryParse(portOutText.Text.ToString(), out int oport))
				a.SmtpPort = oport;
			a.SmtpSecure = SecureOptionsSelect(tlsOutText.SelectedItem);

			if (inMailType == InMailType.IMAP) {
				a.ImapAddr = hostInText.Text.ToString();
				if (int.TryParse(portInText.Text.ToString(), out int iport))
					a.ImapPort = iport;
				a.ImapSecure = SecureOptionsSelect(tlsInText.SelectedItem);
			}
			else if (inMailType == InMailType.POP3) {
				a.Pop3Addr = hostInText.Text.ToString();
				if (int.TryParse(portInText.Text.ToString(), out int iport))
					a.Pop3Port = iport;
				a.Pop3Secure = SecureOptionsSelect(tlsInText.SelectedItem);
			}
		}

		private async void AddItem(UserAccount a, bool b)
		{
			if (!b) return;
			try {
				Global.Instance.Accounts.Add(a);
				data.Add(a.Name);
				await listView.SetSourceAsync(data).ConfigureAwait(false);
				frameList.Title = selectedName.GetListTitle(data.Count);
			} catch (Exception ex) { ex.StatusBarError(); }
		}

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


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HomeMailHub.Gui.Dialogs;
using NStack;
using SecyrityMail;
using SecyrityMail.Proxy;
using SecyrityMail.Proxy.SshProxy;
using SecyrityMail.Vpn;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiSshAccountWindow : Window, IGuiWindow<GuiSshAccountWindow> {
		private const int labelOffset = 12;
		private static readonly string tag = "SSH";
		private static readonly string [] extension = new string [] { ".conf", ".cnf", ".xml" };
		private static readonly ustring[] proxyopt = new ustring[] { "None", "SshSock4", "SshSock5" };
		private static ustring GetInTitle(ProxyType type = ProxyType.None) =>
			(type == ProxyType.SshSock4) ? proxyopt[1] :
				((type == ProxyType.SshSock5) ? proxyopt[2] : proxyopt[0]);

		private Toplevel GuiToplevel { get; set; } = default;
		private MenuBar GuiMenu { get; set; } = default;
		private MenuBarItem urlmenu { get; set; } = default;
		private ListView listView { get; set; } = default;
		private FrameView frameForm { get; set; } = default;
		private FrameView frameList { get; set; } = default;

		private Button buttonPaste { get; set; } = default;
		private Button buttonSave { get; set; } = default;
		private Button buttonClear { get; set; } = default;
		private Button buttonDelete { get; set; } = default;
		private Button buttonImport { get; set; } = default;
		private Button buttonExport { get; set; } = default;

		private Label nameLabel { get; set; } = default;
		private Label loginLabel { get; set; } = default;
		private Label passLabel { get; set; } = default;
		private Label hostLabel { get; set; } = default;
		private Label portLabel { get; set; } = default;
		private Label proxyLabel { get; set; } = default;
		private Label expireLabel { get; set; } = default;

		private TextField nameText { get; set; } = default;
		private TextField loginText { get; set; } = default;
		private TextField passText { get; set; } = default;
		private TextField hostText { get; set; } = default;
		private TextField portText { get; set; } = default;
		private ComboBox  proxyType { get; set; } = default;

		private DateField expireDate { get; set; } = default;
		private CheckBox  expireBox { get; set; } = default;
		private CheckBox  enableBox { get; set; } = default;

		private bool isNotExpire { get; set; } = true;
		private DateTime expireStore { get; set; } = DateTime.MinValue;
		private string selectedName { get; set; } = string.Empty;
		private SshAccount account { get; set; } = default;
		private GuiRunOnce runOnce = new();
		private List<string> data = new();

		public Toplevel GetTop => GuiToplevel;

		public GuiSshAccountWindow () : base (RES.GUISSH_TITLE1, 0)
		{
			X = 0;
			Y = 1;
			Width = Dim.Fill ();
			Height = Dim.Fill () - 1;
			GuiToplevel = GuiExtensions.CreteTop ();
		}

		public new void Dispose() {

			account = default;
			this.GetType().IDisposableObject(this);
			base.Dispose();
		}

		#region Init
		public GuiSshAccountWindow Init(string __)
		{
			frameList = new FrameView (new Rect (0, 0, 35, 25), RES.TAG_ACCOUNTS) {
				X = 1,
				Y = 1
			};
			frameForm = new FrameView (new Rect (0, 0, 80, 25), $"{RES.TAG_ACCOUNT} {GetInTitle()}") {
				X = 37,
				Y = 1
			};
			listView = new ListView (data) {
				X = 1,
				Y = 1,
				Width = Dim.Fill () - 4,
				Height = Dim.Fill () - 4,
				AllowsMarking = true,
				AllowsMultipleSelection = false
			};
			listView.OpenSelectedItem += ListView_OpenSelectedItem;
			listView.SelectedItemChanged += ListView_SelectedItemChanged;

			frameList.Add (listView);
			Add (frameList);

			frameForm.Add (loginLabel = new Label (RES.TAG_LOGIN) {
				X = 1,
				Y = 1,
				AutoSize = true
			});
			frameForm.Add (loginText = new TextField (string.Empty) {
				X = labelOffset,
				Y = 1,
				Width = 30,
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
				Width = 30,
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
				Width = 30,
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
			frameForm.Add(buttonPaste = new Button(10, 19, RES.BTN_PASTE)
			{
				X = labelOffset + 21,
				Y = 7,
				AutoSize = true
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
				Width = 30,
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
				Width = 30,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});

			frameForm.Add (expireLabel = new Label (RES.TAG_EXPIRE) {
				X = 1,
				Y = 14,
				AutoSize = true
			});
			frameForm.Add (expireDate = new DateField (3, 12, DateTime.Now) {
				X = labelOffset,
				Y = 14,
				Width = 12,
				Height = 1,
				Enabled = !isNotExpire,
				ColorScheme = GuiApp.ColorField
			});
			frameForm.Add (expireBox = new CheckBox (1, 0, RES.CHKBOX_EXPIRE) {
				X = labelOffset + 15,
				Y = 14,
				Width = 10,
				Height = 1,
				Checked = isNotExpire
			});
			frameForm.Add (enableBox = new CheckBox (1, 0, RES.TAG_ENABLE) {
				X = labelOffset + 29,
				Y = 14,
				Width = 10,
				Height = 1,
				Checked = true
			});
			expireBox.Toggled += IsExpireBox_Toggled;
			enableBox.Toggled += EnableBox_Toggled;

			frameForm.Add (buttonSave = new Button (10, 19, RES.BTN_SAVE) {
				X = labelOffset,
				Y = 16,
				AutoSize = true,
				Enabled = false,
				TabIndex = 13
			});
			frameForm.Add (buttonClear = new Button (10, 19, RES.BTN_CLEAR) {
				X = 21,
				Y = 16,
				AutoSize = true,
				Enabled = false,
				TabIndex = 14
			});
			frameForm.Add (buttonDelete = new Button (10, 19, RES.BTN_DELETE) {
				X = 31,
				Y = 16,
				AutoSize = true,
				Enabled = false,
				TabIndex = 15
			});
			frameForm.Add (buttonImport = new Button (10, 19, RES.BTN_IMPORT) {
				X = 42,
				Y = 16,
				AutoSize = true,
				TabIndex = 16
			});
			frameForm.Add (buttonExport = new Button (10, 19, RES.BTN_EXPORT) {
				X = 53,
				Y = 16,
				AutoSize = true,
				Enabled = false,
				TabIndex = 17
			});
			buttonSave.Clicked += () => SaveItem();
			buttonClear.Clicked += () => Clean();
			buttonDelete.Clicked += () => Delete();
			buttonPaste.Clicked += async () => await FromClipBoard().ConfigureAwait(false);
			buttonImport.Clicked += async () => {
				GuiOpenDialog d = string.Format(RES.GUIACCOUNT_FMT6, RES.TAG_OPEN_IMPORT, tag).GuiOpenDialogs(true, extension);
				Application.Run(d);
				if (!d.Canceled) {
					try {
						string[] ss = d.GuiReturnDialog();
						if (ss.Length > 0) {
							foreach (string s in ss) {
								try {
									SshAccount a = new();
									bool b = await a.Load(s).ConfigureAwait(false);
									AddItem(a, b);
								} catch (Exception ex) { ex.StatusBarError(); }
							}
							_ = await Global.Instance.SshProxy.Save().ConfigureAwait(false);
                            _ = await LoadSshAccounts_().ConfigureAwait(false);
                        }
					} catch (Exception ex) { ex.StatusBarError(); }
				}
			};
			buttonExport.Clicked += async () => {
				try {
					if (account == default) {
						if (string.IsNullOrEmpty(selectedName))
							return;

						account = (from i in Global.Instance.SshProxy.Items
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
								_ = await account.Save(ss[0]).ConfigureAwait(false);
						} catch (Exception ex) { ex.StatusBarError(); }
					}
				} catch (Exception ex) { ex.StatusBarError(); }
			};
			Add(frameForm);

			urlmenu = new MenuBarItem("_Url", new MenuItem[0]);
			GuiMenu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
					new MenuItem (RES.MENU_RELOAD, "", async () => {
						_ = await Global.Instance.SshProxy.Load().ConfigureAwait(false);
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

		private async Task<bool> FromClipBoard() =>
			await Task.Run(() => {
				try {

					if (Clipboard.Contents.IsEmpty)
						return false;

					Clean();
					SshAccountConverter converter = new(Clipboard.Contents.ToString());
					if (converter.Convert()) {

						SshAccount a = converter.Account;
						Application.MainLoop.Invoke(() => {
							nameText.Text = a.Name;
							loginText.Text = a.Login;
							passText.Text = a.Pass;
							hostText.Text = a.Host;
							portText.Text = a.Port.ToString();
							proxyType.SelectedItem = ProxyTypeSelect(a.Type);
							frameForm.Title = string.IsNullOrWhiteSpace(a.Name) ?
								$"{RES.TAG_ACCOUNT} {a.Host}" : $"{RES.TAG_ACCOUNT} {a.Login} - {a.Name}";

							expireLabel.ColorScheme = Colors.Base;
							enableBox.Checked = true;
							EnableBox_Toggled(!a.Enable);
							expireStore = a.Expired;
							expireDate.Date = expireStore;
							expireBox.Checked = a.Expired == DateTime.MinValue;
							expireDate.Enabled = a.Enable && !expireBox.Checked;
							account = a;
							selectedName = a.Name;
							buttonSave.Enabled = buttonClear.Enabled = buttonDelete.Enabled = true;
						});
                        return true;
					}
				} catch (Exception ex) { ex.StatusBarError(); }
				return false;
			});

		public async void Load() => _ = await Load_().ConfigureAwait(false);
		private async Task<bool> Load_() =>
			await Task.Run(async () => {
                if (!runOnce.GoRun())
                    return false;
                try {
                    _ = await LoadSshAccounts_().ConfigureAwait(false);
                    try {
                        MenuItem[] mitems = await nameof(GuiSshAccountWindow).LoadMenuUrls().ConfigureAwait(false);
                        Application.MainLoop.Invoke(() => urlmenu.Children = mitems);
                    } catch { }
                } finally { runOnce.EndRun(); }
                return true;
			});

        private async Task<bool> LoadSshAccounts_() =>
            await Task.Run(async () => {
                try {
                    DataClear();
                    foreach (SshAccount a in Global.Instance.SshProxy.Items)
                        data.Add(a.Name);
                    await listView.SetSourceAsync(data).ConfigureAwait(false);
                    Application.MainLoop.Invoke(() =>
						frameList.Title = selectedName.GetListTitle(data.Count));
                    Clean();
                }
                catch (Exception ex) { ex.StatusBarError(); }
                return true;
            });

        private void DataClear() {
			data.Clear();
            Clean();
            Application.MainLoop.Invoke(() => frameList.Title = selectedName.GetListTitle(0));
		}
		private void Clean() {
			Application.MainLoop.Invoke(() => {
				nameText.Text =
				passText.Text =
				portText.Text =
				hostText.Text =
				loginText.Text = string.Empty;
				proxyType.SelectedItem = 0;
				enableBox.Checked = true;
				expireDate.Date = DateTime.Now.AddDays(7.0);
				isNotExpire = false;
				expireDate.Enabled = isNotExpire;
				expireBox.Checked = !isNotExpire;
				account = default;
                selectedName = string.Empty;
            });
        }

		private async void Delete() {

			if (!runOnce.IsRange(data.Count) || !runOnce.GoRun())
				return;

			try {
                string s = data[runOnce.LastId];
				if (string.IsNullOrWhiteSpace(s))
					return;

				if (MessageBox.Query(50, 7,
					string.Format(RES.GUIACCOUNT_FMT5, RES.BTN_DELETE.ClearText(), s),
					string.Format(RES.GUIACCOUNT_FMT3, RES.BTN_DELETE.ClearText(), s), RES.TAG_YES, RES.TAG_NO) == 0) {
					try {
						SshAccount a = (from i in Global.Instance.SshProxy.Items
										where i.Name.Equals(s)
										select i).FirstOrDefault();
						if (a == default)
							return;

						Global.Instance.SshProxy.Items.Remove(a);
						_ = await Global.Instance.SshProxy.Save().ConfigureAwait(false);
                        _ = await LoadSshAccounts_().ConfigureAwait(false);
                        runOnce.ResetId();
                    }
                    catch (Exception ex) { ex.StatusBarError(); }
				}
			} finally { runOnce.EndRun(); }
		}

		private void IsExpireBox_Toggled(bool b) =>
			Application.MainLoop.Invoke(() => {
				isNotExpire = !b;
				expireDate.Enabled = b;
				if (!b) {
					expireStore = expireDate.Date;
					expireDate.Date = DateTime.MinValue;
				} else {
					if (expireStore == DateTime.MinValue)
						expireStore = DateTime.Now.AddDays(5.0);
					expireDate.Date = expireStore;
				}
			});

		private void EnableBox_Toggled (bool b) =>
			Application.MainLoop.Invoke(() => {
				buttonClear.Enabled =
				buttonExport.Enabled =
				loginText.Enabled =
				passText.Enabled =
				hostText.Enabled =
				portText.Enabled =
				nameText.Enabled =
				expireDate.Enabled = !b;
				buttonClear.Enabled =
				buttonDelete.Enabled = !b;
                buttonSave.Enabled =
				buttonImport.Enabled =
				buttonPaste.Enabled = true;
			});

		private void ListView_OpenSelectedItem(ListViewItemEventArgs obj) { runOnce.ResetId(); SelectedListItem(obj); }
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
				SshAccount a = (from i in Global.Instance.SshProxy.Items
								where i.Name.Equals(s)
								select i).FirstOrDefault();

				if (a == default) {
					buttonSave.Enabled = buttonClear.Enabled =
					buttonDelete.Enabled = buttonExport.Enabled = false;
					return;
				}

				selectedName = s;

				nameText.Text = a.Name;
				loginText.Text = a.Login;
				passText.Text = a.Pass;
				hostText.Text = a.Host;
				portText.Text = a.Port.ToString();
				proxyType.SelectedItem = ProxyTypeSelect(a.Type);
				frameForm.Title = string.IsNullOrWhiteSpace(a.Name) ?
					$"{RES.TAG_ACCOUNT} {GetInTitle(a.Type)}" : $"{RES.TAG_ACCOUNT} {GetInTitle(a.Type)} - {a.Name}";

				if (a.IsExpired)
					expireLabel.ColorScheme = Colors.Error;
				else
					expireLabel.ColorScheme = Colors.Base;

				enableBox.Checked = a.Enable;
				EnableBox_Toggled(!a.Enable);
				expireStore = a.Expired;
				expireDate.Date = expireStore;
				expireBox.Checked = a.Expired == DateTime.MinValue;
				expireDate.Enabled = a.Enable && !expireBox.Checked;
				account = a;

				buttonSave.Enabled = buttonClear.Enabled = a.Enable;
				buttonDelete.Enabled = buttonExport.Enabled = true;

			} finally { runOnce.EndRun(); }
		}

		private async void SaveItem() {

			if (!runOnce.GoRun())
				return;

			try {
				selectedName = nameText.Text.ToString();
                bool b = string.IsNullOrEmpty(selectedName);
				if (b) {
					SshAccount a = NewItem();
					if (a == default)
						return;
					BuildItem(a);
					AddItem(a, b);
					account = a;
					selectedName = a.Name;
                    _ = await Global.Instance.SshProxy.Save().ConfigureAwait(false);
                    _ = await LoadSshAccounts_().ConfigureAwait(false);
                } else {
					SshAccount a = (from i in Global.Instance.SshProxy.Items
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
					await Global.Instance.SshProxy.Save().ConfigureAwait(false);
				}
			} finally { runOnce.EndRun(); }
		}

		private SshAccount NewItem() {
			if (string.IsNullOrEmpty(loginText.Text.ToString()))
				return default;
			return new SshAccount();
		}

		private void BuildItem(SshAccount a) {
			a.Enable = enableBox.Checked;
			a.Expired = expireDate.Date;

			a.Login = loginText.Text.ToString();
			a.Pass = passText.Text.ToString();
			a.Host = hostText.Text.ToString();
            a.Name = nameText.Text.ToString();
            a.Type = ProxyTypeSelect(proxyType.SelectedItem);
			if (int.TryParse(portText.Text.ToString(), out int port))
				a.Port = port;
		}

		private async void AddItem(SshAccount a, bool b) {
			if (!b) return;
			try {
				Global.Instance.SshProxy.Add(a);
				data.Add(a.Name);
				await listView.SetSourceAsync(data).ConfigureAwait(false);
			} catch (Exception ex) { ex.StatusBarError(); }
		}

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

	}
}

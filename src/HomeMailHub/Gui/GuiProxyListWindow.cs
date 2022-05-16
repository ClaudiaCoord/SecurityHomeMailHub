
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NStack;
using SecyrityMail;
using SecyrityMail.Proxy;
using SecyrityMail.Utils;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    internal class GuiProxyListWindow : Window, IGuiWindow<GuiProxyListWindow>
    {
		private const int labelOffset = 12;
		private static readonly ustring[] proxyopt = new ustring[] { "_None", "_Http", "Http_s", "Sock_4", "Sock_5" };

		private Toplevel GuiToplevel { get; set; } = default;
		private MenuBar GuiMenu { get; set; } = default;
		private MenuBarItem urlmenu { get; set; } = default;
		private ListView  listView { get; set; } = default;
		private FrameView frameSelect { get; set; } = default;
		private FrameView frameForm { get; set; } = default;
		private FrameView frameList { get; set; } = default;
		private FrameView frameCheck { get; set; } = default;

		private Button buttonCheckHost { get; set; } = default;
		private Button buttonCheckAll { get; set; } = default;
		private Button buttonPaste { get; set; } = default;
		private Button buttonSave { get; set; } = default;
		private Button buttonClear { get; set; } = default;
		private Button buttonDelete { get; set; } = default;

		private Label busyLabel { get; set; } = default;
		private Label hostLabel { get; set; } = default;
		private Label portLabel { get; set; } = default;

		private TextField hostText { get; set; } = default;
		private TextField portText { get; set; } = default;
		private TextView  logView { get; set; } = default;
		private RadioGroup proxySelectType { get; set; } = default;

		private ProxyType proxyType { get; set; } = ProxyType.None;
		private List<string> data = new();
		private List<string> log = new();
		private GuiRunOnce runOnce = new();
		private CancellationTokenSafe tokenSafe { get; set; } = new();
		private bool IsEmptyForm =>
			string.IsNullOrWhiteSpace(hostText.Text.ToString()) && string.IsNullOrWhiteSpace(portText.Text.ToString());

		public Toplevel GetTop => GuiToplevel;

		public GuiProxyListWindow() : base(RES.GUIPROXY_TITLE1, 0)
		{
			X = 0;
			Y = 1;
			Width = Dim.Fill();
			Height = Dim.Fill() - 1;
			GuiToplevel = GuiExtensions.CreteTop();
		}

		public new void Dispose() {

			this.GetType().IDisposableObject(this);
			base.Dispose();
		}

		#region Init
		public GuiProxyListWindow Init(string __)
		{
			frameList = new FrameView(new Rect(0, 0, 35, 25), RES.GUIPROXY_TITLE1)
			{
				X = 1,
				Y = 1,
			};
			frameSelect = new FrameView(new Rect(0, 0, 80, 5), $"{RES.GUIPROXY_TITLE2} - {proxyType}")
			{
				X = 37,
				Y = 1
			};
			frameForm = new FrameView(new Rect(0, 0, 80, 10), RES.GUIPROXY_TITLE3)
			{
				X = 37,
				Y = 6
			};
			frameCheck = new FrameView(new Rect(0, 0, 80, 10), $"{RES.GUIPROXY_TITLE4} - {proxyType}")
			{
				X = 37,
				Y = 16
			};
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

			logView = new TextView()
			{
				X = 1,
				Y = 0,
				Width = Dim.Fill(),
				Height = Dim.Fill(),
				Multiline = true,
				ReadOnly = true
			};
			frameCheck.Add(logView);
			Add(frameCheck);

			frameSelect.Add(proxySelectType = new RadioGroup(proxyopt)
			{
				X = 1,
				Y = 1,
				Width = 15,
				DisplayMode = DisplayModeLayout.Horizontal,
				SelectedItem = 1,
				NoSymbol = true
			});
			frameSelect.Add(busyLabel = new Label("  ")
			{
				X = 39,
				Y = 1,
				AutoSize = true
			});
			proxySelectType.SelectedItemChanged += ProxySelectType_SelectedItemChanged;
			Add(frameSelect);

			frameForm.Add(hostLabel = new Label(RES.TAG_HOST)
			{
				X = 1,
				Y = 1,
				AutoSize = true
			});
			frameForm.Add(hostText = new TextField(string.Empty)
			{
				X = labelOffset,
				Y = 1,
				Width = 30,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameForm.Add(portLabel = new Label(RES.TAG_PORT)
			{
				X = 1,
				Y = 3,
				AutoSize = true
			});
			frameForm.Add(portText = new TextField(string.Empty)
			{
				X = labelOffset,
				Y = 3,
				Width = 10,
				Height = 1,
				ColorScheme = GuiApp.ColorField
			});
			frameForm.Add(buttonCheckHost = new Button(10, 19, RES.BTN_CHECK)
			{
				X = labelOffset + 11,
				Y = 3,
				AutoSize = true,
				Enabled = false
			});
			frameForm.Add(buttonPaste = new Button(10, 19, RES.BTN_PASTE)
			{
				X = labelOffset + 21,
				Y = 3,
				AutoSize = true,
				Enabled = false
			});

			frameForm.Add(buttonSave = new Button(10, 19, RES.BTN_SAVE)
			{
				X = labelOffset,
				Y = 6,
				AutoSize = true,
				Enabled = false,
				TabIndex = 13
			});
			frameForm.Add(buttonClear = new Button(10, 19, RES.BTN_CLEAR)
			{
				X = 21,
				Y = 6,
				AutoSize = true,
				Enabled = false,
				TabIndex = 14
			});
			frameForm.Add(buttonDelete = new Button(10, 19, RES.BTN_DELETE)
			{
				X = 31,
				Y = 6,
				AutoSize = true,
				Enabled = false,
				TabIndex = 15
			});
			frameForm.Add(buttonCheckAll = new Button(10, 19, RES.BTN_CHECKALL)
			{
				X = 42,
				Y = 6,
				AutoSize = true,
				Enabled = false,
				TabIndex = 15
			});
			buttonCheckHost.Clicked += () => CheckProxy();
			buttonCheckAll.Clicked += () => CheckProxyes();
			buttonSave.Clicked += () => SaveItem();
			buttonClear.Clicked += () => CleanForm();
			buttonDelete.Clicked += () => Delete();
			buttonPaste.Clicked += async () => await FromClipBoard().ConfigureAwait(false);
			Add(frameForm);

			urlmenu = new MenuBarItem("_Url", new MenuItem[0]);
			GuiMenu = new MenuBar(new MenuBarItem[] {
				new MenuBarItem (RES.MENU_MENU, new MenuItem [] {
					new MenuItem (RES.GUIPROXY_MENU1, "", async () =>
						_ = await FromDownload(ProxyType.All).ConfigureAwait(false), null, null, Key.AltMask | Key.D),
					new MenuItem (RES.GUIPROXY_MENU2, "", async () =>
						_ = await FromDownload(ProxyType.Http).ConfigureAwait(false), null, null, Key.AltMask | Key.H),
					new MenuItem (RES.GUIPROXY_MENU3, "", async () =>
						_ = await FromDownload(ProxyType.Sock5).ConfigureAwait(false), null, null, Key.AltMask | Key.S),
					null,
					new MenuItem (RES.MENU_CLOSE, "", () => Application.RequestStop(), null, null, Key.AltMask | Key.Q)
				}),
				urlmenu
			});
			GuiToplevel.Add(GuiMenu, this);
			return this;
		}
        #endregion

        #region Select proxy type
		private async void ProxySelectType_SelectedItemChanged(SelectedItemChangedArgs obj)
        {
			if ((obj == null) || (obj.SelectedItem < 0) || (obj.SelectedItem >= proxyopt.Length) || !runOnce.GoRun(SetBusy))
				return;
			try {
				ustring s = proxyopt[obj.SelectedItem].Replace("_", "");
				frameSelect.Title = $"{RES.GUIPROXY_TITLE2} - {s}";

				ProxyType pt = ProxyType.None,
						  oldtype = proxyType;
				try {
					if (Enum.TryParse(s.ToString(), out pt) && IsValidType(pt)) {
						proxyType = pt;
					} else {
						DataClear();
						return;
					}
				}
				finally {
					if (pt == ProxyType.None) {
						SelectTypeStateChange(false);
						runOnce.EndRun(SetBusy);
					}
				}
				if (pt != ProxyType.None) {
					await LoadProxyList(pt, oldtype, (pt != oldtype) ? data : default).ContinueWith((t) => {
						SelectTypeStateChange(true);
						runOnce.EndRun(SetBusy);
					}).ConfigureAwait(false);
				}

			} catch (Exception ex) { ex.StatusBarError(); runOnce.EndRun(SetBusy); }
		}
		private void SelectTypeStateChange(bool b) =>
			Application.MainLoop.Invoke(() => {
				buttonCheckAll.Enabled = buttonPaste.Enabled =
				buttonSave.Enabled = buttonClear.Enabled = b;
				buttonDelete.Enabled = b && !IsEmptyForm;
				listView.Redraw(listView.Bounds);
			});
        #endregion

        #region Load/Clipboard/Download
        private async Task<bool> FromClipBoard() =>
			await Task.Run(async () => {
				if (Clipboard.Contents.IsEmpty) {
					RES.GUIPROXY_TXT19.StatusBarText();
					return false;
				}
				if (!runOnce.GoRun(SetBusy))
					return false;

				try
				{
					int count = data.Count;
					ProxyListConverter converter = new();
					List<string> list = await converter.HidemyConvert(Clipboard.Contents.ToString(), data)
													   .ConfigureAwait(false);

					if (list.Count == 0) {
						Application.MainLoop.Invoke(() => frameList.Title = $"{RES.GUIPROXY_TITLE1} : {data.Count}");
						$"{RES.GUIPROXY_TXT10}!".StatusBarText();
					} else {
						Application.MainLoop.Invoke(() => {
							data.AddRange(list);
							count = data.Count - count;
							listView.SetFocus();
							frameList.Title = $"{RES.GUIPROXY_TITLE1} : {data.Count}";
						});
						string.Format(RES.GUIPROXY_FMT2, count).StatusBarText();
					}
				}
				catch (Exception ex) { ex.StatusBarError(); }
				finally { runOnce.EndRun(SetBusy); }
				return true;
			});

		private async Task<bool> FromDownload(ProxyType type) =>
			await Task.Run(async () => {
				if (!runOnce.GoRun(SetBusy))
					return false;

				try {
					Application.MainLoop.Invoke(() => busyLabel.ColorScheme = GuiApp.ColorGreen);
					ProxyListConverter converter = new();
					_ = converter.SpysMeConvert(type)
								 .ConfigureAwait(false)
								 .GetAwaiter()
								 .GetResult();

					_ = await LoadProxyList(type, ProxyType.None).ConfigureAwait(false);
					if (data.Count == 0) {
						Application.MainLoop.Invoke(() => frameList.Title = $"{RES.GUIPROXY_TITLE1} : {data.Count}");
						$"{RES.GUIPROXY_TXT15}!".StatusBarText();
					} else {
						Application.MainLoop.Invoke(() => {
							listView.SetFocus();
							frameList.Title = $"{RES.GUIPROXY_TITLE1} : {data.Count}";
						});
						string.Format(RES.GUIPROXY_FMT1, type, data.Count).StatusBarText();
					}
				}
				catch (Exception ex) { ex.StatusBarError(); }
				finally { runOnce.EndRun(SetBusy); }
				return true;
			});

		public async void Load() => _ = await Load_().ConfigureAwait(false);
		private async Task<bool> Load_() =>
			await Task<bool>.Run(async () => {
                try {
                    MenuItem[] mitems = await nameof(GuiProxyListWindow).LoadMenuUrls().ConfigureAwait(false);
                    Application.MainLoop.Invoke(() => urlmenu.Children = mitems);
                } catch { }
				return true;
			});

		private async Task<bool> LoadProxyList(ProxyType type, ProxyType listtype, List<string> save = default) =>
			await Task.Run(async () => {
				try {
					DataClear();

					if (!IsValidType(type))
						return false;

					List<string> list = await Global.Instance.Proxy.GetAndSaveProxyesAsString(type, listtype, save)
																   .ConfigureAwait(false);
					if (list.Count == 0) {
						Application.MainLoop.Invoke(() => frameList.Title = $"{RES.GUIPROXY_TITLE1} : 0");
						$"{RES.GUIPROXY_TXT14}!".StatusBarText();
					} else {
						Application.MainLoop.Invoke(() => {
							data.AddRange(list);
							listView.SetSource(data);
							listView.Redraw(listView.Bounds);
							listView.SetFocus();
							frameList.Title = $"{RES.GUIPROXY_TITLE1} : {data.Count}";
						});
					}
				} catch (Exception ex) { ex.StatusBarError(); }
				return true;
			});
		#endregion

		#region Clean/Delete
		private void CleanForm() {

			if (!runOnce.GoRun(SetBusy))
				return;
			try {
				if (IsEmptyForm) {
					if (data.Count == 0)
						return;

					Application.MainLoop.Invoke(() => {
						if (MessageBox.Query(50, 7,
							RES.GUIPROXY_TXT9, $"{RES.GUIPROXY_TXT9}?", RES.TAG_YES, RES.TAG_NO) == 0) {
							DataClear();
							RES.GUIPROXY_TXT8.StatusBarText();
						}
					});
					return;
				}
				Clean();
			}
			finally { runOnce.EndRun(SetBusy); }
		}
		private void Clean() {
			portText.Text =
			hostText.Text = string.Empty;
			buttonCheckHost.Enabled =
			buttonDelete.Enabled = false;
		}
		private void DataClear() {
			Application.MainLoop.Invoke(() => {
				Clean();
				data.Clear();
				frameList.Title = $"{RES.GUIPROXY_TITLE1} : 0";
			});
			runOnce.ResetId();
		}

		private void Delete() {

			if (!runOnce.IsRange(data.Count) || !runOnce.GoRun(SetBusy))
				return;

			try {
				string s = data[runOnce.LastId];
				if (string.IsNullOrWhiteSpace(s))
					s = "unknown";

				if (MessageBox.Query(50, 7,
					$"{RES.GUIPROXY_TXT12} '{s}'",
					$"{RES.GUIPROXY_TXT13} '{s}' ?", RES.TAG_YES, RES.TAG_NO) == 0) {
					try {
						data.RemoveAt(runOnce.LastId);
						Clean();
					} catch (Exception ex) { ex.StatusBarError(); }
				}
			}
			catch (Exception ex) { ex.StatusBarError(); }
			finally { runOnce.EndRun(SetBusy); }
		}
		#endregion

		#region ListView/Select/Save item
		private void ListView_OpenSelectedItem(ListViewItemEventArgs obj) => SelectedListItem(obj);
		private void ListView_SelectedItemChanged(ListViewItemEventArgs obj) => SelectedListItem(obj);
		private void SelectedListItem(ListViewItemEventArgs obj) {
			if (obj == null)
				return;
			if ((obj.Item >= 0) && (obj.Item < data.Count))
				SelectItem(data[obj.Item], obj.Item);
		}

		private void SelectItem(string s, int id) {

			if (!runOnce.GoRun(id, SetBusy))
				return;
			try {
				if (string.IsNullOrEmpty(s)) {
					Clean();
					return;
				}

				int idx = s.IndexOf(':');
				if (idx > 0) {

					string[] ss = s.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
					if ((ss != null) && (ss.Length >= 2)) {
						if (!string.IsNullOrWhiteSpace(ss[0]))
							hostText.Text = ss[0].Trim();
						if (!string.IsNullOrWhiteSpace(ss[1]))
							portText.Text = ss[1].Trim();
					} else
						hostText.Text = s.Trim();
				} else {
					hostText.Text = s.Trim();
				}
				buttonCheckHost.Enabled =
				buttonDelete.Enabled = true;
			} finally { runOnce.EndRun(SetBusy); }
		}

		private void SaveItem() {

			if (!runOnce.GoRun(SetBusy))
				return;

			try {
				if (IsEmptyForm) {

					if (data.Count == 0)
						return;

					Application.MainLoop.Invoke(async () => {
						if (MessageBox.Query(50, 7,
							RES.GUIPROXY_TXT16, RES.GUIPROXY_TXT17, RES.TAG_YES, RES.TAG_NO) == 0) {
							_ = await Global.Instance.Proxy.SaveProxyes(proxyType, data)
														   .ConfigureAwait(false);
							RES.GUIPROXY_TXT18.StatusBarText();
						}
					});
					return;
				}

				string host = hostText.Text.ToString(),
					   port = portText.Text.ToString(),
					   uri;

				if (string.IsNullOrWhiteSpace(host)) {
					hostText.ColorScheme = Colors.Error;
					return;
				}
				hostText.ColorScheme = Colors.Base;

				if (string.IsNullOrWhiteSpace(port)) {
					portText.ColorScheme = Colors.Error;
					return;
				}
				portText.ColorScheme = Colors.Base;

				uri = $"{host}:{port}";
				if (!data.Contains(uri))
					data.Add(uri);
			} finally { runOnce.EndRun(SetBusy); }
		}
		#endregion

		#region Check Proxyes
		private async void CheckProxyes() {
			if (Global.Instance.Proxy.IsProxyCheck) {

				if (!tokenSafe.IsCancellationRequested)
					tokenSafe.Cancel();

				Application.MainLoop.Invoke(() => {
					frameCheck.Title = RES.GUIPROXY_TITLE4;
					buttonCheckAll.ColorScheme = GuiApp.ColorRed;
					RES.GUIPROXY_TXT1.StatusBarText();
				});
				return;

			} else {
				if (proxyType == ProxyType.None) {
					RES.GUIPROXY_TXT2.StatusBarText();
					return;
				}
				if (data.Count == 0) {
					RES.GUIPROXY_TXT3.StatusBarText();
					return;
				}
				if (!runOnce.GoRun(SetBusy)) {
					RES.GUIPROXY_TXT4.StatusBarText();
					return;
				}
				if (tokenSafe.IsCancellationRequested)
					tokenSafe.Reload();
				try {
					_goodCheck = _badCheck = 0;
					frameCheck.Title = $"{RES.GUIPROXY_TITLE4} - {proxyType}";
					buttonCheckAll.ColorScheme = GuiApp.ColorGreen;
					RES.GUIPROXY_TXT5.StatusBarText();
					List<string> list = new(data);
					await Global.Instance.Proxy.CheckProxyes(proxyType, list, tokenSafe.Token, UpdateLogPanel)
											   .ContinueWith(async (t) => {
												   Application.MainLoop.Invoke(() => {
													   buttonCheckAll.ColorScheme = Colors.Base;
													   string.Empty.StatusBarText();
												   });
												   await LoadProxyList(proxyType, proxyType, list)
															.ContinueWith((t) => runOnce.EndRun(SetBusy))
															.ConfigureAwait(false);
											   }).ConfigureAwait(false);
				} catch (Exception ex) { ex.StatusBarError(); runOnce.EndRun(SetBusy); }
			}
		}

		private async void CheckProxy() {

			if (Global.Instance.Proxy.IsProxyCheck || runOnce.IsLocked) {

				if (!tokenSafe.IsCancellationRequested)
					tokenSafe.Cancel();
				Application.MainLoop.Invoke(() => {
					frameCheck.Title = RES.GUIPROXY_TITLE5;
					buttonCheckHost.ColorScheme = GuiApp.ColorRed;
					RES.GUIPROXY_TXT1.StatusBarText();
				});
				return;
			} else {
				if (proxyType == ProxyType.None) {
					RES.GUIPROXY_TXT2.StatusBarText();
					return;
				}
				if (IsEmptyForm) {
					RES.GUIPROXY_TXT6.StatusBarText();
					return;
				}
				if (!runOnce.GoRun(SetBusy)) {
					RES.GUIPROXY_TXT4.StatusBarText();
					return;
				}
				if (tokenSafe.IsCancellationRequested)
					tokenSafe.Reload();
				try {
					_goodCheck = _badCheck = 0;
					string host = hostText.Text.ToString();
					if (!int.TryParse(portText.Text.ToString(), out int port)) {
						$"{host} {RES.GUIPROXY_TXT11}".StatusBarText();
						return;
					}
					frameCheck.Title = $"{RES.GUIPROXY_TITLE5} - {proxyType} - {host}:{port}";
					buttonCheckHost.ColorScheme = GuiApp.ColorGreen;
					$"{RES.GUIPROXY_TXT7} {hostText.Text}:{portText.Text}..".StatusBarText();
					await Global.Instance.Proxy.CheckProxyHost(proxyType, host, port, tokenSafe.Token, UpdateLogPanel)
											   .ContinueWith((t) => {
												   Application.MainLoop.Invoke(() => {
													   buttonCheckHost.ColorScheme = Colors.Base;
													   string.Empty.StatusBarText();
												   });
												   runOnce.EndRun(SetBusy);
											   }).ConfigureAwait(false);
				} catch (Exception ex) { ex.StatusBarError(); runOnce.EndRun(SetBusy); }
			}
		}

		private void UpdateLogPanel(string _, string txt) =>
			Application.MainLoop.Invoke(() => UpdateLogPanel(txt));

		private int _goodCheck = 0, _badCheck = 0;
		private static readonly object __lockLog = new();
		private void UpdateLogPanel(string txt) {
			lock (__lockLog) {
				try {
					if (log.Count >= 8)
						log.RemoveRange(7, log.Count - 7);
					log.Insert(0, $"{DateTime.Now:HH:mm} - {txt}");
					logView.Text = string.Join(Environment.NewLine, log);

					if (txt.Contains("check ")) {
						if (txt.Contains(" OK ")) _goodCheck++;
						else if (txt.Contains(" ERROR ")) _badCheck++;
					}
					frameCheck.Title = $"{RES.GUIPROXY_TITLE4} - {proxyType} - {_goodCheck}/{_badCheck}";
				} catch { }
			}
		}
		#endregion

		private void SetBusy(bool b) =>
			Application.MainLoop.Invoke(() => busyLabel.ColorScheme = b ? GuiApp.ColorRed : Colors.Base);

		private int ProxyTypeSelect(ProxyType opt) =>
			opt switch {
				ProxyType.None => 0,
				ProxyType.Http => 1,
				ProxyType.Https => 2,
				ProxyType.Sock4 => 3,
				ProxyType.Sock5 => 4,
				_ => 0
			};

		private ProxyType ProxyTypeSelect(int idx) =>
			idx switch {
				0 => ProxyType.None,
				1 => ProxyType.Http,
				2 => ProxyType.Https,
				3 => ProxyType.Sock4,
				4 => ProxyType.Sock5,
				_ => ProxyType.None
			};

		private bool IsValidType(ProxyType opt) =>
			opt switch {
				ProxyType.Http => true,
				ProxyType.Https => true,
				ProxyType.Sock4 => true,
				ProxyType.Sock5 => true,
				_ => false
			};

	}
}

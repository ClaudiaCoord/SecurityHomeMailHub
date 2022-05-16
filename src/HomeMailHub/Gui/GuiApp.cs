
using System;
using System.Threading;
using SecyrityMail.Data;
using SecyrityMail.Utils;
using Terminal.Gui;
using Settings = HomeMailHub.Properties.Settings;
using GuiAttribute = Terminal.Gui.Attribute;
using SecyrityMail;

namespace HomeMailHub.Gui {

	public class GuiApp : MailEvent, IDisposable {

		private GuiRootWindow GuiMainWindow = default;
		private GuiRootStatusBar GuiMainStatusBar;
		private Toplevel GuiTop;

		private static GuiApp __this = default;
		public static GuiApp Get => __this;
		public GuiRootWindow GetView => GuiMainWindow;
		public GuiRootStatusBar GetViewBar => GuiMainStatusBar;

		#region static setter/getter
		private static long _sendNoWarning = 0,
							_viewLog = 1,
							_viewEvent = 1,
							_writeLog = 0,
							_windowActive = 0;

		public bool IsWindowActive {
			get => Interlocked.Read(ref _windowActive) > 0L;
			set {
				Interlocked.Exchange(ref _windowActive, value ? 1L : 0L);
				if (__this != default) __this.OnPropertyChanged();
			}
		}
		public static bool IsSendNoWarning {
			get => Interlocked.Read(ref _sendNoWarning) > 0L;
			set {
				Interlocked.Exchange(ref _sendNoWarning, value ? 1L : 0L);
				if (__this != default) __this.OnPropertyChanged();
			}
		}
		public static bool IsViewLog {
			get => Interlocked.Read(ref _viewLog) > 0L;
			set {
				Interlocked.Exchange(ref _viewLog, value ? 1L : 0L);
				if (__this != default) __this.OnPropertyChanged();
			}
		}
		public static bool IsViewEvent {
			get => Interlocked.Read(ref _viewEvent) > 0L;
			set {
				Interlocked.Exchange(ref _viewEvent, value ? 1L : 0L);
				if (__this != default) __this.OnPropertyChanged();
			}
		}
		public static bool IsWriteLog {
			get => Interlocked.Read(ref _writeLog) > 0L;
			set {
				Interlocked.Exchange(ref _writeLog, value ? 1L : 0L);
				if (__this != default) __this.OnPropertyChanged();
			}
		}
		public static bool IsLightText {
			get => Settings.Default.IsGuiLightText;
			set {
				if (Settings.Default.IsGuiLightText == value)
					return;
				Settings.Default.IsGuiLightText = value;
				InitFieldColor(value);
				if (__this != default) __this.OnPropertyChanged();
			}
		}

		public static ColorScheme ColorGreen { get; private set; }
		public static ColorScheme ColorRed { get; private set; }
		public static ColorScheme ColorField { get; private set; }

		static void InitStaticGuiApp() {
			GuiAttribute cgreen = Application.Driver.MakeAttribute(Color.White, Color.Green);
			GuiAttribute cdgreen = Application.Driver.MakeAttribute(Color.DarkGray, Color.Green);
			GuiAttribute cfgreen = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Green);

			GuiAttribute cred = Application.Driver.MakeAttribute(Color.White, Color.Red);
			GuiAttribute cdred = Application.Driver.MakeAttribute(Color.DarkGray, Color.Red);
			GuiAttribute cfred = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Red);

			GuiAttribute cnmenu = Application.Driver.MakeAttribute(Color.Black, Color.BrightCyan);
			GuiAttribute cfmenu = Application.Driver.MakeAttribute(Color.White, Color.Black);
			GuiAttribute cdmenu = Application.Driver.MakeAttribute(Color.DarkGray, Color.BrightCyan);
			GuiAttribute chfmenu = Application.Driver.MakeAttribute(Color.Red, Color.Black);
			GuiAttribute chnmenu = Application.Driver.MakeAttribute(Color.BrightYellow, Color.BrightCyan);

            GuiAttribute cndialog = Application.Driver.MakeAttribute(Color.Black, Color.Gray);
            GuiAttribute cfdialog = Application.Driver.MakeAttribute(Color.Black, Color.BrightCyan);
            GuiAttribute cddialog = Application.Driver.MakeAttribute(Color.DarkGray, Color.Gray);
            GuiAttribute chfdialog = Application.Driver.MakeAttribute(Color.White, Color.BrightCyan);
            GuiAttribute chndialog = Application.Driver.MakeAttribute(Color.Red, Color.Gray);

            ColorRed = new ColorScheme() { Normal = cred, Focus = cred, HotFocus = cfred, HotNormal = cfred, Disabled = cdred };
			ColorGreen = new ColorScheme() { Normal = cgreen, Focus = cgreen, HotFocus = cfgreen, HotNormal = cfgreen, Disabled = cdgreen };
			Colors.Menu = new ColorScheme() { Normal = cnmenu, Focus = cfmenu, HotFocus = chfmenu, HotNormal = chnmenu, Disabled = cdmenu };
            Colors.Dialog = new ColorScheme() { Normal = cndialog, Focus = cfdialog, HotFocus = chfdialog, HotNormal = chndialog, Disabled = cddialog };
            InitFieldColor(Settings.Default.IsGuiLightText);
        }
		static void InitFieldColor(bool b = false) {
			GuiAttribute cfield = Application.Driver.MakeAttribute(b ? Color.White : Color.Gray, Color.BrightBlue);
			GuiAttribute cffield = Application.Driver.MakeAttribute(Color.Blue, Color.Gray);
			ColorField = new ColorScheme() { Normal = cfield, Focus = cfield, HotFocus = cffield, HotNormal = cffield, Disabled = cffield };
		}
		static ColorScheme GetStatusBarColor()
		{
			GuiAttribute csb = Application.Driver.MakeAttribute(Color.White, Color.BrightBlue);
			GuiAttribute cfsb = Application.Driver.MakeAttribute(Color.DarkGray, Color.BrightBlue);
			return new ColorScheme() { Normal = csb, Focus = csb, HotFocus = cfsb, HotNormal = cfsb, Disabled = cfsb };
		}
		#endregion

		public GuiApp()
		{
			__this = this;
			Application.Init();
			InitStaticGuiApp();
			GuiTop = new Toplevel() {
				X = 0,
				Y = 0,
				Width = Dim.Fill(),
				Height = Dim.Fill()
			};
			GuiMainStatusBar = new();
			GuiMainStatusBar.ColorScheme = GetStatusBarColor();
			GuiMainWindow = new GuiRootWindow().Init();
			Init(GuiMainWindow.Views);
		}
		~GuiApp() => Dispose();

		public void LoadWindow(Type t, string s = default)
		{
			switch (t.Name) {
				case nameof(GuiMailAccountWindow): LoadWindow<GuiMailAccountWindow>(s); break;
				case nameof(GuiVpnAccountWindow): LoadWindow<GuiVpnAccountWindow>(s); break;
				case nameof(GuiSshAccountWindow): LoadWindow<GuiSshAccountWindow>(s); break;
				case nameof(GuiMailMessagesWindow): LoadWindow<GuiMailMessagesWindow>(s); break;
				case nameof(GuiReadMessageWindow): LoadWindow<GuiReadMessageWindow>(s); break;
				case nameof(GuiWriteMessageWindow): LoadWindow<GuiWriteMessageWindow>(s); break;
				case nameof(GuiProxyListWindow): LoadWindow<GuiProxyListWindow>(s); break;
				case nameof(GuiServicesSettingsWindow): LoadWindow<GuiServicesSettingsWindow>(s); break;
			}
		}

		private void LoadWindow<T>(string s = default) where T : Window, IGuiWindow<T>, new() {

			T child = default;
			try {
				IsWindowActive = false;
				child = new T();
				if (child is IGuiWindow<T> win) {
					try {
						win.Init(s).Load();
						win.GetTop.Add(GuiMainStatusBar);
						Application.Run(win.GetTop);
					} catch (Exception ex) { GuiMainStatusBar.UpdateStatus(GuiStatusItemId.Error, ex.Message); }
				}
			}
			catch (Exception ex) { GuiMainStatusBar.UpdateStatus(GuiStatusItemId.Error, ex.Message); }
			finally {
				IsWindowActive = true;
				ChildWindowDispose(child);
			}
		}

		private void Init(View[] views) {
			try {
				foreach (View v in views)
					GuiTop.Add(v);
				GuiTop.Add(GuiMainStatusBar);
			} catch (Exception ex) { GuiMainStatusBar.UpdateStatus(GuiStatusItemId.Error, ex.Message); }
		}

		public void Start() {
			try { Application.Run(GuiTop); }
			catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.ToString()); Global.Instance.Log.Add(nameof(GuiApp), ex); }
		}

		public void ChildWindowDispose<T>(IGuiWindow<T> child) {
			if (child != null) child.Dispose();
		}

		public void Dispose() {

			__this = default;

			Toplevel t = GuiTop;
			GuiTop = default;
			if (t != null) t.Dispose();

			Window w = GuiMainWindow;
			GuiMainWindow = default;
			if (w != null) w.Dispose();

			StatusBar s = GuiMainStatusBar;
			GuiMainStatusBar = default;
			if (s != null) s.Dispose();
		}

		#region Logs/Events
		public bool AddLog(Tuple<DateTime, string, string> t) => (GuiMainWindow != default) ? GuiMainWindow.AddLog(t) : false;
		public void AddEvent(EventActionArgs a)
		{
			Tuple<DateTime, string, string> t = default;

			switch (a.Id)
			{
				case MailEventId.None: return;
				case MailEventId.PropertyChanged:
					{
						if ("TunnelRx".Equals(a.Src))
							GuiMainStatusBar.UpdateStatus(GuiStatusItemId.Rx, ((long)a.Obj).Humanize());
						else if ("TunnelTx".Equals(a.Src))
							GuiMainStatusBar.UpdateStatus(GuiStatusItemId.Tx, ((long)a.Obj).Humanize());
						return;
					}
				case MailEventId.DateExpired:
					{
						if (a.Obj is TimeSpan ts)
							t = new(DateTime.Now, $"{a.Sender.GetType().Name}/{a.Src}", $"date expired {ts}");
						else
							t = new(DateTime.Now, $"{a.Sender.GetType().Name}/{a.Src}", "date expired");
						break;
					}
				case MailEventId.StopFetchMail:
				case MailEventId.StartFetchMail:
					{
						t = new(
							DateTime.Now, "Mail Fetch", (a.Id == MailEventId.StartFetchMail) ? "start" : "end");
						GuiMainStatusBar.UpdateStatus(
							GuiStatusItemId.ServiceName, (a.Id == MailEventId.StartFetchMail) ? "Mail Fetch" : string.Empty);
						break;
					}
				case MailEventId.BeginCall:
					GuiMainStatusBar.UpdateStatus(GuiStatusItemId.ServiceName, a.Src); return;
				case MailEventId.EndCall:
					GuiMainStatusBar.UpdateStatus(GuiStatusItemId.ServiceName, string.Empty); return;
				default:
					{
						t = new(DateTime.Now, a.Sender.GetType().Name, a.Src);
						break;
					}
			}
			_ = AddLog(t);
		}
		#endregion
	}
}

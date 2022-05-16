
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SecyrityMail;
using SecyrityMail.Data;
using Terminal.Gui;

namespace HomeMailHub.Gui
{

    public class GuiRootWindow : Window {
		private const string titleLogOnly = "Log";
		private const string titleEventOnly = "Event";
		private const string titleLogWrite = "WLog+";

		private List<Tuple<DateTime, string, string>> logList = new();
		private GuiRootMenuBar GuiMenu { get; set; } = default;

		private TextView textView { get; set; } = default;
		public TextView GetView => textView;
		public View [] Views { get; private set; } = default;

		private bool IsViewLog {
			get => GuiApp.IsViewLog;
			set { GuiApp.IsViewLog = value; UpdateLogTitle(); }
		}
		private bool IsViewEvent {
			get => GuiApp.IsViewEvent;
			set { GuiApp.IsViewEvent = value; UpdateLogTitle(); }
		}
		private bool IsWriteLog {
			get => GuiApp.IsWriteLog;
			set { GuiApp.IsWriteLog = value; UpdateLogTitle(); }
		}

		public GuiRootWindow () : base (" + ", 0) {
			X = 0;
			Y = 1;
			Width = Dim.Fill();
			Height = Dim.Fill() - 1;
			Global.Instance.EventCb += Instance_EventCb;
		}
		~GuiRootWindow() => Global.Instance.EventCb -= Instance_EventCb;

		public new void Dispose() {

			this.GetType().IDisposableObject(this);
			base.Dispose();
		}

		#region Init
		public GuiRootWindow Init()
		{
			textView = new TextView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill(),
				Height = Dim.Fill(),
				Multiline = true,
				ReadOnly = true
			};

			GuiMenu = new GuiRootMenuBar(
				UpdateLogTitle,
				() => { lock (__lock) { textView.Text = string.Empty; logList.Clear(); }});

			Add (textView);

			Views = new View [] { GuiMenu, this };
			UpdateLogTitle(); 
			return this;
		}
		#endregion

		public bool AddLog(Tuple<DateTime, string, string> t) => UpdateLogList(t);
		private void Instance_EventCb(object sender, EventActionArgs a) => GuiMenu.Load(a);
		private static readonly object __lock = new();
		private bool UpdateLogList(Tuple<DateTime, string, string> t) {
            lock (__lock) {
				try {
					if (logList.Count >= 100)
						logList.RemoveRange(99, logList.Count - 99);
					logList.Insert(0, t);
					string s = string.Join(Environment.NewLine, logList.Select(t => $"{t.Item1:HH:mm:ss} - {t.Item2} - {t.Item3}"));
					textView.Text = s;
				} catch { }
				Application.MainLoop.Invoke(() => { try { Redraw(Bounds); } catch { } });
			}
			return true;
		}

		private void UpdateLogTitle()
		{
			StringBuilder sb = new ();
			if (IsViewLog) sb.Append(titleLogOnly);
			if (IsViewLog && IsViewEvent) sb.Append (" & ");
			if (IsViewEvent) sb.Append(titleEventOnly);

			if ((sb.Length > 0) && IsWriteLog) sb.Append (" view & ");
			else if (sb.Length > 0) sb.Append (" view");

			if (IsWriteLog) sb.Append(titleLogWrite);
			if (sb.Length == 0) sb.Append ("+");
			Title = sb.ToString();
		}
	}
}

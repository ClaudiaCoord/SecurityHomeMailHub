using System;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	public class StatusItem {
		public StatusItem (Key shortcut, ustring title, Action action)
		{
			Title = title ?? "";
			Shortcut = shortcut;
			Action = action;
		}

		public Key Shortcut { get; }

		public ustring Title { get; set; }

		public Action Action { get; }
	};

	public class StatusBar : View {
		bool disposedValue;

		public StatusItem [] Items { get; set; }

		public StatusBar () : this (items: new StatusItem [] { }) { }

		public StatusBar (StatusItem [] items) : base ()
		{
			Items = items;
			CanFocus = false;
			ColorScheme = Colors.Menu;
			X = 0;
			Width = Dim.Fill ();
			Height = 1;

			Initialized += StatusBar_Initialized;
			Application.Resized += Application_Resized ();
		}

		private void StatusBar_Initialized (object sender, EventArgs e)
		{
			if (SuperView.Frame == Rect.Empty) {
				((Toplevel)SuperView).Loaded += StatusBar_Loaded;
			} else {
				Y = Math.Max (SuperView.Frame.Height - (Visible ? 1 : 0), 0);
			}
		}

		private void StatusBar_Loaded ()
		{
			Y = Math.Max (SuperView.Frame.Height - (Visible ? 1 : 0), 0);
			((Toplevel)SuperView).Loaded -= StatusBar_Loaded;
		}

		private Action<Application.ResizedEventArgs> Application_Resized ()
		{
			return delegate {
				X = 0;
				Height = 1;
				if (SuperView != null || SuperView is Toplevel) {
					if (Frame.Y != SuperView.Frame.Height - (Visible ? 1 : 0)) {
						Y = SuperView.Frame.Height - (Visible ? 1 : 0);
					}
				}
			};
		}

		static ustring shortcutDelimiter = "-";
		public static ustring ShortcutDelimiter {
			get => shortcutDelimiter;
			set {
				if (shortcutDelimiter != value) {
					shortcutDelimiter = value == ustring.Empty ? " " : value;
				}
			}
		}

		Attribute ToggleScheme (Attribute scheme)
		{
			var result = scheme == ColorScheme.Normal ? ColorScheme.HotNormal : ColorScheme.Normal;
			Driver.SetAttribute (result);
			return result;
		}

		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			Driver.SetAttribute (GetNormalColor ());
			for (int i = 0; i < Frame.Width; i++)
				Driver.AddRune (' ');

			Move (1, 0);
			var scheme = GetNormalColor ();
			Driver.SetAttribute (scheme);
			for (int i = 0; i < Items.Length; i++) {
				var title = Items [i].Title.ToString ();
				for (int n = 0; n < Items [i].Title.RuneCount; n++) {
					if (title [n] == '~') {
						scheme = ToggleScheme (scheme);
						continue;
					}
					Driver.AddRune (title [n]);
				}
				if (i + 1 < Items.Length) {
					Driver.AddRune (' ');
					Driver.AddRune (Driver.VLine);
					Driver.AddRune (' ');
				}
			}
		}

		public override bool ProcessHotKey (KeyEvent kb)
		{
			foreach (var item in Items) {
				if (kb.Key == item.Shortcut) {
					Run (item.Action);
					return true;
				}
			}
			return false;
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags != MouseFlags.Button1Clicked)
				return false;

			int pos = 1;
			for (int i = 0; i < Items.Length; i++) {
				if (me.X >= pos && me.X < pos + GetItemTitleLength (Items [i].Title)) {
					Run (Items [i].Action);
					break;
				}
				pos += GetItemTitleLength (Items [i].Title) + 3;
			}
			return true;
		}

		int GetItemTitleLength (ustring title)
		{
			int len = 0;
			foreach (var ch in title) {
				if (ch == '~')
					continue;
				len++;
			}

			return len;
		}

		void Run (Action action)
		{
			if (action == null)
				return;

			Application.MainLoop.AddIdle (() => {
				action ();
				return false;
			});
		}

		protected override void Dispose (bool disposing)
		{
			if (!disposedValue) {
				if (disposing) {
					Application.Resized -= Application_Resized ();
				}
				disposedValue = true;
			}
		}

		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}

		public void AddItemAt (int index, StatusItem item)
		{
			var itemsList = new List<StatusItem> (Items);
			itemsList.Insert (index, item);
			Items = itemsList.ToArray ();
			SetNeedsDisplay ();
		}

		public StatusItem RemoveItem (int index)
		{
			var itemsList = new List<StatusItem> (Items);
			var item = itemsList [index];
			itemsList.RemoveAt (index);
			Items = itemsList.ToArray ();
			SetNeedsDisplay ();

			return item;
		}
	}
}
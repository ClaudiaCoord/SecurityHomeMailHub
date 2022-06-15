using System;
using NStack;

namespace Terminal.Gui {

	public class CheckBox : View {
		ustring text;
		int hot_pos = -1;
		Rune hot_key;

		public event Action<bool> Toggled;

		public virtual void OnToggled (bool previousChecked)
		{
			Toggled?.Invoke (previousChecked);
		}

		public CheckBox () : this (string.Empty) { }

		public CheckBox (ustring s, bool is_checked = false) : base ()
		{
			Initialize (s, is_checked);
		}

		public CheckBox (int x, int y, ustring s) : this (x, y, s, false)
		{
		}

		public CheckBox (int x, int y, ustring s, bool is_checked) : base (new Rect (x, y, s.Length + 4, 1))
		{
			Initialize (s, is_checked);
		}

		void Initialize (ustring s, bool is_checked)
		{
			Checked = is_checked;
			Text = s;
			CanFocus = true;
			Height = 1;
			Width = s.RuneCount + 4;

			AddCommand (Command.ToggleChecked, () => ToggleChecked ());

			AddKeyBinding ((Key)' ', Command.ToggleChecked);
			AddKeyBinding (Key.Space, Command.ToggleChecked);
		}

		public bool Checked { get; set; }

		public new ustring Text {
			get {
				return text;
			}

			set {
				text = value;

				int i = 0;
				hot_pos = -1;
				hot_key = (char)0;
				foreach (Rune c in text) {
					if (c == '_') {
						hot_key = text [i + 1];
						HotKey = (Key)(char)hot_key.ToString ().ToUpper () [0];
						text = text.ToString ().Replace ("_", "");
						hot_pos = i;
						break;
					}
					i++;
				}
			}
		}

		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : GetNormalColor ());
			Move (0, 0);
			Driver.AddRune (Checked ? Driver.Checked : Driver.UnChecked);
			Driver.AddRune (' ');
			Move (2, 0);
			Driver.AddStr (Text);
			if (hot_pos != -1) {
				Move (2 + hot_pos, 0);
				Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : Enabled ? ColorScheme.HotNormal : ColorScheme.Disabled);
				Driver.AddRune (hot_key);
			}
		}

		public override void PositionCursor () => Move(0, 0);

		public override bool ProcessKey (KeyEvent kb)
		{
			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return base.ProcessKey (kb);
		}

		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (kb.Key == (Key.AltMask | HotKey))
				return ToggleChecked ();

			return false;
		}

		bool ToggleChecked ()
		{
			if (!HasFocus) {
				SetFocus ();
			}
			Checked = !Checked;
#			if TERM_EVENT_NEWVALUE
			OnToggled (Checked);
#			else
			OnToggled (!Checked);
#			endif
			SetNeedsDisplay ();
			return true;
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) || !CanFocus)
				return false;

			SetFocus ();
            Checked = !Checked;
#			if TERM_EVENT_NEWVALUE
            OnToggled(Checked);
#			else
			OnToggled (!Checked);
#			endif
			SetNeedsDisplay ();
			return true;
		}

		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}
}

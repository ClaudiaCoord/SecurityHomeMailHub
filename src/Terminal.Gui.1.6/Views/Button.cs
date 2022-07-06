using System;
using NStack;

namespace Terminal.Gui {
	public class Button : View {
		ustring text;
		bool is_default;

		public Button () : this (text: string.Empty, is_default: false) { }
        public Button(int x, int y, ustring text) : this(x, y, text, false) { }
        public Button (ustring text, bool is_default = false) : base (text) =>
			Initialize (text, is_default);
		public Button (int x, int y, ustring text, bool is_default)
		    : base (new Rect (x, y, text.RuneCount + 4 + (is_default ? 2 : 0), 1), text) =>
			Initialize (text, is_default);

		Rune _leftBracket;
		Rune _rightBracket;
		Rune _leftDefault;
		Rune _rightDefault;
		private Key hotKey = Key.Null;
		private Rune hotKeySpecifier;

		void Initialize (ustring text, bool is_default)
		{
			TextAlignment = TextAlignment.Centered;

			HotKeySpecifier = new Rune ('_');

			_leftBracket = new Rune (Driver != null ? Driver.LeftBracket : '[');
			_rightBracket = new Rune (Driver != null ? Driver.RightBracket : ']');
			_leftDefault = new Rune (Driver != null ? Driver.LeftDefaultIndicator : '<');
			_rightDefault = new Rune (Driver != null ? Driver.RightDefaultIndicator : '>');

			CanFocus = true;
			this.is_default = is_default;
			this.text = text ?? string.Empty;
			Update ();

			AddCommand (Command.Accept, () => AcceptKey ());

			AddKeyBinding (Key.Enter, Command.Accept);
			AddKeyBinding (Key.Space, Command.Accept);
			if (HotKey != Key.Null)
				AddKeyBinding (Key.Space | HotKey, Command.Accept);
		}

		public override ustring Text {
			get {
				return text;
			}
			set {
				text = value;
				TextFormatter.FindHotKey (text, HotKeySpecifier, true, out _, out Key hk);
				if (hotKey != hk) {
					HotKey = hk;
				}
				Update ();
			}
		}

		public bool IsDefault {
			get => is_default;
			set {
				is_default = value;
				Update ();
			}
		}

		public override Key HotKey {
			get => hotKey;
			set {
				if (hotKey != value) {
					var v = value == Key.Unknown ? Key.Null : value;
					if (hotKey != Key.Null && ContainsKeyBinding (Key.Space | hotKey)) {
						if (v == Key.Null) {
							ClearKeybinding (Key.Space | hotKey);
						} else {
							ReplaceKeyBinding (Key.Space | hotKey, Key.Space | v);
						}
					} else if (v != Key.Null) {
						AddKeyBinding (Key.Space | v, Command.Accept);
					}
					hotKey = v;
				}
			}
		}

		public override Rune HotKeySpecifier {
			get => hotKeySpecifier;
			set {
				hotKeySpecifier = TextFormatter.HotKeySpecifier = value;
			}
		}

		public override bool AutoSize {
			get => base.AutoSize;
			set {
				base.AutoSize = value;
				Update ();
			}
		}

		internal void Update ()
		{
#			if !TERM_NO_RUNE_DEFAULT
			if (IsDefault)
				TextFormatter.Text = ustring.Make (_leftBracket) + ustring.Make (_leftDefault) + " " + text + " " + ustring.Make (_rightDefault) + ustring.Make (_rightBracket);
			else
#			endif
				TextFormatter.Text = ustring.Make (_leftBracket) + " " + text + " " + ustring.Make (_rightBracket);

			int w = TextFormatter.Size.Width - (TextFormatter.Text.Contains (HotKeySpecifier) ? 1 : 0);
			GetCurrentWidth (out int cWidth);
			var canSetWidth = SetWidth (w, out int rWidth);
			if (canSetWidth && (cWidth < rWidth || AutoSize)) {
				Width = rWidth;
				w = rWidth;
			} else if (!canSetWidth || !AutoSize) {
				w = cWidth;
			}
			var layout = LayoutStyle;
			bool layoutChanged = false;
			if (!(Height is Dim.DimAbsolute)) {
				layoutChanged = true;
				LayoutStyle = LayoutStyle.Absolute;
			}
			Height = 1;
			if (layoutChanged) {
				LayoutStyle = layout;
			}
			Frame = new Rect (Frame.Location, new Size (w, 1));
			SetNeedsDisplay ();
		}

		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (!Enabled)
				return false;
			return ExecuteHotKey (kb);
		}

		public override bool ProcessColdKey (KeyEvent kb)
		{
			if (!Enabled)
				return false;
			return ExecuteColdKey (kb);
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			if (!Enabled) {
				return false;
			}

			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return base.ProcessKey (kb);
		}

		bool ExecuteHotKey (KeyEvent ke)
		{
			if (ke.Key == (Key.AltMask | HotKey))
				return AcceptKey ();
			return false;
		}

		bool ExecuteColdKey (KeyEvent ke)
		{
			if (IsDefault && ke.KeyValue == '\n')
				return AcceptKey ();
			return ExecuteHotKey (ke);
		}

		bool AcceptKey ()
		{
			if (!HasFocus)
				SetFocus ();
			OnClicked ();
			return true;
		}

		public virtual void OnClicked ()
		{
			Clicked?.Invoke ();
		}

		public event Action Clicked;

		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags == MouseFlags.Button1Clicked || me.Flags == MouseFlags.Button1DoubleClicked ||
				me.Flags == MouseFlags.Button1TripleClicked) {
				if (CanFocus && Enabled) {
					if (!HasFocus) {
						SetFocus ();
						SetNeedsDisplay ();
						Redraw (Bounds);
					}
					OnClicked ();
				}

				return true;
			}
			return false;
		}

		public override void PositionCursor ()
		{
			if (HotKey == Key.Unknown && text != "") {
				for (int i = 0; i < TextFormatter.Text.RuneCount; i++) {
					if (TextFormatter.Text [i] == text [0]) {
						Move (i, 0);
						return;
					}
				}
			}
			base.PositionCursor ();
		}

		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}
}

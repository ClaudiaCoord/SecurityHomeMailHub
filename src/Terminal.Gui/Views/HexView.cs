using System;
using System.Collections.Generic;
using System.IO;

namespace Terminal.Gui {
	public class HexView : View {
		SortedDictionary<long, byte> edits = new SortedDictionary<long, byte> ();
		Stream source;
		long displayStart, pos;
		bool firstNibble, leftSide;

		private long position {
			get => pos;
			set {
				pos = value;
				OnPositionChanged ();
			}
		}

		public HexView (Stream source) : base ()
		{
			Source = source;
			CanFocus = true;
			leftSide = true;
			firstNibble = true;

			AddCommand (Command.Left, () => MoveLeft ());
			AddCommand (Command.Right, () => MoveRight ());
			AddCommand (Command.LineDown, () => MoveDown (bytesPerLine));
			AddCommand (Command.LineUp, () => MoveUp (bytesPerLine));
			AddCommand (Command.ToggleChecked, () => ToggleSide ());
			AddCommand (Command.PageUp, () => MoveUp (bytesPerLine * Frame.Height));
			AddCommand (Command.PageDown, () => MoveDown (bytesPerLine * Frame.Height));
			AddCommand (Command.TopHome, () => MoveHome ());
			AddCommand (Command.BottomEnd, () => MoveEnd ());
			AddCommand (Command.StartOfLine, () => MoveStartOfLine ());
			AddCommand (Command.EndOfLine, () => MoveEndOfLine ());
			AddCommand (Command.StartOfPage, () => MoveUp (bytesPerLine * ((int)(position - displayStart) / bytesPerLine)));
			AddCommand (Command.EndOfPage, () => MoveDown (bytesPerLine * (Frame.Height - 1 - ((int)(position - displayStart) / bytesPerLine))));

			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.CursorRight, Command.Right);
			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.Enter, Command.ToggleChecked);

			AddKeyBinding ('v' + Key.AltMask, Command.PageUp);
			AddKeyBinding (Key.PageUp, Command.PageUp);

			AddKeyBinding (Key.V | Key.CtrlMask, Command.PageDown);
			AddKeyBinding (Key.PageDown, Command.PageDown);

			AddKeyBinding (Key.Home, Command.TopHome);
			AddKeyBinding (Key.End, Command.BottomEnd);
			AddKeyBinding (Key.CursorLeft | Key.CtrlMask, Command.StartOfLine);
			AddKeyBinding (Key.CursorRight | Key.CtrlMask, Command.EndOfLine);
			AddKeyBinding (Key.CursorUp | Key.CtrlMask, Command.StartOfPage);
			AddKeyBinding (Key.CursorDown | Key.CtrlMask, Command.EndOfPage);
		}

		public HexView () : this (source: new MemoryStream ()) { }

		public event Action<KeyValuePair<long, byte>> Edited;

		public event Action<HexViewEventArgs> PositionChanged;

		public Stream Source {
			get => source;
			set {
				if (value == null)
					throw new ArgumentNullException ("source");
				if (!value.CanSeek)
					throw new ArgumentException ("The source stream must be seekable (CanSeek property)", "source");
				source = value;

				if (displayStart > source.Length)
					DisplayStart = 0;
				if (position > source.Length)
					position = 0;
				SetNeedsDisplay ();
			}
		}

		internal void SetDisplayStart (long value)
		{
			if (value > 0 && value >= source.Length)
				displayStart = source.Length - 1;
			else if (value < 0)
				displayStart = 0;
			else
				displayStart = value;
			SetNeedsDisplay ();
		}

		public long DisplayStart {
			get => displayStart;
			set {
				position = value;

				SetDisplayStart (value);
			}
		}

		const int displayWidth = 9;
		const int bsize = 4;
		int bpl;
		private int bytesPerLine {
			get => bpl;
			set {
				bpl = value;
				OnPositionChanged ();
			}
		}

		public override Rect Frame {
			get => base.Frame;
			set {
				base.Frame = value;

				bytesPerLine = bsize;
				if (value.Width - displayWidth > 17)
					bytesPerLine = bsize * ((value.Width - displayWidth) / 18);
			}
		}

		byte GetData (byte [] buffer, int offset, out bool edited)
		{
			var pos = DisplayStart + offset;
			if (edits.TryGetValue (pos, out byte v)) {
				edited = true;
				return v;
			}
			edited = false;
			return buffer [offset];
		}

		public override void Redraw (Rect bounds)
		{
			Attribute currentAttribute;
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);

			var frame = Frame;

			var nblocks = bytesPerLine / bsize;
			var data = new byte [nblocks * bsize * frame.Height];
			Source.Position = displayStart;
			var n = source.Read (data, 0, data.Length);

			int activeColor = ColorScheme.HotNormal;
			int trackingColor = ColorScheme.HotFocus;

			for (int line = 0; line < frame.Height; line++) {
				var lineRect = new Rect (0, line, frame.Width, 1);
				if (!bounds.Contains (lineRect))
					continue;

				Move (0, line);
				Driver.SetAttribute (ColorScheme.HotNormal);
				Driver.AddStr (string.Format ("{0:x8} ", displayStart + line * nblocks * bsize));

				currentAttribute = ColorScheme.HotNormal;
				SetAttribute (GetNormalColor ());

				for (int block = 0; block < nblocks; block++) {
					for (int b = 0; b < bsize; b++) {
						var offset = (line * nblocks * bsize) + block * bsize + b;
						var value = GetData (data, offset, out bool edited);
						if (offset + displayStart == position || edited)
							SetAttribute (leftSide ? activeColor : trackingColor);
						else
							SetAttribute (GetNormalColor ());

						Driver.AddStr (offset >= n && !edited ? "  " : string.Format ("{0:x2}", value));
						SetAttribute (GetNormalColor ());
						Driver.AddRune (' ');
					}
					Driver.AddStr (block + 1 == nblocks ? " " : "| ");
				}

				for (int bitem = 0; bitem < nblocks * bsize; bitem++) {
					var offset = line * nblocks * bsize + bitem;
					var b = GetData (data, offset, out bool edited);
					Rune c;
					if (offset >= n && !edited)
						c = ' ';
					else {
						if (b < 32)
							c = '.';
						else if (b > 127)
							c = '.';
						else
							c = b;
					}
					if (offset + displayStart == position || edited)
						SetAttribute (leftSide ? trackingColor : activeColor);
					else
						SetAttribute (GetNormalColor ());

					Driver.AddRune (c);
				}
			}

			void SetAttribute (Attribute attribute)
			{
				if (currentAttribute != attribute) {
					currentAttribute = attribute;
					Driver.SetAttribute (attribute);
				}
			}
		}

		public override void PositionCursor ()
		{
			var delta = (int)(position - displayStart);
			var line = delta / bytesPerLine;
			var item = delta % bytesPerLine;
			var block = item / bsize;
			var column = (item % bsize) * 3;

			if (leftSide)
				Move (displayWidth + block * 14 + column + (firstNibble ? 0 : 1), line);
			else
				Move (displayWidth + (bytesPerLine / bsize) * 14 + item - 1, line);
		}

		void RedisplayLine (long pos)
		{
			var delta = (int)(pos - DisplayStart);
			var line = delta / bytesPerLine;

			SetNeedsDisplay (new Rect (0, line, Frame.Width, 1));
		}

		bool MoveEndOfLine ()
		{
			position = Math.Min ((position / bytesPerLine * bytesPerLine) + bytesPerLine - 1, source.Length);
			SetNeedsDisplay ();

			return true;
		}

		bool MoveStartOfLine ()
		{
			position = position / bytesPerLine * bytesPerLine;
			SetNeedsDisplay ();

			return true;
		}

		bool MoveEnd ()
		{
			position = source.Length;
			if (position >= (DisplayStart + bytesPerLine * Frame.Height)) {
				SetDisplayStart (position);
				SetNeedsDisplay ();
			} else
				RedisplayLine (position);

			return true;
		}

		bool MoveHome ()
		{
			DisplayStart = 0;
			SetNeedsDisplay ();

			return true;
		}

		bool ToggleSide ()
		{
			leftSide = !leftSide;
			RedisplayLine (position);
			firstNibble = true;

			return true;
		}

		bool MoveLeft ()
		{
			RedisplayLine (position);
			if (leftSide) {
				if (!firstNibble) {
					firstNibble = true;
					return true;
				}
				firstNibble = false;
			}
			if (position == 0)
				return true;
			if (position - 1 < DisplayStart) {
				SetDisplayStart (displayStart - bytesPerLine);
				SetNeedsDisplay ();
			} else
				RedisplayLine (position);
			position--;

			return true;
		}

		bool MoveRight ()
		{
			RedisplayLine (position);
			if (leftSide) {
				if (firstNibble) {
					firstNibble = false;
					return true;
				} else
					firstNibble = true;
			}
			if (position < source.Length)
				position++;
			if (position >= (DisplayStart + bytesPerLine * Frame.Height)) {
				SetDisplayStart (DisplayStart + bytesPerLine);
				SetNeedsDisplay ();
			} else
				RedisplayLine (position);

			return true;
		}

		bool MoveUp (int bytes)
		{
			RedisplayLine (position);
			if (position - bytes > -1)
				position -= bytes;
			if (position < DisplayStart) {
				SetDisplayStart (DisplayStart - bytes);
				SetNeedsDisplay ();
			} else
				RedisplayLine (position);

			return true;
		}

		bool MoveDown (int bytes)
		{
			RedisplayLine (position);
			if (position + bytes < source.Length)
				position += bytes;
			else if ((bytes == bytesPerLine * Frame.Height && source.Length >= (DisplayStart + bytesPerLine * Frame.Height))
				|| (bytes <= (bytesPerLine * Frame.Height - bytesPerLine) && source.Length <= (DisplayStart + bytesPerLine * Frame.Height))) {
				var p = position;
				while (p + bytesPerLine < source.Length) {
					p += bytesPerLine;
				}
				position = p;
			}
			if (position >= (DisplayStart + bytesPerLine * Frame.Height)) {
				SetDisplayStart (DisplayStart + bytes);
				SetNeedsDisplay ();
			} else
				RedisplayLine (position);

			return true;
		}

		public override bool ProcessKey (KeyEvent keyEvent)
		{
			var result = InvokeKeybindings (keyEvent);
			if (result != null)
				return (bool)result;

			if (!AllowEdits)
				return false;

			if (keyEvent.Key < Key.Space || keyEvent.Key > Key.CharMask)
				return false;

			if (leftSide) {
				int value;
				var k = (char)keyEvent.Key;
				if (k >= 'A' && k <= 'F')
					value = k - 'A' + 10;
				else if (k >= 'a' && k <= 'f')
					value = k - 'a' + 10;
				else if (k >= '0' && k <= '9')
					value = k - '0';
				else
					return false;

				byte b;
				if (!edits.TryGetValue (position, out b)) {
					source.Position = position;
					b = (byte)source.ReadByte ();
				}
				RedisplayLine (position);
				if (firstNibble) {
					firstNibble = false;
					b = (byte)(b & 0xf | (value << bsize));
					edits [position] = b;
					OnEdited (new KeyValuePair<long, byte> (position, edits [position]));
				} else {
					b = (byte)(b & 0xf0 | value);
					edits [position] = b;
					OnEdited (new KeyValuePair<long, byte> (position, edits [position]));
					MoveRight ();
				}
				return true;
			} else
				return false;
		}

		public virtual void OnEdited (KeyValuePair<long, byte> keyValuePair)
		{
			Edited?.Invoke (keyValuePair);
		}

		public virtual void OnPositionChanged ()
		{
			PositionChanged?.Invoke (new HexViewEventArgs (Position, CursorPosition, BytesPerLine));
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
				&& !me.Flags.HasFlag (MouseFlags.WheeledDown) && !me.Flags.HasFlag (MouseFlags.WheeledUp))
				return false;

			if (!HasFocus)
				SetFocus ();

			if (me.Flags == MouseFlags.WheeledDown) {
				DisplayStart = Math.Min (DisplayStart + bytesPerLine, source.Length);
				return true;
			}

			if (me.Flags == MouseFlags.WheeledUp) {
				DisplayStart = Math.Max (DisplayStart - bytesPerLine, 0);
				return true;
			}

			if (me.X < displayWidth)
				return true;
			var nblocks = bytesPerLine / bsize;
			var blocksSize = nblocks * 14;
			var blocksRightOffset = displayWidth + blocksSize - 1;
			if (me.X > blocksRightOffset + bytesPerLine - 1)
				return true;
			leftSide = me.X >= blocksRightOffset;
			var lineStart = (me.Y * bytesPerLine) + displayStart;
			var x = me.X - displayWidth + 1;
			var block = x / 14;
			x -= block * 2;
			var empty = x % 3;
			var item = x / 3;
			if (!leftSide && item > 0 && (empty == 0 || x == (block * 14) + 14 - 1 - (block * 2)))
				return true;
			firstNibble = true;
			if (leftSide)
				position = Math.Min (lineStart + me.X - blocksRightOffset, source.Length);
			else
				position = Math.Min (lineStart + item, source.Length);

			if (me.Flags == MouseFlags.Button1DoubleClicked) {
				leftSide = !leftSide;
				if (leftSide)
					firstNibble = empty == 1;
				else
					firstNibble = true;
			}
			SetNeedsDisplay ();

			return true;
		}

		public bool AllowEdits { get; set; } = true;

		public IReadOnlyDictionary<long, byte> Edits => edits;

		public long Position => position + 1;

		public Point CursorPosition {
			get {
				var delta = (int)position;
				var line = delta / bytesPerLine + 1;
				var item = delta % bytesPerLine + 1;

				return new Point (item, line);
			}
		}

		public int BytesPerLine => bytesPerLine;

		public void ApplyEdits (Stream stream = null)
		{
			foreach (var kv in edits) {
				source.Position = kv.Key;
				source.WriteByte (kv.Value);
				source.Flush ();
				if (stream != null) {
					stream.Position = kv.Key;
					stream.WriteByte (kv.Value);
					stream.Flush ();
				}
			}
			edits = new SortedDictionary<long, byte> ();
			SetNeedsDisplay ();
		}

		public void DiscardEdits ()
		{
			edits = new SortedDictionary<long, byte> ();
		}

		private CursorVisibility desiredCursorVisibility = CursorVisibility.Default;

		public CursorVisibility DesiredCursorVisibility {
			get => desiredCursorVisibility;
			set {
				if (desiredCursorVisibility != value && HasFocus) {
					Application.Driver.SetCursorVisibility (value);
				}

				desiredCursorVisibility = value;
			}
		}

		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (DesiredCursorVisibility);

			return base.OnEnter (view);
		}

		public class HexViewEventArgs : EventArgs {
			public long Position { get; private set; }
			public Point CursorPosition { get; private set; }

			public int BytesPerLine { get; private set; }

			public HexViewEventArgs (long pos, Point cursor, int lineLength)
			{
				Position = pos;
				CursorPosition = cursor;
				BytesPerLine = lineLength;
			}
		}
	}
}

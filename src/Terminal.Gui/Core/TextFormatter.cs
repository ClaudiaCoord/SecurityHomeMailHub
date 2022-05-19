using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	public enum TextAlignment {
		Left,
		Right,
		Centered,
		Justified
	}

	public enum VerticalTextAlignment {
		Top,
		Bottom,
		Middle,
		Justified
	}

	public enum TextDirection {
		LeftRight_TopBottom,
		TopBottom_LeftRight,
		RightLeft_TopBottom,
		TopBottom_RightLeft,
		LeftRight_BottomTop,
		BottomTop_LeftRight,
		RightLeft_BottomTop,
		BottomTop_RightLeft
	}

	public class TextFormatter {
		List<ustring> lines = new List<ustring> ();
		ustring text;
		TextAlignment textAlignment;
		VerticalTextAlignment textVerticalAlignment;
		TextDirection textDirection;
		Attribute textColor = -1;
		bool needsFormat;
		Key hotKey;
		Size size;

		public event Action<Key> HotKeyChanged;

		public virtual ustring Text {
			get => text;
			set {
				text = value;

				if (text.RuneCount > 0 && (Size.Width == 0 || Size.Height == 0 || Size.Width != text.RuneCount)) {
					Size = new Size (TextFormatter.MaxWidth (Text, int.MaxValue), 1);
				}

				NeedsFormat = true;
			}
		}

		public bool AutoSize { get; set; }

		public TextAlignment Alignment {
			get => textAlignment;
			set {
				textAlignment = value;
				NeedsFormat = true;
			}
		}

		public VerticalTextAlignment VerticalAlignment {
			get => textVerticalAlignment;
			set {
				textVerticalAlignment = value;
				NeedsFormat = true;
			}
		}

		public TextDirection Direction {
			get => textDirection;
			set {
				textDirection = value;
				NeedsFormat = true;
			}
		}

		public static bool IsHorizontalDirection (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.LeftRight_TopBottom:
			case TextDirection.LeftRight_BottomTop:
			case TextDirection.RightLeft_TopBottom:
			case TextDirection.RightLeft_BottomTop:
				return true;
			default:
				return false;
			}
		}

		public static bool IsVerticalDirection (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.TopBottom_LeftRight:
			case TextDirection.TopBottom_RightLeft:
			case TextDirection.BottomTop_LeftRight:
			case TextDirection.BottomTop_RightLeft:
				return true;
			default:
				return false;
			}
		}

		public static bool IsLeftToRight (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.LeftRight_TopBottom:
			case TextDirection.LeftRight_BottomTop:
				return true;
			default:
				return false;
			}
		}

		public static bool IsTopToBottom (TextDirection textDirection)
		{
			switch (textDirection) {
			case TextDirection.TopBottom_LeftRight:
			case TextDirection.TopBottom_RightLeft:
				return true;
			default:
				return false;
			}
		}

		public Size Size {
			get => size;
			set {
				size = value;
				NeedsFormat = true;
			}
		}

		public Rune HotKeySpecifier { get; set; } = (Rune)0xFFFF;

		public int HotKeyPos { get => hotKeyPos; set => hotKeyPos = value; }

		public Key HotKey {
			get => hotKey;
			internal set {
				if (hotKey != value) {
					var oldKey = hotKey;
					hotKey = value;
					HotKeyChanged?.Invoke (oldKey);
				}
			}
		}

		public uint HotKeyTagMask { get; set; } = 0x100000;

		public int CursorPosition { get; set; }

		public List<ustring> Lines {
			get {
				if (ustring.IsNullOrEmpty (Text)) {
					lines = new List<ustring> ();
					lines.Add (ustring.Empty);
					NeedsFormat = false;
					return lines;
				}

				if (NeedsFormat) {
					var shown_text = text;
					if (FindHotKey (text, HotKeySpecifier, true, out hotKeyPos, out Key newHotKey)) {
						HotKey = newHotKey;
						shown_text = RemoveHotKeySpecifier (Text, hotKeyPos, HotKeySpecifier);
						shown_text = ReplaceHotKeyWithTag (shown_text, hotKeyPos);
					}
					if (Size.IsEmpty) {
						throw new InvalidOperationException ("Size must be set before accessing Lines");
					}

					if (IsVerticalDirection (textDirection)) {
						lines = Format (shown_text, Size.Height, textVerticalAlignment == VerticalTextAlignment.Justified, Size.Width > 1);
						if (!AutoSize && lines.Count > Size.Width) {
							lines.RemoveRange (Size.Width, lines.Count - Size.Width);
						}
					} else {
						lines = Format (shown_text, Size.Width, textAlignment == TextAlignment.Justified, Size.Height > 1);
						if (!AutoSize && lines.Count > Size.Height) {
							lines.RemoveRange (Size.Height, lines.Count - Size.Height);
						}
					}

					NeedsFormat = false;
				}
				return lines;
			}
		}

		public bool NeedsFormat { get => needsFormat; set => needsFormat = value; }

		static ustring StripCRLF (ustring str)
		{
			var runes = str.ToRuneList ();
			for (int i = 0; i < runes.Count; i++) {
				switch (runes [i]) {
				case '\n':
					runes.RemoveAt (i);
					break;

				case '\r':
					if ((i + 1) < runes.Count && runes [i + 1] == '\n') {
						runes.RemoveAt (i);
						runes.RemoveAt (i + 1);
						i++;
					} else {
						runes.RemoveAt (i);
					}
					break;
				}
			}
			return ustring.Make (runes);
		}
		static ustring ReplaceCRLFWithSpace (ustring str)
		{
			var runes = str.ToRuneList ();
			for (int i = 0; i < runes.Count; i++) {
				switch (runes [i]) {
				case '\n':
					runes [i] = (Rune)' ';
					break;

				case '\r':
					if ((i + 1) < runes.Count && runes [i + 1] == '\n') {
						runes [i] = (Rune)' ';
						runes.RemoveAt (i + 1);
						i++;
					} else {
						runes [i] = (Rune)' ';
					}
					break;
				}
			}
			return ustring.Make (runes);
		}


		public static string ClipOrPad (string text, int width)
		{
			if (string.IsNullOrEmpty (text))
				return text;

			if (text.Sum (c => Rune.ColumnWidth (c)) < width) {

				int toPad = width - (text.Sum (c => Rune.ColumnWidth (c)));

				return text + new string (' ', toPad);
			}

			return new string (text.TakeWhile (c => (width -= Rune.ColumnWidth (c)) >= 0).ToArray ());
		}

		public static List<ustring> WordWrap (ustring text, int width, bool preserveTrailingSpaces = false, int tabWidth = 0)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}

			int start = 0, end;
			var lines = new List<ustring> ();

			if (ustring.IsNullOrEmpty (text)) {
				return lines;
			}

			var runes = StripCRLF (text).ToRuneList ();
			if (!preserveTrailingSpaces) {
				while ((end = start + width) < runes.Count) {
					while (runes [end] != ' ' && end > start)
						end--;
					if (end == start)
						end = start + width;
					lines.Add (ustring.Make (runes.GetRange (start, end - start)));
					start = end;
					if (runes [end] == ' ') {
						start++;
					}
				}
			} else {
				while ((end = start) < runes.Count) {
					end = GetNextWhiteSpace (start, width);
					lines.Add (ustring.Make (runes.GetRange (start, end - start)));
					start = end;
				}
			}

			int GetNextWhiteSpace (int from, int cWidth, int cLength = 0)
			{
				var to = from;
				var length = cLength;

				while (length < cWidth && to < runes.Count) {
					var rune = runes [to];
					length += Rune.ColumnWidth (rune);
					if (rune == ' ') {
						if (length == cWidth) {
							return to + 1;
						} else if (length > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (to + 1, cWidth, length);
						}
					} else if (rune == '\t') {
						length += tabWidth + 1;
						if (length == tabWidth && tabWidth > cWidth) {
							return to + 1;
						} else if (length > cWidth && tabWidth > cWidth) {
							return to;
						} else {
							return GetNextWhiteSpace (to + 1, cWidth, length);
						}
					}
					to++;
				}
				if (cLength > 0 && to < runes.Count && runes [to] != ' ') {
					return from;
				} else {
					return to;
				}
			}

			if (start < text.RuneCount) {
				lines.Add (ustring.Make (runes.GetRange (start, runes.Count - start)));
			}

			return lines;
		}

		public static ustring ClipAndJustify (ustring text, int width, TextAlignment talign)
		{
			return ClipAndJustify (text, width, talign == TextAlignment.Justified);
		}

		public static ustring ClipAndJustify (ustring text, int width, bool justify)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}
			if (ustring.IsNullOrEmpty (text)) {
				return text;
			}

			var runes = text.ToRuneList ();
			int slen = runes.Count;
			if (slen > width) {
				return ustring.Make (runes.GetRange (0, width));
			} else {
				if (justify) {
					return Justify (text, width);
				}
				return text;
			}
		}

		public static ustring Justify (ustring text, int width, char spaceChar = ' ')
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("Width cannot be negative.");
			}
			if (ustring.IsNullOrEmpty (text)) {
				return text;
			}

			var words = text.Split (ustring.Make (' '));
			int textCount = words.Sum (arg => arg.RuneCount);

			var spaces = words.Length > 1 ? (width - textCount) / (words.Length - 1) : 0;
			var extras = words.Length > 1 ? (width - textCount) % words.Length : 0;

			var s = new System.Text.StringBuilder ();
			for (int w = 0; w < words.Length; w++) {
				var x = words [w];
				s.Append (x);
				if (w + 1 < words.Length)
					for (int i = 0; i < spaces; i++)
						s.Append (spaceChar);
				if (extras > 0) {
					extras--;
				}
			}
			return ustring.Make (s.ToString ());
		}

		static char [] whitespace = new char [] { ' ', '\t' };
		private int hotKeyPos;

		public static List<ustring> Format (ustring text, int width, TextAlignment talign, bool wordWrap, bool preserveTrailingSpaces = false, int tabWidth = 0)
		{
			return Format (text, width, talign == TextAlignment.Justified, wordWrap, preserveTrailingSpaces, tabWidth);
		}

		public static List<ustring> Format (ustring text, int width, bool justify, bool wordWrap,
			bool preserveTrailingSpaces = false, int tabWidth = 0)
		{
			if (width < 0) {
				throw new ArgumentOutOfRangeException ("width cannot be negative");
			}
			if (preserveTrailingSpaces && !wordWrap) {
				throw new ArgumentException ("if 'preserveTrailingSpaces' is true, then 'wordWrap' must be true either.");
			}
			List<ustring> lineResult = new List<ustring> ();

			if (ustring.IsNullOrEmpty (text) || width == 0) {
				lineResult.Add (ustring.Empty);
				return lineResult;
			}

			if (wordWrap == false) {
				text = ReplaceCRLFWithSpace (text);
				lineResult.Add (ClipAndJustify (text, width, justify));
				return lineResult;
			}

			var runes = text.ToRuneList ();
			int runeCount = runes.Count;
			int lp = 0;
			for (int i = 0; i < runeCount; i++) {
				Rune c = runes [i];
				if (c == '\n') {
					var wrappedLines = WordWrap (ustring.Make (runes.GetRange (lp, i - lp)), width, preserveTrailingSpaces, tabWidth);
					foreach (var line in wrappedLines) {
						lineResult.Add (ClipAndJustify (line, width, justify));
					}
					if (wrappedLines.Count == 0) {
						lineResult.Add (ustring.Empty);
					}
					lp = i + 1;
				}
			}
			foreach (var line in WordWrap (ustring.Make (runes.GetRange (lp, runeCount - lp)), width, preserveTrailingSpaces, tabWidth)) {
				lineResult.Add (ClipAndJustify (line, width, justify));
			}

			return lineResult;
		}

		public static int MaxLines (ustring text, int width)
		{
			var result = TextFormatter.Format (text, width, false, true);
			return result.Count;
		}

		public static int MaxWidth (ustring text, int width)
		{
			var result = TextFormatter.Format (text, width, false, true);
			var max = 0;
			result.ForEach (s => {
				var m = 0;
				s.ToRuneList ().ForEach (r => m += Rune.ColumnWidth (r));
				if (m > max) {
					max = m;
				}
			});
			return max;
		}

		public static Rect CalcRect (int x, int y, ustring text, TextDirection direction = TextDirection.LeftRight_TopBottom)
		{
			if (ustring.IsNullOrEmpty (text)) {
				return new Rect (new Point (x, y), Size.Empty);
			}

			int w, h;

			if (IsHorizontalDirection (direction)) {
				int mw = 0;
				int ml = 1;

				int cols = 0;
				foreach (var rune in text) {
					if (rune == '\n') {
						ml++;
						if (cols > mw) {
							mw = cols;
						}
						cols = 0;
					} else {
						if (rune != '\r') {
							cols++;
							var rw = Rune.ColumnWidth (rune);
							if (rw > 0) {
								rw--;
							}
							cols += rw;
						}
					}
				}
				if (cols > mw) {
					mw = cols;
				}
				w = mw;
				h = ml;
			} else {
				int vw = 0;
				int vh = 0;

				int rows = 0;
				foreach (var rune in text) {
					if (rune == '\n') {
						vw++;
						if (rows > vh) {
							vh = rows;
						}
						rows = 0;
					} else {
						if (rune != '\r') {
							rows++;
							var rw = Rune.ColumnWidth (rune);
							if (rw < 0) {
								rw++;
							}
							if (rw > vw) {
								vw = rw;
							}
						}
					}
				}
				if (rows > vh) {
					vh = rows;
				}
				w = vw;
				h = vh;
			}

			return new Rect (x, y, w, h);
		}

		public static bool FindHotKey (ustring text, Rune hotKeySpecifier, bool firstUpperCase, out int hotPos, out Key hotKey)
		{
			if (ustring.IsNullOrEmpty (text) || hotKeySpecifier == (Rune)0xFFFF) {
				hotPos = -1;
				hotKey = Key.Unknown;
				return false;
			}

			Rune hot_key = (Rune)0;
			int hot_pos = -1;

			int i = 0;
			foreach (Rune c in text) {
				if ((char)c != 0xFFFD) {
					if (c == hotKeySpecifier) {
						hot_pos = i;
					} else if (hot_pos > -1) {
						hot_key = c;
						break;
					}
				}
				i++;
			}


			if (hot_pos == -1 && firstUpperCase) {
				i = 0;
				foreach (Rune c in text) {
					if ((char)c != 0xFFFD) {
						if (Rune.IsUpper (c)) {
							hot_key = c;
							hot_pos = i;
							break;
						}
					}
					i++;
				}
			}

			if (hot_key != (Rune)0 && hot_pos != -1) {
				hotPos = hot_pos;

				if (hot_key.IsValid && char.IsLetterOrDigit ((char)hot_key)) {
					hotKey = (Key)char.ToUpperInvariant ((char)hot_key);
					return true;
				}
			}

			hotPos = -1;
			hotKey = Key.Unknown;
			return false;
		}

		public ustring ReplaceHotKeyWithTag (ustring text, int hotPos)
		{
			var runes = text.ToRuneList ();
			if (Rune.IsLetterOrNumber (runes [hotPos])) {
				runes [hotPos] = new Rune ((uint)runes [hotPos] | HotKeyTagMask);
			}
			return ustring.Make (runes);
		}

		public static ustring RemoveHotKeySpecifier (ustring text, int hotPos, Rune hotKeySpecifier)
		{
			if (ustring.IsNullOrEmpty (text)) {
				return text;
			}

			ustring start = ustring.Empty;
			int i = 0;
			foreach (Rune c in text) {
				if (c == hotKeySpecifier && i == hotPos) {
					i++;
					continue;
				}
				start += ustring.Make (c);
				i++;
			}
			return start;
		}

		public void Draw (Rect bounds, Attribute normalColor, Attribute hotColor)
		{
			if (ustring.IsNullOrEmpty (text)) {
				return;
			}

			Application.Driver?.SetAttribute (normalColor);

			var linesFormated = Lines;
			switch (textDirection) {
			case TextDirection.TopBottom_RightLeft:
			case TextDirection.LeftRight_BottomTop:
			case TextDirection.RightLeft_BottomTop:
			case TextDirection.BottomTop_RightLeft:
				linesFormated.Reverse ();
				break;
			}

			for (int line = 0; line < linesFormated.Count; line++) {
				var isVertical = IsVerticalDirection (textDirection);

				if ((isVertical && (line > bounds.Width)) || (!isVertical && (line > bounds.Height)))
					continue;

				var runes = lines [line].ToRunes ();

				switch (textDirection) {
				case TextDirection.RightLeft_BottomTop:
				case TextDirection.RightLeft_TopBottom:
				case TextDirection.BottomTop_LeftRight:
				case TextDirection.BottomTop_RightLeft:
					runes = runes.Reverse ().ToArray ();
					break;
				}

				int x, y;
				if (textAlignment == TextAlignment.Right || (textAlignment == TextAlignment.Justified && !IsLeftToRight (textDirection))) {
					if (isVertical) {
						x = bounds.Right - Lines.Count + line;
						CursorPosition = bounds.Width - Lines.Count + hotKeyPos;
					} else {
						x = bounds.Right - runes.Length;
						CursorPosition = bounds.Width - runes.Length + hotKeyPos;
					}
				} else if (textAlignment == TextAlignment.Left || textAlignment == TextAlignment.Justified) {
					if (isVertical) {
						x = bounds.Left + line;
					} else {
						x = bounds.Left;
					}
					CursorPosition = hotKeyPos;
				} else if (textAlignment == TextAlignment.Centered) {
					if (isVertical) {
						x = bounds.Left + line + ((bounds.Width - Lines.Count) / 2);
						CursorPosition = (bounds.Width - Lines.Count) / 2 + hotKeyPos;
					} else {
						x = bounds.Left + (bounds.Width - runes.Length) / 2;
						CursorPosition = (bounds.Width - runes.Length) / 2 + hotKeyPos;
					}
				} else {
					throw new ArgumentOutOfRangeException ();
				}

				if (textVerticalAlignment == VerticalTextAlignment.Bottom || (textVerticalAlignment == VerticalTextAlignment.Justified && !IsTopToBottom (textDirection))) {
					if (isVertical) {
						y = bounds.Bottom - runes.Length;
					} else {
						y = bounds.Bottom - Lines.Count + line;
					}
				} else if (textVerticalAlignment == VerticalTextAlignment.Top || textVerticalAlignment == VerticalTextAlignment.Justified) {
					if (isVertical) {
						y = bounds.Top;
					} else {
						y = bounds.Top + line;
					}
				} else if (textVerticalAlignment == VerticalTextAlignment.Middle) {
					if (isVertical) {
						var s = (bounds.Height - runes.Length) / 2;
						y = bounds.Top + s;
					} else {
						var s = (bounds.Height - Lines.Count) / 2;
						y = bounds.Top + line + s;
					}
				} else {
					throw new ArgumentOutOfRangeException ();
				}

				var start = isVertical ? bounds.Top : bounds.Left;
				var size = isVertical ? bounds.Height : bounds.Width;

				var current = start;
				for (var idx = start; idx < start + size; idx++) {
					if (idx < 0) {
						current++;
						continue;
					}
					var rune = (Rune)' ';
					if (isVertical) {
						Application.Driver?.Move (x, current);
						if (idx >= y && idx < (y + runes.Length)) {
							rune = runes [idx - y];
						}
					} else {
						Application.Driver?.Move (current, y);
						if (idx >= x && idx < (x + runes.Length)) {
							rune = runes [idx - x];
						}
					}
					if ((rune & HotKeyTagMask) == HotKeyTagMask) {
						if ((isVertical && textVerticalAlignment == VerticalTextAlignment.Justified) ||
						    (!isVertical && textAlignment == TextAlignment.Justified)) {
							CursorPosition = idx - start;
						}
						Application.Driver?.SetAttribute (hotColor);
						Application.Driver?.AddRune ((Rune)((uint)rune & ~HotKeyTagMask));
						Application.Driver?.SetAttribute (normalColor);
					} else {
						Application.Driver?.AddRune (rune);
					}
					current += Rune.ColumnWidth (rune);
					if (idx + 1 < runes.Length && current + Rune.ColumnWidth (runes [idx + 1]) > size) {
						break;
					}
				}
			}
		}
	}
}

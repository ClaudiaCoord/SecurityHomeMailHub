using NStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {
	public class RadioGroup : View {
		int selected = -1;
		int cursor;
		DisplayModeLayout displayMode;
		int horizontalSpace = 2;
		List<(int pos, int length)> horizontal;
		
		public bool NoSymbol { get; set; } = false;

		public RadioGroup () : this (radioLabels: new ustring [] { }) { }

		public RadioGroup (ustring [] radioLabels, int selected = 0) : base ()
		{
			Initialize (radioLabels, selected);
		}

		public RadioGroup (Rect rect, ustring [] radioLabels, int selected = 0) : base (rect)
		{
			Initialize (radioLabels, selected);
		}

		public RadioGroup (int x, int y, ustring [] radioLabels, int selected = 0) :
			this (MakeRect (x, y, radioLabels != null ? radioLabels.ToList () : null), radioLabels, selected)
		{ }

		void Initialize (ustring [] radioLabels, int selected)
		{
			if (radioLabels == null) {
				this.radioLabels = new List<ustring> ();
			} else {
				this.radioLabels = radioLabels.ToList ();
			}

			this.selected = selected;
			SetWidthHeight (this.radioLabels);
			CanFocus = true;

			AddCommand (Command.LineUp, () => { MoveUp (); return true; });
			AddCommand (Command.LineDown, () => { MoveDown (); return true; });
			AddCommand (Command.TopHome, () => { MoveHome (); return true; });
			AddCommand (Command.BottomEnd, () => { MoveEnd (); return true; });
			AddCommand (Command.Accept, () => { SelectItem (); return true; });

			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.Home, Command.TopHome);
			AddKeyBinding (Key.End, Command.BottomEnd);
			AddKeyBinding (Key.Space, Command.Accept);
		}

		public DisplayModeLayout DisplayMode {
			get { return displayMode; }
			set {
				if (displayMode != value) {
					displayMode = value;
					SetWidthHeight (radioLabels);
					SetNeedsDisplay ();
				}
			}
		}

		public int HorizontalSpace {
			get { return horizontalSpace; }
			set {
				if (horizontalSpace != value && displayMode == DisplayModeLayout.Horizontal) {
					horizontalSpace = value;
					SetWidthHeight (radioLabels);
					SetNeedsDisplay ();
				}
			}
		}

		void SetWidthHeight (List<ustring> radioLabels)
		{
			switch (displayMode) {
			case DisplayModeLayout.Vertical:
				var r = MakeRect (0, 0, radioLabels);
				if (LayoutStyle == LayoutStyle.Computed) {
					Width = r.Width;
					Height = radioLabels.Count;
				} else {
					Frame = new Rect (Frame.Location, new Size (r.Width, radioLabels.Count));
				}
				break;
			case DisplayModeLayout.Horizontal:
				CalculateHorizontalPositions ();
				var length = 0;
				foreach (var item in horizontal) {
					length += item.length;
				}
				var hr = new Rect (0, 0, length, 1);
				if (LayoutStyle == LayoutStyle.Computed) {
					Width = hr.Width;
					Height = 1;
				}
				break;
			}
		}

		static Rect MakeRect (int x, int y, List<ustring> radioLabels)
		{
			int width = 0;

			if (radioLabels == null) {
				return new Rect (x, y, width, 0);
			}

			foreach (var s in radioLabels)
				width = Math.Max (s.RuneCount + 3, width);
			return new Rect (x, y, width, radioLabels.Count);
		}


		List<ustring> radioLabels = new List<ustring> ();

		public ustring [] RadioLabels {
			get => radioLabels.ToArray ();
			set {
				var prevCount = radioLabels.Count;
				radioLabels = value.ToList ();
				if (prevCount != radioLabels.Count) {
					SetWidthHeight (radioLabels);
				}
				SelectedItem = 0;
				cursor = 0;
				SetNeedsDisplay ();
			}
		}

		private void CalculateHorizontalPositions ()
		{
			if (displayMode == DisplayModeLayout.Horizontal) {
				horizontal = new List<(int pos, int length)> ();
				int start = 0;
				int length = 0;
				for (int i = 0; i < radioLabels.Count; i++) {
					start += length;
					length = radioLabels [i].RuneCount + horizontalSpace;
					horizontal.Add ((start, length));
				}
			}
		}

		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (GetNormalColor ());
			Clear ();
			for (int i = 0; i < radioLabels.Count; i++) {
				switch (DisplayMode) {
				case DisplayModeLayout.Vertical:
					Move (0, i);
					break;
				case DisplayModeLayout.Horizontal:
					Move (horizontal [i].pos, 0);
					break;
				}
				Driver.SetAttribute (GetNormalColor ());
				if (!NoSymbol)
					Driver.AddStr (ustring.Make (new Rune [] { (i == selected ? Driver.Selected : Driver.UnSelected), ' ' }));
				DrawHotString (radioLabels [i], HasFocus && i == cursor, ColorScheme);
			}
		}

		public override void PositionCursor ()
		{
			switch (DisplayMode) {
			case DisplayModeLayout.Vertical:
				Move (0, cursor);
				break;
			case DisplayModeLayout.Horizontal:
				Move (horizontal [cursor].pos, 0);
				break;
			}
		}

		public event Action<SelectedItemChangedArgs> SelectedItemChanged;

		public int SelectedItem {
			get => selected;
			set {
				OnSelectedItemChanged (value, SelectedItem);
				cursor = selected;
				SetNeedsDisplay ();
			}
		}

		public void Refresh ()
		{
			OnSelectedItemChanged (selected, -1);
		}

		public virtual void OnSelectedItemChanged (int selectedItem, int previousSelectedItem)
		{
			selected = selectedItem;
			SelectedItemChanged?.Invoke (new SelectedItemChangedArgs (selectedItem, previousSelectedItem));
		}

		public override bool ProcessColdKey (KeyEvent kb)
		{
			var key = kb.KeyValue;
			if (key < Char.MaxValue && Char.IsLetterOrDigit ((char)key)) {
				int i = 0;
				key = Char.ToUpper ((char)key);
				foreach (var l in radioLabels) {
					bool nextIsHot = false;
					foreach (var c in l) {
						if (c == '_')
							nextIsHot = true;
						else {
							if (nextIsHot && c == key) {
								SelectedItem = i;
								cursor = i;
								if (!HasFocus)
									SetFocus ();
								return true;
							}
							nextIsHot = false;
						}
					}
					i++;
				}
			}
			return false;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return base.ProcessKey (kb);
		}

		void SelectItem ()
		{
			SelectedItem = cursor;
		}

		void MoveEnd ()
		{
			cursor = Math.Max (radioLabels.Count - 1, 0);
		}

		void MoveHome ()
		{
			cursor = 0;
		}

		void MoveDown ()
		{
			if (cursor + 1 < radioLabels.Count) {
				cursor++;
				SetNeedsDisplay ();
			} else if (cursor > 0) {
				cursor = 0;
				SetNeedsDisplay ();
			}
		}

		void MoveUp ()
		{
			if (cursor > 0) {
				cursor--;
				SetNeedsDisplay ();
			} else if (radioLabels.Count - 1 > 0) {
				cursor = radioLabels.Count - 1;
				SetNeedsDisplay ();
			}
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked)) {
				return false;
			}
			if (!CanFocus) {
				return false;
			}
			SetFocus ();

			var pos = displayMode == DisplayModeLayout.Horizontal ? me.X : me.Y;
			var rCount = displayMode == DisplayModeLayout.Horizontal ? horizontal.Last ().pos + horizontal.Last ().length : radioLabels.Count;

			if (pos < rCount) {
				var c = displayMode == DisplayModeLayout.Horizontal ? horizontal.FindIndex ((x) => x.pos <= me.X && x.pos + x.length - 2 >= me.X) : me.Y;
				if (c > -1) {
					cursor = SelectedItem = c;
					SetNeedsDisplay ();
				}
			}
			return true;
		}

		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}

	public enum DisplayModeLayout {
		Vertical,
		Horizontal
	}

	public class SelectedItemChangedArgs : EventArgs {
		public int PreviousSelectedItem { get; }

		public int SelectedItem { get; }

		public SelectedItemChangedArgs (int selectedItem, int previousSelectedItem)
		{
			PreviousSelectedItem = previousSelectedItem;
			SelectedItem = selectedItem;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NStack;

namespace Terminal.Gui {
	public interface IListDataSource {
		int Count { get; }

		int Length { get; }

		void Render (ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0);

		bool IsMarked (int item);

		void SetMark (int item, bool value);

		IList ToList ();
	}

	public class ListView : View {
		int top, left;
		int selected;

		IListDataSource source;
		public IListDataSource Source {
			get => source;
			set {
				source = value;
				top = 0;
				selected = 0;
				lastSelectedItem = -1;
				SetNeedsDisplay ();
			}
		}

		public void SetSource (IList source)
		{
			if (source == null)
				Source = null;
			else {
				Source = MakeWrapper (source);
			}
		}

		public Task SetSourceAsync (IList source)
		{
			return Task.Factory.StartNew (() => {
				if (source == null)
					Source = null;
				else
					Source = MakeWrapper (source);
				return source;
			}, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		}

		bool allowsMarking;
		public bool AllowsMarking {
			get => allowsMarking;
			set {
				allowsMarking = value;
				SetNeedsDisplay ();
			}
		}

		public bool AllowsMultipleSelection {
			get => allowsMultipleSelection;
			set {
				allowsMultipleSelection = value;
				if (Source != null && !allowsMultipleSelection) {
					for (int i = 0; i < Source.Count; i++) {
						if (Source.IsMarked (i) && i != selected) {
							Source.SetMark (i, false);
						}
					}
				}
			}
		}

		public int TopItem {
			get => top;
			set {
				if (source == null)
					return;

				if (value < 0 || (source.Count > 0 && value >= source.Count))
					throw new ArgumentException (nameof(TopItem));
				top = value;
				SetNeedsDisplay ();
			}
		}

		public int LeftItem {
			get => left;
			set {
				if (source == null)
					return;

				if (value < 0 || (Maxlength > 0 && value >= Maxlength))
					throw new ArgumentException (nameof(LeftItem));
				left = value;
				SetNeedsDisplay ();
			}
		}

		public int Maxlength => (source?.Length) ?? 0;

		public int SelectedItem {
			get => selected;
			set {
				if (source == null || source.Count == 0) {
					return;
				}
				if (value < 0 || value >= source.Count) {
					throw new ArgumentException (nameof(SelectedItem));
				}
				selected = value;
				OnSelectedChanged ();
			}
		}

		static IListDataSource MakeWrapper (IList source)
		{
			return new ListWrapper (source);
		}

		public ListView (IList source) : this (MakeWrapper (source))
		{
		}

		public ListView (IListDataSource source) : base ()
		{
			this.source = source;
			Initialize ();
		}

		public ListView () : base ()
		{
			Initialize ();
		}

		public ListView (Rect rect, IList source) : this (rect, MakeWrapper (source))
		{
			Initialize ();
		}

		public ListView (Rect rect, IListDataSource source) : base (rect)
		{
			this.source = source;
			Initialize ();
		}

		void Initialize ()
		{
			Source = source;
			CanFocus = true;

			AddCommand (Command.LineUp, () => MoveUp ());
			AddCommand (Command.LineDown, () => MoveDown ());
			AddCommand (Command.ScrollUp, () => ScrollUp (1));
			AddCommand (Command.ScrollDown, () => ScrollDown (1));
			AddCommand (Command.PageUp, () => MovePageUp ());
			AddCommand (Command.PageDown, () => MovePageDown ());
			AddCommand (Command.TopHome, () => MoveHome ());
			AddCommand (Command.BottomEnd, () => MoveEnd ());
			AddCommand (Command.OpenSelectedItem, () => OnOpenSelectedItem ());
			AddCommand (Command.ToggleChecked, () => MarkUnmarkRow ());

			AddKeyBinding (Key.CursorUp,Command.LineUp);
			AddKeyBinding (Key.P | Key.CtrlMask, Command.LineUp);

			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.N | Key.CtrlMask, Command.LineDown);

			AddKeyBinding(Key.PageUp,Command.PageUp);

			AddKeyBinding (Key.PageDown, Command.PageDown);
			AddKeyBinding (Key.V | Key.CtrlMask, Command.PageDown);

			AddKeyBinding (Key.Home, Command.TopHome);

			AddKeyBinding (Key.End, Command.BottomEnd);

			AddKeyBinding (Key.Enter, Command.OpenSelectedItem);

			AddKeyBinding (Key.Space, Command.ToggleChecked);
		}

		public override void Redraw (Rect bounds)
		{
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);
			var f = Frame;
			var item = top;
			bool focused = HasFocus;
			int col = allowsMarking ? 2 : 0;
			int start = left;

			for (int row = 0; row < f.Height; row++, item++) {
				bool isSelected = item == selected;

				var newcolor = focused ? (isSelected ? ColorScheme.Focus : GetNormalColor ())
						       : (isSelected ? ColorScheme.HotNormal : GetNormalColor ());

				if (newcolor != current) {
					Driver.SetAttribute (newcolor);
					current = newcolor;
				}

				Move (0, row);
				if (source == null || item >= source.Count) {
					for (int c = 0; c < f.Width; c++)
						Driver.AddRune (' ');
				} else {
					var rowEventArgs = new ListViewRowEventArgs (item);
					OnRowRender (rowEventArgs);
					if (rowEventArgs.RowAttribute != null && current != rowEventArgs.RowAttribute) {
						current = (Attribute)rowEventArgs.RowAttribute;
						Driver.SetAttribute (current);
					}
					if (allowsMarking) {
						Driver.AddRune (source.IsMarked (item) ? (AllowsMultipleSelection ? Driver.Checked : Driver.Selected) : (AllowsMultipleSelection ? Driver.UnChecked : Driver.UnSelected));
						Driver.AddRune (' ');
					}
					Source.Render (this, Driver, isSelected, item, col, row, f.Width - col, start);
				}
			}
		}

		public event Action<ListViewItemEventArgs> SelectedItemChanged;

		public event Action<ListViewItemEventArgs> OpenSelectedItem;

		public event Action<ListViewRowEventArgs> RowRender;

		public override bool ProcessKey (KeyEvent kb)
		{
			if (source == null)
				return base.ProcessKey (kb);

			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return false;
		}

		public virtual bool AllowsAll ()
		{
			if (!allowsMarking)
				return false;
			if (!AllowsMultipleSelection) {
				for (int i = 0; i < Source.Count; i++) {
					if (Source.IsMarked (i) && i != selected) {
						Source.SetMark (i, false);
						return true;
					}
				}
			}
			return true;
		}

		public virtual bool MarkUnmarkRow ()
		{
			if (AllowsAll ()) {
				Source.SetMark (SelectedItem, !Source.IsMarked (SelectedItem));
				SetNeedsDisplay ();
				return true;
			}

			return false;
		}

		public virtual bool MovePageUp ()
		{
			int n = (selected - Frame.Height);
			if (n < 0)
				n = 0;
			if (n != selected) {
				top = selected = n;
                OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		public virtual bool MovePageDown ()
		{
			var n = (selected + Frame.Height);
			if (n >= source.Count)
				n = source.Count - 1;
			if (n != selected) {
				selected = n;
				if (source.Count >= Frame.Height)
					top = selected;
				else
					top = 0;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		public virtual bool MoveDown ()
		{
			if (source.Count == 0) {
				return false;      
			}
			if (selected >= source.Count) {
				selected = source.Count - 1;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected + 1 < source.Count) {      
				selected++;

				if (selected >= top + Frame.Height) {
					top++;
				} else if (selected < top) {
					top = selected;
				}
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected == 0) {
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		public virtual bool MoveUp ()
		{
			if (source.Count == 0) {
				return false;      
			}
			if (selected >= source.Count) {
				selected = source.Count - 1;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			} else if (selected > 0) {
				selected--;
				if (selected > Source.Count) {
					selected = Source.Count - 1;
				}
				if (selected < top) {
					top = selected;
				} else if (selected > top + Frame.Height) {
					top = Math.Max (selected - Frame.Height + 1, 0);
				}
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}
			return true;
		}

		public virtual bool MoveEnd ()
		{
			if (source.Count > 0 && selected != source.Count - 1) {
				selected = source.Count - 1;
				if (top + selected > Frame.Height - 1) {
					top = selected;
				}
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		public virtual bool MoveHome ()
		{
			if (selected != 0) {
				selected = 0;
				top = selected;
				OnSelectedChanged ();
				SetNeedsDisplay ();
			}

			return true;
		}

		public virtual bool ScrollDown (int lines)
		{
			top = Math.Max (Math.Min (top + lines, source.Count - 1), 0);
			SetNeedsDisplay ();
			return true;
		}

		public virtual bool ScrollUp (int lines)
		{
			top = Math.Max (top - lines, 0);
			SetNeedsDisplay ();
			return true;
		}

		public virtual bool ScrollRight (int cols)
		{
			left = Math.Max (Math.Min (left + cols, Maxlength - 1), 0);
			SetNeedsDisplay ();
			return true;
		}

		public virtual bool ScrollLeft (int cols)
		{
			left = Math.Max (left - cols, 0);
			SetNeedsDisplay ();
			return true;
		}

		int lastSelectedItem = -1;
		private bool allowsMultipleSelection = true;

		public virtual bool OnSelectedChanged ()
		{
			if (selected != lastSelectedItem) {
				var value = source?.Count > 0 ? source.ToList () [selected] : null;
				SelectedItemChanged?.Invoke (new ListViewItemEventArgs (selected, value));
				if (HasFocus) {
					lastSelectedItem = selected;
				}
				return true;
			}

			return false;
		}

		public virtual bool OnOpenSelectedItem ()
		{
			if (source.Count <= selected || selected < 0 || OpenSelectedItem == null) {
				return false;
			}

			var value = source.ToList () [selected];

			OpenSelectedItem?.Invoke (new ListViewItemEventArgs (selected, value));

			return true;
		}

		public virtual void OnRowRender (ListViewRowEventArgs rowEventArgs)
		{
			RowRender?.Invoke (rowEventArgs);
		}

		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			if (lastSelectedItem == -1) {
				EnsuresVisibilitySelectedItem ();
				OnSelectedChanged ();
			}

			return base.OnEnter (view);
		}

		public override bool OnLeave (View view)
		{
			if (lastSelectedItem > -1) {
				lastSelectedItem = -1;
			}

			return base.OnLeave (view);
		}

		void EnsuresVisibilitySelectedItem ()
		{
			SuperView?.LayoutSubviews ();
			if (selected < top) {
				top = selected;
			} else if (Frame.Height > 0 && selected >= top + Frame.Height) {
				top = Math.Max (selected - Frame.Height + 1, 0);
			}
		}

		public override void PositionCursor ()
		{
			if (allowsMarking)
				Move (0, selected - top);
			else
				Move (Bounds.Width - 1, selected - top);
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp &&
				me.Flags != MouseFlags.WheeledRight && me.Flags != MouseFlags.WheeledLeft)
				return false;

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}

			if (source == null) {
				return false;
			}

			if (me.Flags == MouseFlags.WheeledDown) {
				ScrollDown (1);
				return true;
			} else if (me.Flags == MouseFlags.WheeledUp) {
				ScrollUp (1);
				return true;
			} else if (me.Flags == MouseFlags.WheeledRight) {
				ScrollRight (1);
				return true;
			} else if (me.Flags == MouseFlags.WheeledLeft) {
				ScrollLeft (1);
				return true;
			}

			if (me.Y + top >= source.Count) {
				return true;
			}

			selected = top + me.Y;
            OnSelectedChanged();
            if (AllowsAll ()) {
				Source.SetMark (SelectedItem, !Source.IsMarked (SelectedItem));
				SetNeedsDisplay ();
				return true;
			}
			SetNeedsDisplay();
			if (me.Flags == MouseFlags.Button1DoubleClicked) {
				OnOpenSelectedItem ();
			}
			return true;
		}
	}

	public class ListWrapper : IListDataSource {
		IList src;
		BitArray marks;
		int count, len;

		public ListWrapper (IList source)
		{
			if (source != null) {
				count = source.Count;
				marks = new BitArray (count);
				src = source;
				len = GetMaxLengthItem ();
			}
		}

		public int Count => src != null ? src.Count : 0;

		public int Length => len;

		int GetMaxLengthItem ()
		{
			if (src?.Count == 0) {
				return 0;
			}

			int maxLength = 0;
			for (int i = 0; i < src.Count; i++) {
				var t = src [i];
				int l;
				if (t is ustring u) {
					l = u.RuneCount;
				} else if (t is string s) {
					l = s.Length;
				} else {
					l = t.ToString ().Length;
				}

				if (l > maxLength) {
					maxLength = l;
				}
			}

			return maxLength;
		}

		void RenderUstr (ConsoleDriver driver, ustring ustr, int col, int line, int width, int start = 0)
		{
			int byteLen = ustr.Length;
			int used = 0;
			for (int i = start; i < byteLen;) {
				(var rune, var size) = Utf8.DecodeRune (ustr, i, i - byteLen);
				var count = Rune.ColumnWidth (rune);
				if (used + count > width)
					break;
				driver.AddRune (rune);
				used += count;
				i += size;
			}
			for (; used < width; used++) {
				driver.AddRune (' ');
			}
		}

		public void Render (ListView container, ConsoleDriver driver, bool marked, int item, int col, int line, int width, int start = 0)
		{
			container.Move (col, line);
			var t = src [item];
			if (t == null) {
				RenderUstr (driver, ustring.Make (""), col, line, width);
			} else {
				if (t is ustring u) {
					RenderUstr (driver, u, col, line, width, start);
				} else if (t is string s) {
					RenderUstr (driver, s, col, line, width, start);
				} else {
					RenderUstr (driver, t.ToString (), col, line, width, start);
				}
			}
		}

		public bool IsMarked (int item)
		{
			if (item >= 0 && item < count)
				return marks [item];
			return false;
		}

		public void SetMark (int item, bool value)
		{
			if (item >= 0 && item < count)
				marks [item] = value;
		}

		public IList ToList ()
		{
			return src;
		}
	}

	public class ListViewItemEventArgs : EventArgs {
		public int Item { get; }
		public object Value { get; }

		public ListViewItemEventArgs (int item, object value)
		{
			Item = item;
			Value = value;
		}
	}

	public class ListViewRowEventArgs : EventArgs {
		public int Row { get; }
		public Attribute? RowAttribute { get; set; }

		public ListViewRowEventArgs (int row)
		{
			Row = row;
		}
	}
}

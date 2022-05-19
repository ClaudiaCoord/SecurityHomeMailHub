using System;
using System.Linq;
using System.Reflection;

namespace Terminal.Gui {
	public class ScrollView : View {
		View contentView = null;
		ScrollBarView vertical, horizontal;

		public ScrollView (Rect frame) : base (frame)
		{
			Initialize (frame);
		}


		public ScrollView () : base ()
		{
			Initialize (Rect.Empty);
		}

		void Initialize (Rect frame)
		{
			contentView = new View (frame);
			vertical = new ScrollBarView (1, 0, isVertical: true) {
				X = Pos.AnchorEnd (1),
				Y = 0,
				Width = 1,
				Height = Dim.Fill (showHorizontalScrollIndicator ? 1 : 0)
			};
			vertical.ChangedPosition += delegate {
				ContentOffset = new Point (ContentOffset.X, vertical.Position);
			};
			vertical.Host = this;
			horizontal = new ScrollBarView (1, 0, isVertical: false) {
				X = 0,
				Y = Pos.AnchorEnd (1),
				Width = Dim.Fill (showVerticalScrollIndicator ? 1 : 0),
				Height = 1
			};
			horizontal.ChangedPosition += delegate {
				ContentOffset = new Point (horizontal.Position, ContentOffset.Y);
			};
			horizontal.Host = this;
			vertical.OtherScrollBarView = horizontal;
			horizontal.OtherScrollBarView = vertical;
			base.Add (contentView);
			CanFocus = true;

			MouseEnter += View_MouseEnter;
			MouseLeave += View_MouseLeave;
			contentView.MouseEnter += View_MouseEnter;
			contentView.MouseLeave += View_MouseLeave;

			AddCommand (Command.ScrollUp, () => ScrollUp (1));
			AddCommand (Command.ScrollDown, () => ScrollDown (1));
			AddCommand (Command.ScrollLeft, () => ScrollLeft (1));
			AddCommand (Command.ScrollRight, () => ScrollRight (1));
			AddCommand (Command.PageUp, () => ScrollUp (Bounds.Height));
			AddCommand (Command.PageDown, () => ScrollDown (Bounds.Height));
			AddCommand (Command.PageLeft, () => ScrollLeft (Bounds.Width));
			AddCommand (Command.PageRight, () => ScrollRight (Bounds.Width));
			AddCommand (Command.TopHome, () => ScrollUp (contentSize.Height));
			AddCommand (Command.BottomEnd, () => ScrollDown (contentSize.Height));
			AddCommand (Command.LeftHome, () => ScrollLeft (contentSize.Width));
			AddCommand (Command.RightEnd, () => ScrollRight (contentSize.Width));

			AddKeyBinding (Key.CursorUp, Command.ScrollUp);
			AddKeyBinding (Key.CursorDown, Command.ScrollDown);
			AddKeyBinding (Key.CursorLeft, Command.ScrollLeft);
			AddKeyBinding (Key.CursorRight, Command.ScrollRight);

			AddKeyBinding (Key.PageUp, Command.PageUp);
			AddKeyBinding ((Key)'v' | Key.AltMask, Command.PageUp);

			AddKeyBinding (Key.PageDown, Command.PageDown);
			AddKeyBinding (Key.V | Key.CtrlMask, Command.PageDown);

			AddKeyBinding (Key.PageUp | Key.CtrlMask, Command.PageLeft);
			AddKeyBinding (Key.PageDown | Key.CtrlMask, Command.PageRight);
			AddKeyBinding (Key.Home, Command.TopHome);
			AddKeyBinding (Key.End, Command.BottomEnd);
			AddKeyBinding (Key.Home | Key.CtrlMask, Command.LeftHome);
			AddKeyBinding (Key.End | Key.CtrlMask, Command.RightEnd);
		}

		Size contentSize;
		Point contentOffset;
		bool showHorizontalScrollIndicator;
		bool showVerticalScrollIndicator;
		bool keepContentAlwaysInViewport = true;
		bool autoHideScrollBars = true;

		public Size ContentSize {
			get {
				return contentSize;
			}
			set {
				if (contentSize != value) {
					contentSize = value;
					contentView.Frame = new Rect (contentOffset, value);
					vertical.Size = contentSize.Height;
					horizontal.Size = contentSize.Width;
					SetNeedsDisplay ();
				}
			}
		}

		public Point ContentOffset {
			get {
				return contentOffset;
			}
			set {
				var co = new Point (-Math.Abs (value.X), -Math.Abs (value.Y));
				if (contentOffset != co) {
					contentOffset = co;
					contentView.Frame = new Rect (contentOffset, contentSize);
					var p = Math.Max (0, -contentOffset.Y);
					if (vertical.Position != p) {
						vertical.Position = Math.Max (0, -contentOffset.Y);
					}
					p = Math.Max (0, -contentOffset.X);
					if (horizontal.Position != p) {
						horizontal.Position = Math.Max (0, -contentOffset.X);
					}
					SetNeedsDisplay ();
				}
			}
		}

		public bool AutoHideScrollBars {
			get => autoHideScrollBars;
			set {
				if (autoHideScrollBars != value) {
					autoHideScrollBars = value;
					SetNeedsDisplay ();
				}
			}
		}

		public bool KeepContentAlwaysInViewport {
			get { return keepContentAlwaysInViewport; }
			set {
				if (keepContentAlwaysInViewport != value) {
					keepContentAlwaysInViewport = value;
					vertical.OtherScrollBarView.KeepContentAlwaysInViewport = value;
					horizontal.OtherScrollBarView.KeepContentAlwaysInViewport = value;
					Point p = default;
					if (value && -contentOffset.X + Bounds.Width > contentSize.Width) {
						p = new Point (contentSize.Width - Bounds.Width + (showVerticalScrollIndicator ? 1 : 0), -contentOffset.Y);
					}
					if (value && -contentOffset.Y + Bounds.Height > contentSize.Height) {
						if (p == default) {
							p = new Point (-contentOffset.X, contentSize.Height - Bounds.Height + (showHorizontalScrollIndicator ? 1 : 0));
						} else {
							p.Y = contentSize.Height - Bounds.Height + (showHorizontalScrollIndicator ? 1 : 0);
						}
					}
					if (p != default) {
						ContentOffset = p;
					}
				}
			}
		}

		public override void Add (View view)
		{
			if (!IsOverridden (view)) {
				view.MouseEnter += View_MouseEnter;
				view.MouseLeave += View_MouseLeave;
			}
			contentView.Add (view);
			SetNeedsLayout ();
		}

		void View_MouseLeave (MouseEventArgs e)
		{
			if (Application.mouseGrabView != null && Application.mouseGrabView != vertical && Application.mouseGrabView != horizontal) {
				Application.UngrabMouse ();
			}
		}

		void View_MouseEnter (MouseEventArgs e)
		{
			Application.GrabMouse (this);
		}

		bool IsOverridden (View view)
		{
			Type t = view.GetType ();
			MethodInfo m = t.GetMethod ("MouseEvent");

			return (m.DeclaringType == t || m.ReflectedType == t) && m.GetBaseDefinition ().DeclaringType == typeof (Responder);
		}

		public bool ShowHorizontalScrollIndicator {
			get => showHorizontalScrollIndicator;
			set {
				if (value == showHorizontalScrollIndicator) {
					return;
				}

				showHorizontalScrollIndicator = value;
				SetNeedsLayout ();
				if (value) {
					base.Add (horizontal);
					horizontal.OtherScrollBarView = vertical;
					horizontal.OtherScrollBarView.ShowScrollIndicator = value;
					horizontal.MouseEnter += View_MouseEnter;
					horizontal.MouseLeave += View_MouseLeave;
				} else {
					base.Remove (horizontal);
					horizontal.OtherScrollBarView = null;
					horizontal.MouseEnter -= View_MouseEnter;
					horizontal.MouseLeave -= View_MouseLeave;
				}
				vertical.Height = Dim.Fill (showHorizontalScrollIndicator ? 1 : 0);
			}
		}

		public override void RemoveAll ()
		{
			contentView.RemoveAll ();
		}

		public bool ShowVerticalScrollIndicator {
			get => showVerticalScrollIndicator;
			set {
				if (value == showVerticalScrollIndicator) {
					return;
				}

				showVerticalScrollIndicator = value;
				SetNeedsLayout ();
				if (value) {
					base.Add (vertical);
					vertical.OtherScrollBarView = horizontal;
					vertical.OtherScrollBarView.ShowScrollIndicator = value;
					vertical.MouseEnter += View_MouseEnter;
					vertical.MouseLeave += View_MouseLeave;
				} else {
					Remove (vertical);
					vertical.OtherScrollBarView = null;
					vertical.MouseEnter -= View_MouseEnter;
					vertical.MouseLeave -= View_MouseLeave;
				}
				horizontal.Width = Dim.Fill (showVerticalScrollIndicator ? 1 : 0);
			}
		}

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (GetNormalColor ());
			SetViewsNeedsDisplay ();
			Clear ();

			var savedClip = ClipToBounds ();
			OnDrawContent (new Rect (ContentOffset,
				new Size (Math.Max (Bounds.Width - (ShowVerticalScrollIndicator ? 1 : 0), 0),
					Math.Max (Bounds.Height - (ShowHorizontalScrollIndicator ? 1 : 0), 0))));
			contentView.Redraw (contentView.Frame);
			Driver.Clip = savedClip;

			if (autoHideScrollBars) {
				ShowHideScrollBars ();
			} else {
				if (ShowVerticalScrollIndicator) {
					vertical.Redraw (vertical.Bounds);
				}

				if (ShowHorizontalScrollIndicator) {
					horizontal.Redraw (horizontal.Bounds);
				}
			}

			if (ShowVerticalScrollIndicator && ShowHorizontalScrollIndicator) {
				AddRune (Bounds.Width - 1, Bounds.Height - 1, ' ');
			}
			Driver.SetAttribute (GetNormalColor ());
		}

		void ShowHideScrollBars ()
		{
			bool v = false, h = false; bool p = false;

			if (Bounds.Height == 0 || Bounds.Height > contentSize.Height) {
				if (ShowVerticalScrollIndicator) {
					ShowVerticalScrollIndicator = false;
				}
				v = false;
			} else if (Bounds.Height > 0 && Bounds.Height == contentSize.Height) {
				p = true;
			} else {
				if (!ShowVerticalScrollIndicator) {
					ShowVerticalScrollIndicator = true;
				}
				v = true;
			}
			if (Bounds.Width == 0 || Bounds.Width > contentSize.Width) {
				if (ShowHorizontalScrollIndicator) {
					ShowHorizontalScrollIndicator = false;
				}
				h = false;
			} else if (Bounds.Width > 0 && Bounds.Width == contentSize.Width && p) {
				if (ShowHorizontalScrollIndicator) {
					ShowHorizontalScrollIndicator = false;
				}
				h = false;
				if (ShowVerticalScrollIndicator) {
					ShowVerticalScrollIndicator = false;
				}
				v = false;
			} else {
				if (p) {
					if (!ShowVerticalScrollIndicator) {
						ShowVerticalScrollIndicator = true;
					}
					v = true;
				}
				if (!ShowHorizontalScrollIndicator) {
					ShowHorizontalScrollIndicator = true;
				}
				h = true;
			}
			var dim = Dim.Fill (h ? 1 : 0);
			if (!vertical.Height.Equals (dim)) {
				vertical.Height = dim;
			}
			dim = Dim.Fill (v ? 1 : 0);
			if (!horizontal.Width.Equals (dim)) {
				horizontal.Width = dim;
			}

			if (v) {
				vertical.SetRelativeLayout (Bounds);
				vertical.Redraw (vertical.Bounds);
			}
			if (h) {
				horizontal.SetRelativeLayout (Bounds);
				horizontal.Redraw (horizontal.Bounds);
			}
		}

		void SetViewsNeedsDisplay ()
		{
			foreach (View view in contentView.Subviews) {
				view.SetNeedsDisplay ();
			}
		}

		public override void PositionCursor ()
		{
			if (InternalSubviews.Count == 0)
				Move (0, 0);
			else
				base.PositionCursor ();
		}

		public bool ScrollUp (int lines)
		{
			if (contentOffset.Y < 0) {
				ContentOffset = new Point (contentOffset.X, Math.Min (contentOffset.Y + lines, 0));
				return true;
			}
			return false;
		}

		public bool ScrollLeft (int cols)
		{
			if (contentOffset.X < 0) {
				ContentOffset = new Point (Math.Min (contentOffset.X + cols, 0), contentOffset.Y);
				return true;
			}
			return false;
		}

		public bool ScrollDown (int lines)
		{
			if (vertical.CanScroll (lines, out _, true)) {
				ContentOffset = new Point (contentOffset.X, contentOffset.Y - lines);
				return true;
			}
			return false;
		}

		public bool ScrollRight (int cols)
		{
			if (horizontal.CanScroll (cols, out _)) {
				ContentOffset = new Point (contentOffset.X - cols, contentOffset.Y);
				return true;
			}
			return false;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			if (base.ProcessKey (kb))
				return true;

			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return false;
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp &&
				me.Flags != MouseFlags.WheeledRight && me.Flags != MouseFlags.WheeledLeft &&
				me.Flags != MouseFlags.Button1Pressed && me.Flags != MouseFlags.Button1Clicked &&
				!me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
				return false;
			}

			if (me.Flags == MouseFlags.WheeledDown && ShowVerticalScrollIndicator) {
				ScrollDown (1);
			} else if (me.Flags == MouseFlags.WheeledUp && ShowVerticalScrollIndicator) {
				ScrollUp (1);
			} else if (me.Flags == MouseFlags.WheeledRight && showHorizontalScrollIndicator) {
				ScrollRight (1);
			} else if (me.Flags == MouseFlags.WheeledLeft && ShowVerticalScrollIndicator) {
				ScrollLeft (1);
			} else if (me.X == vertical.Frame.X && ShowVerticalScrollIndicator) {
				vertical.MouseEvent (me);
			} else if (me.Y == horizontal.Frame.Y && ShowHorizontalScrollIndicator) {
				horizontal.MouseEvent (me);
			} else if (IsOverridden (me.View)) {
				Application.UngrabMouse ();
				return false;
			}
			return true;
		}

		protected override void Dispose (bool disposing)
		{
			if (!showVerticalScrollIndicator) {
				vertical?.Dispose ();
			}
			if (!showHorizontalScrollIndicator) {
				horizontal?.Dispose ();
			}
			base.Dispose (disposing);
		}

		public override bool OnEnter (View view)
		{
			if (Subviews.Count == 0 || !Subviews.Any (subview => subview.CanFocus)) {
				Application.Driver?.SetCursorVisibility (CursorVisibility.Invisible);
			}

			return base.OnEnter (view);
		}
	}
}

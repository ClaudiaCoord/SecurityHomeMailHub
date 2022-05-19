using System.Collections;
using NStack;

namespace Terminal.Gui {
	public class Window : Toplevel {
		View contentView;
		ustring title;

		public ustring Title {
			get => title;
			set {
				title = value;
				SetNeedsDisplay ();
			}
		}

		public override Border Border {
			get => base.Border;
			set {
				if (base.Border != null && base.Border.Child != null && value.Child == null) {
					value.Child = base.Border.Child;
				}
				base.Border = value;
				if (value == null) {
					return;
				}
				Rect frame;
				if (contentView != null && (contentView.Width is Dim || contentView.Height is Dim)) {
					frame = Rect.Empty;
				} else {
					frame = Frame;
				}
				AdjustContentView (frame);

				Border.BorderChanged += Border_BorderChanged;
			}
		}

		void Border_BorderChanged (Border border)
		{
			Rect frame;
			if (contentView != null && (contentView.Width is Dim || contentView.Height is Dim)) {
				frame = Rect.Empty;
			} else {
				frame = Frame;
			}
			AdjustContentView (frame);
		}


		class ContentView : View {
			Window instance;

			public ContentView (Rect frame, Window instance) : base (frame)
			{
				this.instance = instance;
			}
			public ContentView (Window instance) : base ()
			{
				this.instance = instance;
			}

			public override void OnCanFocusChanged ()
			{
				if (MostFocused == null && CanFocus && Visible) {
					EnsureFocus ();
				}

				base.OnCanFocusChanged ();
			}

			public override bool OnMouseEvent (MouseEvent mouseEvent)
			{
				return instance.OnMouseEvent (mouseEvent);
			}
		}

		public Window (Rect frame, ustring title = null) : this (frame, title, padding: 0, border: null)
		{
		}

		public Window (ustring title = null) : this (title, padding: 0, border: null)
		{
		}

		public Window () : this (title: null) { }

		public Window (Rect frame, ustring title = null, int padding = 0, Border border = null) : base (frame)
		{
			Initialize (title, frame, padding, border);
		}

		public Window (ustring title = null, int padding = 0, Border border = null) : base ()
		{
			Initialize (title, Rect.Empty, padding, border);
		}

		void Initialize (ustring title, Rect frame, int padding = 0, Border border = null)
		{
			CanFocus = true;
			ColorScheme = Colors.Base;
			Title = title;
			if (border == null) {
				Border = new Border () {
					BorderStyle = BorderStyle.Single,
					Padding = new Thickness (padding),
					BorderBrush = ColorScheme.Normal.Background
				};
			} else {
				Border = border;
			}
		}

		void AdjustContentView (Rect frame)
		{
			var borderLength = Border.DrawMarginFrame ? 1 : 0;
			var sumPadding = Border.GetSumThickness ();
			var wb = new Size ();
			if (frame == Rect.Empty) {
				wb.Width = borderLength + sumPadding.Right;
				wb.Height = borderLength + sumPadding.Bottom;
				if (contentView == null) {
					contentView = new ContentView (this) {
						X = borderLength + sumPadding.Left,
						Y = borderLength + sumPadding.Top,
						Width = Dim.Fill (wb.Width),
						Height = Dim.Fill (wb.Height)
					};
				} else {
					contentView.X = borderLength + sumPadding.Left;
					contentView.Y = borderLength + sumPadding.Top;
					contentView.Width = Dim.Fill (wb.Width);
					contentView.Height = Dim.Fill (wb.Height);
				}
			} else {
				wb.Width = (2 * borderLength) + sumPadding.Right + sumPadding.Left;
				wb.Height = (2 * borderLength) + sumPadding.Bottom + sumPadding.Top;
				var cFrame = new Rect (borderLength + sumPadding.Left, borderLength + sumPadding.Top, frame.Width - wb.Width, frame.Height - wb.Height);
				if (contentView == null) {
					contentView = new ContentView (cFrame, this);
				} else {
					contentView.Frame = cFrame;
				}
			}
			base.Add (contentView);
			Border.Child = contentView;
		}

		public override void Add (View view)
		{
			contentView.Add (view);
			if (view.CanFocus) {
				CanFocus = true;
			}
			AddMenuStatusBar (view);
		}


		public override void Remove (View view)
		{
			if (view == null) {
				return;
			}

			SetNeedsDisplay ();
			contentView.Remove (view);

			if (contentView.InternalSubviews.Count < 1) {
				CanFocus = false;
			}
			RemoveMenuStatusBar (view);
			if (view != contentView && Focused == null) {
				FocusFirst ();
			}
		}

		public override void RemoveAll ()
		{
			contentView.RemoveAll ();
		}

		public override void Redraw (Rect bounds)
		{
			var padding = Border.GetSumThickness ();
			var scrRect = ViewToScreen (new Rect (0, 0, Frame.Width, Frame.Height));
			if (!NeedDisplay.IsEmpty) {
				Driver.SetAttribute (GetNormalColor ());
				Border.DrawContent ();
			}
			var savedClip = contentView.ClipToBounds ();

			contentView.Redraw (contentView.Bounds);
			Driver.Clip = savedClip;

			ClearLayoutNeeded ();
			ClearNeedsDisplay ();
			if (Border.BorderStyle != BorderStyle.None) {
				Driver.SetAttribute (GetNormalColor ());
				if (HasFocus)
					Driver.SetAttribute (ColorScheme.HotNormal);
				Driver.DrawWindowTitle (scrRect, Title, padding.Left, padding.Top, padding.Right, padding.Bottom);
			}
			Driver.SetAttribute (GetNormalColor ());

			if (SuperView != null) {
				SuperView.SetNeedsLayout ();
				SuperView.SetNeedsDisplay ();
			}
		}

		public override void OnCanFocusChanged ()
		{
			if (contentView != null) {
				contentView.CanFocus = CanFocus;
			}
			base.OnCanFocusChanged ();
		}

		public override ustring Text {
			get => contentView.Text;
			set {
				base.Text = value;
				if (contentView != null) {
					contentView.Text = value;
				}
			}
		}

		public override TextAlignment TextAlignment {
			get => contentView.TextAlignment;
			set {
				base.TextAlignment = contentView.TextAlignment = value;
			}
		}
	}
}

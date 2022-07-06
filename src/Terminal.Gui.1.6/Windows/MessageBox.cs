
using System;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui
{
	public static class MessageBox {
        
		public static int Query(int width, int height, ustring title, ustring message, params ustring[] buttons) =>
            QueryFull(false, width, height, title, message, 0, null, TextAlignment.Centered, buttons);

        public static int Query(int width, int height, ustring title, ustring message, TextAlignment aligment, params ustring[] buttons) =>
            QueryFull(false, width, height, title, message, 0, null, aligment, buttons);

        public static int Query (ustring title, ustring message, params ustring [] buttons) =>
			QueryFull (false, 0, 0, title, message, 0, null, TextAlignment.Centered, buttons);

		public static int ErrorQuery (int width, int height, ustring title, ustring message, params ustring [] buttons) =>
			QueryFull (true, width, height, title, message, 0, null, TextAlignment.Centered, buttons);

		public static int ErrorQuery (ustring title, ustring message, params ustring [] buttons) =>
			QueryFull (true, 0, 0, title, message, 0, null, TextAlignment.Centered, buttons);

		public static int Query (int width, int height, ustring title, ustring message, int defaultButton = 0, params ustring [] buttons) =>
            QueryFull (false, width, height, title, message, defaultButton, null, TextAlignment.Centered, buttons);

		public static int Query (ustring title, ustring message, int defaultButton = 0, params ustring [] buttons) =>
            QueryFull (false, 0, 0, title, message, defaultButton, null, TextAlignment.Centered, buttons);

		public static int Query (int width, int height, ustring title, ustring message, int defaultButton = 0, Border border = null, params ustring [] buttons) =>
            QueryFull (false, width, height, title, message, defaultButton, border, TextAlignment.Centered, buttons);

		public static int Query (ustring title, ustring message, int defaultButton = 0, Border border = null, params ustring [] buttons) =>
            QueryFull (false, 0, 0, title, message, defaultButton, border, TextAlignment.Centered, buttons);

		public static int ErrorQuery (int width, int height, ustring title, ustring message, int defaultButton = 0, params ustring [] buttons) =>
            QueryFull (true, width, height, title, message, defaultButton, null, TextAlignment.Centered, buttons);

		public static int ErrorQuery (ustring title, ustring message, int defaultButton = 0, params ustring [] buttons) =>
            QueryFull (true, 0, 0, title, message, defaultButton, null, TextAlignment.Centered, buttons);

		public static int ErrorQuery (int width, int height, ustring title, ustring message, int defaultButton = 0, Border border = null, params ustring [] buttons) =>
			QueryFull (true, width, height, title, message, defaultButton, border, TextAlignment.Centered, buttons);

		public static int ErrorQuery (ustring title, ustring message, int defaultButton = 0, Border border = null, params ustring [] buttons) =>
			QueryFull (true, 0, 0, title, message, defaultButton, border, TextAlignment.Centered, buttons);

		static int QueryFull (bool useErrorColors, int width, int height, ustring title, ustring message,
			int defaultButton = 0, Border border = null, TextAlignment alignment = TextAlignment.Centered, params ustring [] buttons)
		{
			const int defaultWidth = 50;
			int textWidth = TextFormatter.MaxWidth (message, width == 0 ? defaultWidth : width);
			int textHeight = TextFormatter.MaxLines (message, textWidth);      
			int msgboxHeight = Math.Max (1, textHeight) + 3;           

			int count = 0;
			List<Button> buttonList = new List<Button> ();
			if (buttons != null && defaultButton > buttons.Length - 1) {
				defaultButton = buttons.Length - 1;
			}
			foreach (var s in buttons) {
				var b = new Button (s);
				if (count == defaultButton) {
					b.IsDefault = true;
				}
				buttonList.Add (b);
				count++;
			}

			Dialog d;
			if (width == 0 & height == 0) {
				d = new Dialog (title, buttonList.ToArray ());
				d.Height = msgboxHeight;
			} else {
				d = new Dialog (title, Math.Max (width, textWidth) + 4, height, buttonList.ToArray ());
			}

			if (border != null) {
				d.Border = border;
			}

			if (useErrorColors) {
				d.ColorScheme = Colors.Error;
			}

			if (message != null) {
				var l = new Label (textWidth > width ? 0 : (width - 4 - textWidth) / 2, 1, message);
				l.LayoutStyle = LayoutStyle.Computed;
				l.TextAlignment = alignment;
				l.X = Pos.Center ();
				l.Y = Pos.Center ();
				l.Width = Dim.Fill (2);
				l.Height = Dim.Fill (1);
				d.Add (l);
			}

			int msgboxWidth = Math.Max (defaultWidth, Math.Max (title.RuneCount + 8, Math.Max (textWidth + 4, d.GetButtonsWidth ()) + 8));          
			d.Width = msgboxWidth;

			int clicked = -1;
			for (int n = 0; n < buttonList.Count; n++) {
				int buttonId = n;
				var b = buttonList [n];
				b.Clicked += () => {
					clicked = buttonId;
					Application.RequestStop ();
				};
				if (b.IsDefault) {
					b.SetFocus ();
				}
			}

			Application.Run (d);
			return clicked;
		}
	}
}

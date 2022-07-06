/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HomeMailHub.Gui.Dialogs;
using Terminal.Gui;
using static Terminal.Gui.View;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{

    public static class GuiExtensions {
        public static GuiOpenDialog GuiOpenDialogs(this string s, string path, string[] ext = default) {
            return new GuiOpenDialog(RES.BTN_OPEN, s) {
                AllowedFileTypes = ext,
                AllowsMultipleSelection = false,
				CanChooseDirectories = true,
                CanChooseFiles = false,
                DirectoryPath = path
            };
        }
        public static GuiOpenDialog GuiOpenDialogs (this string s, bool ismulti = false, string [] ext = default) {
			return new GuiOpenDialog (RES.BTN_OPEN, s) {
				AllowedFileTypes = ext,
				AllowsMultipleSelection = ismulti,
				CanChooseFiles = true,
				DirectoryPath = GetLocalPath()
			};
		}
		public static GuiSaveDialog GuiSaveDialogs (this string s, string [] ext = default) {
			return new GuiSaveDialog (RES.BTN_SAVE, s) {
				AllowedFileTypes = ext,
				DirectoryPath = GetLocalPath()
			};
		}
        public static GuiSaveDialog GuiSaveDialogs(this string s, string path, string[] ext = default) {
            return new GuiSaveDialog(RES.BTN_SAVE, s) {
                AllowedFileTypes = ext,
                DirectoryPath = path
            };
        }
        public static string [] GuiReturnDialog (this IFileDialog d)
		{
			if (d == null)
				return new string [0];

			if ((d.FilePaths != default) && (d.FilePaths.Count > 0)) {
				string [] ss = new string [d.FilePaths.Count];
				Array.Copy (d.FilePaths.ToArray(), ss, ss.Length);
				return ss;
			}
			string s = d.FilePath.ToString();
			if (!string.IsNullOrWhiteSpace(s))
				return new string [1] { s };
			return new string [0];
		}
		public static MenuItem CreateCheckedMenuItem (this string s, Func<bool, bool> f, bool isenabled = true)
		{
			MenuItem mi = default (MenuItem);
			mi = new MenuItem (s, "", () => { mi.Checked = f.Invoke (true); }) {
				CheckType = MenuItemCheckStyle.Checked,
				Checked = f.Invoke (false),
				CanExecute = () => isenabled
			};
			return mi;
		}
		public static Toplevel CreteTop() =>
			new Toplevel () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

		public static Key ParseKeyEvent(this KeyEvent a) {

            if ((a != null) && a.IsAlt) {

                Key key = a.Key switch {
                    Key.a | Key.AltMask => Key.A,
                    Key.A | Key.AltMask => Key.A,
                    Key.b | Key.AltMask => Key.B,
                    Key.B | Key.AltMask => Key.B,
                    Key.c | Key.AltMask => Key.C,
                    Key.C | Key.AltMask => Key.C,
                    Key.d | Key.AltMask => Key.D,
                    Key.D | Key.AltMask => Key.D,
                    Key.f | Key.AltMask => Key.F,
                    Key.F | Key.AltMask => Key.F,
                    Key.r | Key.AltMask => Key.R,
                    Key.R | Key.AltMask => Key.R,
                    Key.u | Key.AltMask => Key.U,
                    Key.U | Key.AltMask => Key.U,
                    Key.o | Key.AltMask => Key.O,
                    Key.O | Key.AltMask => Key.O,
                    Key.s | Key.AltMask => Key.S,
                    Key.S | Key.AltMask => Key.S,
                    Key.t | Key.AltMask => Key.T,
                    Key.T | Key.AltMask => Key.T,
                    Key.w | Key.AltMask => Key.W,
                    Key.W | Key.AltMask => Key.W,
                    _ => Key.Unknown
                };
                if (key == Key.Unknown)
                    key = (uint)a.Key switch {
                        2147484708 => Key.A,
                        2147484740 => Key.A,
                        2147484696 => Key.B,
                        2147484728 => Key.B,
                        2147484705 => Key.C,
                        2147484737 => Key.C,
                        2147484690 => Key.D,
                        2147484722 => Key.D,
                        2147484688 => Key.F,
                        2147484720 => Key.F,
                        2147484698 => Key.R,
                        2147484730 => Key.R,
                        2147484691 => Key.U,
                        2147484723 => Key.U,
                        2147484713 => Key.O,
                        2147484745 => Key.O,
                        2147484715 => Key.S,
                        2147484747 => Key.S,
                        2147484693 => Key.T,
                        2147484725 => Key.T,
                        2147484710 => Key.W,
                        2147484742 => Key.W,
                        _ => Key.Unknown
                    };
                return key;
            }
            return Key.Unknown;
        }

        public static void SetLinearLayout(this Button btn, GuiLinearData data, Pos x, Pos y) {
            btn.X = x + data.X;
            btn.Y = y + data.Y;
            btn.AutoSize = data.AutoSize;
        }

        private static string GetLocalPath () =>
			Path.Combine (Path.GetDirectoryName (Assembly.GetExecutingAssembly().Location), "mail");
	}
}

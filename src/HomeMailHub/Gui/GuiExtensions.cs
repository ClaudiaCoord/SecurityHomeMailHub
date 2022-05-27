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
				DirectoryPath = GetLocalPath(),
			};
		}
		public static GuiSaveDialog GuiSaveDialogs (this string s, string [] ext = default)
		{
			return new GuiSaveDialog (RES.BTN_SAVE, s) {
				AllowedFileTypes = ext,
				DirectoryPath = GetLocalPath(),
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

		private static string GetLocalPath () =>
			Path.Combine (Path.GetDirectoryName (Assembly.GetExecutingAssembly().Location), "mail");
	}
}

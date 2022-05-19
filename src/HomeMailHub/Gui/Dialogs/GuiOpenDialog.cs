/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using Terminal.Gui;

namespace HomeMailHub.Gui.Dialogs {
	public class GuiOpenDialog : OpenDialog, IFileDialog {
		public GuiOpenDialog (string tag, string desc) : base (tag, desc) { }
	}
}

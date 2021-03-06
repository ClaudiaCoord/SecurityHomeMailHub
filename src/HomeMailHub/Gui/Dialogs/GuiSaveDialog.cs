/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using Terminal.Gui;

namespace HomeMailHub.Gui.Dialogs {
	public class GuiSaveDialog : SaveDialog, IFileDialog {
		public GuiSaveDialog (string tag, string desc) : base (tag, desc) { }
		public IReadOnlyList<string> FilePaths => default;
	}
}

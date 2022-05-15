
using Terminal.Gui;

namespace HomeMailHub.Gui.Dialogs {
	public class GuiOpenDialog : OpenDialog, IFileDialog {
		public GuiOpenDialog (string tag, string desc) : base (tag, desc) { }
	}
}

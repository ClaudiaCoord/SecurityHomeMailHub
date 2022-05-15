
using System.Collections.Generic;

namespace HomeMailHub.Gui.Dialogs {
	public interface IFileDialog {
		string [] AllowedFileTypes { get; set; }
		NStack.ustring DirectoryPath { get; }
		NStack.ustring FilePath { get; }
		IReadOnlyList<string> FilePaths { get; }
	}
}

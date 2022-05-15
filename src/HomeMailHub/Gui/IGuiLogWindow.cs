using Terminal.Gui;

namespace HomeMailHub.Gui {
	public interface IGuiWindow<T> {

		void Dispose();
		T Init(string s);
		void Load();
		Toplevel GetTop { get; }
	}
}
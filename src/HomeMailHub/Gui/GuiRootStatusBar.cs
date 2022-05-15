
using Terminal.Gui;

namespace HomeMailHub.Gui {

	public enum GuiStatusItemId : int {
		ServiceName = 0,
		Send,
		Receive,
		Delete,
		Total,
		Rx,
		Tx,
		Text,
		Error
	}

	public class GuiRootStatusBar : StatusBar {
		private static StatusItem [] GuiStatusItems { get; } = new StatusItem [6];
		private static GuiRootStatusBar __this = default;
		public static GuiRootStatusBar Get => __this;

		public GuiRootStatusBar () : base (GuiStatusItems) {
			GuiStatusItems [0] = (GuiStatusItems [0] == null) ? new StatusItem (Key.Null, "  - ", null) : GuiStatusItems [0];
			GuiStatusItems [1] = (GuiStatusItems [1] == null) ? new StatusItem (Key.Null, "0", null) : GuiStatusItems [1];
			GuiStatusItems [2] = (GuiStatusItems [2] == null) ? new StatusItem (Key.Null, "0", null) : GuiStatusItems [2];
			GuiStatusItems [3] = (GuiStatusItems [3] == null) ? new StatusItem (Key.Null, "0", null) : GuiStatusItems [3];
			GuiStatusItems [4] = (GuiStatusItems [4] == null) ? new StatusItem (Key.Null, "0", null) : GuiStatusItems [4];
			GuiStatusItems [5] = (GuiStatusItems [5] == null) ? new StatusItem (Key.Null, "", null) :  GuiStatusItems [5];
			__this = this;
		}
		~GuiRootStatusBar () => __this = default;

		public void UpdateStatus<T1> (GuiStatusItemId id, T1 val) where T1 : class
		{
			int idx = id switch {
				GuiStatusItemId.ServiceName => 0,
				GuiStatusItemId.Send => 1,
				GuiStatusItemId.Receive => 1,
				GuiStatusItemId.Delete => 2,
				GuiStatusItemId.Total => 2,
				GuiStatusItemId.Rx => 3,
				GuiStatusItemId.Tx => 4,
				GuiStatusItemId.Text => 5,
				GuiStatusItemId.Error => 5,
				_ => -1
			};
			if (idx < 0)
				return;

			Application.MainLoop.Invoke(() => {
				GuiStatusItems[idx].Title =
					((id == GuiStatusItemId.ServiceName) && string.IsNullOrWhiteSpace(val.ToString())) ?
						"  -  " : val.ToString();
                Redraw(Bounds);
            });
		}
	}
}

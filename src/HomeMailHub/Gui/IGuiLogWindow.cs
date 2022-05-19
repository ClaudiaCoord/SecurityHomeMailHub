/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using Terminal.Gui;

namespace HomeMailHub.Gui {
	public interface IGuiWindow<T> {

		void Dispose();
		T Init(string s);
		void Load();
		Toplevel GetTop { get; }
	}
}
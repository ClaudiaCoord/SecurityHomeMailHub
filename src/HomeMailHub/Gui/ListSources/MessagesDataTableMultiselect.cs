/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System.Collections.Generic;
using Terminal.Gui;

namespace HomeMailHub.Gui.ListSources
{
    internal class MessagesDataTableMultiselect
    {
        private List<int> selected = new();
        public bool IsEmpty => selected.Count == 0;
        public int this[int i] { get => selected[i]; set => selected[i] = value; }
        public int Count => selected.Count;
        public void Clear() => selected.Clear();
        public void Add(int i) { selected.Clear(); selected.Add(i); }
        public bool Add(Stack<TableView.TableSelection> selections) {
            selected.Clear();
            if ((selections == null) || (selections.Count == 0))
                return false;

            List<int> list = new ();
            foreach (TableView.TableSelection sel in selections)
                for (int i = 0, n = sel.Origin.Y; i < sel.Rect.Height; i++, n++)
                    list.Add(n);
            if (list.Count > 0)
                selected.AddRange(list);
            return !IsEmpty;
        }
    }
}

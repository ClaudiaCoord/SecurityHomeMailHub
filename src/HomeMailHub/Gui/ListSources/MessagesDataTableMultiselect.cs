/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using static Terminal.Gui.View;

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
        public bool Add(Stack<TableView.TableSelection> selections)
        {
            selected.Clear();
            if ((selections == null) || (selections.Count == 0))
                return false;

            List<int> list = new();
            foreach (TableView.TableSelection sel in selections)
                for (int i = 0, n = sel.Origin.Y; i < sel.Rect.Height; i++, n++)
                    list.Add(n);
            if (list.Count > 0)
                selected.AddRange(list.Distinct().Reverse());
            return !IsEmpty;
        }

        public bool MouseMultiSelect(TableView tv, MouseEventArgs a)
        {
            Point? cell = tv.ScreenToCell(a.MouseEvent.X, a.MouseEvent.Y);
            if (cell != null)
            {
                if (tv.MultiSelectedRegions.Count == 0)
                {
                    tv.MultiSelectedRegions.Push(
                        new TableView.TableSelection(
                            new Point(tv.SelectedColumn, tv.SelectedRow),
                            new Rect(tv.SelectedColumn, tv.SelectedRow, 1, 1)
                        ));
                }
                else
                {
                    var tab = (from i in tv.MultiSelectedRegions
                               where i.Rect.X == cell.Value.X && i.Rect.Y == cell.Value.Y
                               select i).FirstOrDefault();
                    if (tab != null)
                    {
                        List<TableView.TableSelection> list = tv.MultiSelectedRegions.ToList();
                        list.Remove(tab);
                        tv.MultiSelectedRegions.Clear();
                        foreach (TableView.TableSelection s in list)
                            tv.MultiSelectedRegions.Push(s);
                        tv.SelectedRow = (list.Count > 0) ? list[0].Rect.Y : -1;
                    }
                    else
                    {
                        tv.MultiSelectedRegions.Push(
                            new TableView.TableSelection(
                                cell.Value,
                                new Rect(cell.Value.X, cell.Value.Y, 1, 1)
                            ));
                    }
                }
                tv.Update();
                return true;
            }
            return false;
        }
        public bool MouseMultiSelectReset(TableView tv, MouseEventArgs a) {
            Point? cell = tv.ScreenToCell(a.MouseEvent.X, a.MouseEvent.Y);
            if (cell != null) {
                tv.MultiSelectedRegions.Clear();
                tv.SetSelection(cell.Value.X, cell.Value.Y, false);
                tv.Update();
                return true;
            }
            return false;
        }
    }
}

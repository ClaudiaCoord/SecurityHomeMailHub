/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

namespace HomeMailHub.Gui.ListSources
{
    public class ListViewItem
    {
        public string Name { get; set; }
        public bool IsEnable { get; set; }
        public bool IsExpire { get; set; }

        public ListViewItem(string s, bool isenable) {
            Name = s;
            IsEnable = isenable;
            IsExpire = false;
        }
        public ListViewItem(string s, bool isenable, bool isexpire)
        {
            Name = s;
            IsEnable = isenable;
            IsExpire = isexpire;
        }

        public void Set(bool isenable, bool isexpire)
        {
            IsEnable = isenable;
            IsExpire = isexpire;
        }
    }
}

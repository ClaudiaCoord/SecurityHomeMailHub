/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecyrityMail;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    public class GuiFrameList : FrameView, IDisposable
    {
        private int __last_id = -1;

        private Button buttonAdd { get; set; } = default;
        private Button buttonSort { get; set; } = default;
        private Button buttonDelete { get; set; } = default;

        private ListView listView { get; set; } = default;
        private Label editLabel { get; set; } = default;
        private TextField editText { get; set; } = default;
        private GuiLinearLayot linearLayot { get; } = new();

        private List<string> Items { get; } = new();
        public Func<List<string>> GetList = () => default;
        public int Count => Items.Count;
        public bool IsEmpty => Items.Count == 0;
        public string this[int i] { get => Items[i]; set => Items[i] = value; }

        public new bool Enabled {
            get => base.Enabled;
            set {
                base.Enabled =
                editText.Enabled =
                buttonSort.Enabled =
                buttonAdd.Enabled =
                buttonDelete.Enabled = value;
            }
        }

        public GuiFrameList(string title, string edit) : base(title) {

            #region linearLayot
            linearLayot.Add("en", new List<GuiLinearData> {
                new GuiLinearData(2,  1, true),
                new GuiLinearData(6,  1, true),
                new GuiLinearData(21, 3, true),
                new GuiLinearData(30, 3, true),
                new GuiLinearData(38, 3, true),
                new GuiLinearData(2,  1, true),
                new GuiLinearData(6,  1, true),
                new GuiLinearData(30, 3, true),
                new GuiLinearData(39, 3, true),
                new GuiLinearData(47, 3, true)
            });
            linearLayot.Add("ru", new List<GuiLinearData> {
                new GuiLinearData(2,  1, true),
                new GuiLinearData(6,  1, true),
                new GuiLinearData(8,  3, true),
                new GuiLinearData(24, 3, true),
                new GuiLinearData(37, 3, true),
                new GuiLinearData(2,  1, true),
                new GuiLinearData(6,  1, true),
                new GuiLinearData(17, 3, true),
                new GuiLinearData(33, 3, true),
                new GuiLinearData(46, 3, true)
            });

            int idx = 0;
            Pos bottom;
            List<GuiLinearData> layout = linearLayot.GetDefault();
            #endregion

            #region elements
            Add(listView = new ListView(Global.Instance.Config.ForbidenRouteList)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 2,
                Height = Dim.Fill() - 4,
                AllowsMarking = true,
                AllowsMultipleSelection = false
            });
            bottom = Pos.Bottom(listView);

            Add(editLabel = new Label(edit)
            {
                X = layout[idx].X,
                Y = bottom + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
            });
            Add(editText = new TextField(string.Empty)
            {
                X = layout[idx].X,
                Y = bottom + layout[idx++].Y,
                Width = 51,
                Height = 1,
                ColorScheme = GuiApp.ColorField
            });
            Add(buttonSort = new Button(RES.BTN_SORT)
            {
                X = layout[idx].X,
                Y = bottom + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
            });
            Add(buttonAdd = new Button(RES.BTN_ADD)
            {
                X = layout[idx].X,
                Y = bottom + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
            });
            Add(buttonDelete = new Button(RES.BTN_DELETE)
            {
                X = layout[idx].X,
                Y = bottom + layout[idx].Y,
                AutoSize = layout[idx++].AutoSize
            });
            listView.OpenSelectedItem += ListView_OpenSelectedItem;
            listView.SelectedItemChanged += ListView_SelectedItemChanged;
            editText.KeyUp += EditText_KeyUp;

            buttonAdd.Clicked += () => { };
            buttonSort.Clicked += () => { };
            buttonDelete.Clicked += () => { };
            #endregion

        }

        public new void Dispose() {

            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        public async Task<bool> Load(Func<List<string>> fun) =>
            await Task.Run(() => {
                try {
                    var data = fun.Invoke();
                    Items.Clear();
                    Items.AddRange(data);
                    Application.MainLoop.Invoke(() => editText.Text = string.Empty);
                    __last_id = -1;
                } catch {}
                return false;
            });

        private void ListView_SelectedItemChanged(ListViewItemEventArgs obj) =>
            SelectedList(obj.Item);

        private void ListView_OpenSelectedItem(ListViewItemEventArgs obj) =>
            SelectedList(obj.Item);

        private void EditText_KeyUp(KeyEventEventArgs obj) {
            if ((obj != null) && (obj.KeyEvent.Key == Key.Enter))
                AddList();
        }
        
        private void SelectedList(int idx) {
            if ((__last_id != idx) && (idx >= 0) && (idx < Items.Count)) {
                editText.Text = Items[idx];
                __last_id = idx;
            }
        }

        #region AddList (Enter key)
        private async void AddList()  {

            string s = editText.Text.ToString();
            if (string.IsNullOrWhiteSpace(s) || (Items.Count == 0))
                return;

            bool b = Items.Contains(s);
            if (b)
                Items.Remove(s);
            else {
                Items.Add(s);
                editText.Text = string.Empty;
            }
            __last_id = -1;
            await listView.SetSourceAsync(Items).ContinueWith((a) => {
                if (!b && (Items.Count > 0))
                listView.SelectedItem = Items.Count - 1;

            }).ConfigureAwait(false);
        }
        #endregion
    }
}

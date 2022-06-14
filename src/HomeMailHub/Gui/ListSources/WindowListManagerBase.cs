/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HomeMailHub.Gui.Dialogs;
using SecyrityMail.MailAccounts;
using SecyrityMail.Proxy.SshProxy;
using SecyrityMail.Vpn;
using Terminal.Gui;
using GuiAttribute = Terminal.Gui.Attribute;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui.ListSources
{
    public abstract class WindowListManagerBase<T1> : Window, IDisposable where T1 : class, new()
    {
        protected Button buttonPaste { get; set; } = default;
        protected Button buttonSave { get; set; } = default;
        protected Button buttonClear { get; set; } = default;
        protected Button buttonDelete { get; set; } = default;
        protected Button buttonImport { get; set; } = default;
        protected Button buttonExport { get; set; } = default;

        protected Toplevel GuiToplevel { get; set; } = default;
        protected FrameView frameList { get; set; } = default;
        protected ListView listView { get; set; } = default;

        protected string[] extension { get; set; } = default;
        protected string tag { get; set; } = string.Empty;
        protected List<ListViewItem> data { get; } = new();
        protected GuiRunOnce runOnce { get; } = new();

        private GuiAttribute colorExpired { get; set; }
        private GuiAttribute colorDisabled { get; set; }

        protected virtual bool IsEmptyForm => true;

        public Toplevel GetTop => GuiToplevel;

        public WindowListManagerBase(string s, string t, string[] ss) : base(s, 0) {

            X = 0;
            Y = 1;
            Width = Dim.Fill();
            Height = Dim.Fill() - 1;
            GuiToplevel = GuiExtensions.CreteTop();
            tag = t;
            extension = ss;

            #region frame List
            frameList = new FrameView(RES.TAG_ACCOUNTS)
            {
                X = 1,
                Y = 1,
                Width = 35,
                Height = Dim.Fill()
            };
            frameList.Add(listView = new ListView(data)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 4,
                Height = Dim.Fill() - 1,
                AllowsMarking = true,
                AllowsMultipleSelection = false
            });
            listView.OpenSelectedItem += ListView_OpenSelectedItem;
            listView.SelectedItemChanged += ListView_SelectedItemChanged;
            listView.RowRender += ListView_RowRender;
            Add(frameList);
            #endregion

            #region Buttons
            buttonPaste =  new Button(RES.BTN_PASTE);
            buttonSave =   new Button(RES.BTN_SAVE)   { Enabled = false };
            buttonClear =  new Button(RES.BTN_CLEAR)  { Enabled = false };
            buttonDelete = new Button(RES.BTN_DELETE) { Enabled = false };
            buttonExport = new Button(RES.BTN_EXPORT) { Enabled = false };
            buttonImport = new Button(RES.BTN_IMPORT);

            buttonSave.Clicked += async () => await SaveItem().ConfigureAwait(false);
            buttonClear.Clicked += () => Clean();
            buttonDelete.Clicked += () => DeleteItem();
            buttonPaste.Clicked += async () => await ImportClipBoard().ConfigureAwait(false);
            buttonImport.Clicked += async () => {
                try {
                    GuiOpenDialog d = string.Format(RES.GUIACCOUNT_FMT6, RES.TAG_OPEN_IMPORT, tag).GuiOpenDialogs(true, extension);
                    Application.Run(d);
                    if (!d.Canceled) {
                        string[] ss = d.GuiReturnDialog();
                        if (ss != null)
                            _ = await ImportFile(ss).ConfigureAwait(false);
                    }
                } catch (Exception ex) { ex.StatusBarError(); }
            };
            buttonExport.Clicked += async () => {
                try {
                    if (!runOnce.IsValidIds())
                        return;
                    T1 acc = VirtualGetItem(runOnce.Ids);
                    if (acc == default)
                        return;
                    GuiSaveDialog d = string.Format(RES.GUIACCOUNT_FMT6, RES.TAG_SAVE_EXPORT, runOnce.Ids).GuiSaveDialogs(extension);
                    Application.Run(d);
                    if (!d.Canceled) {
                        string[] ss = d.GuiReturnDialog();
                        if (ss.Length > 0)
                            _ = await ExportFile(acc, ss[0]).ConfigureAwait(false);
                    }
                } catch (Exception ex) { ex.StatusBarError(); }
            };
            #endregion

            #region color reender
            colorExpired = new GuiAttribute(Color.BrightRed, Color.Blue);
            colorDisabled = new GuiAttribute(Color.DarkGray, Color.Blue);
            #endregion
        }

        public new void Dispose() {

            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        #region Buttons enable
        protected void ButtonsEnable(bool b1, bool b2) {
            buttonDelete.Enabled = buttonExport.Enabled = b1;
            buttonSave.Enabled = buttonClear.Enabled = b2;
            buttonImport.Enabled = true;
            if (buttonPaste != null)
                buttonPaste.Enabled = true;
        }
        #endregion

        #region Enable box toggled
        protected virtual void VirtualEnableToggled(bool b) { }
        protected void EnableBox_Toggled(bool b) {
            Application.MainLoop.Invoke(() => {
                bool b2 = !IsEmptyForm && b;
                VirtualEnableToggled(b);
                ButtonsEnable(b2, b2);
                buttonSave.Enabled = true;
            });
            if (runOnce.IsRange(data.Count))
                data[runOnce.Id].IsEnable = b;
        }
        #endregion

        #region Clear
        protected void DataClear(bool b = true) {
            data.Clear();
            Clean();
            if (b) SetList();
        }
        protected void DataRemove(string s) {
            var a = (from i in data where i.Name.Equals(s) select i).FirstOrDefault();
            if (a == null) return;
            data.Remove(a);
            Clean();
            SetList();
        }
        protected virtual void VirtualClean() { }
        protected void Clean() {
            Application.MainLoop.Invoke(() => {
                VirtualClean();
                ButtonsEnable(false, false);
            });
            runOnce.ResetId();
        }
        #endregion

        #region Set list
        protected async void SetList() {
            try {
                if (data.Count > 0) {
                    List<string> list = (from i in data select i.Name).ToList();
                    await listView.SetSourceAsync(list).ConfigureAwait(false);
                } else
                    await listView.SetSourceAsync(new List<string>()).ConfigureAwait(false);
                Application.MainLoop.Invoke(() => frameList.Title = string.Empty.GetListTitle(data.Count));
            } catch (Exception ex) { ex.StatusBarError(); }
        }
        #endregion

        #region Add item
        protected virtual void VirtualAddItem(T1 acc) { }
        protected void AddItem(T1 acc, bool b) {
            try {
                string name;
                bool isvalid,
                     isenable,
                     isexpired;

                (name, isenable, isexpired, isvalid) = GetItemValues(acc);
                if (!isvalid) return;

                if (b) {
                    if (runOnce.IsRange(data.Count)) {
                        runOnce.ChangeId(name);
                        data[runOnce.Id].Set(isenable, isexpired);
                    }
                    return;
                }
                VirtualAddItem(acc);

                data.Add(new(name, isenable, isexpired));
                runOnce.ChangeId(data.Count - 1, name);
                data[runOnce.Id].Set(isenable, isexpired);
                SetList();
                Application.MainLoop.Invoke(() => listView.SelectedItem = runOnce.Id);
            } catch (Exception ex) { ex.StatusBarError(); }
        }

        private (string, bool, bool, bool) GetItemValues(T1 acc) {
            if (acc is VpnAccount vpn)
                return (vpn.Name, vpn.Enable, vpn.IsExpired, true);
            else if (acc is SshAccount ssh)
                return (ssh.Name, ssh.Enable, ssh.IsExpired, true);
            else if (acc is UserAccount usr)
                return (usr.Email, usr.Enable, false, true);
            return (string.Empty, false, false, false);
        }
        #endregion

        #region Select item
        protected virtual void VirtualSelectItem(T1 acc) { }
        protected void SelectItem(string s, int id) {
            if (string.IsNullOrEmpty(s) || !runOnce.Begin(id))
                return;
            try {
                T1 a = VirtualGetItem(s);
                if (a == default) {
                    buttonSave.Enabled = buttonClear.Enabled =
                    buttonDelete.Enabled = buttonExport.Enabled = false;
                    return;
                }
                VirtualSelectItem(a);
            } finally { runOnce.End(); }
        }
        #endregion

        #region Save all
        protected virtual async Task<bool> VirtualSaveAll() => await Task.FromResult(false).ConfigureAwait(false);
        #endregion

        #region Save item
        protected virtual T1 VirtualNewItem() => default;
        protected virtual T1 VirtualGetItem(string s) => default;
        protected virtual void VirtualBuildItem(T1 acc) { }
        protected async Task<bool> SaveItem() =>
            await Task.Run(async () => {

                if (!runOnce.Begin())
                    return false;

                try {
                    T1 a = default;
                    bool b = runOnce.IsValidIds();
                    if (!b) {
                        runOnce.ResetId();
                        a = VirtualNewItem();
                        if (a == default)
                            return false;
                    } else {
                        a = VirtualGetItem(runOnce.Ids);
                        if (a == default) {
                            b = false;
                            runOnce.ResetId();
                            a = VirtualNewItem();
                            if (a == default)
                                return false;
                        }
                    }
                    if (a != default) {
                        VirtualBuildItem(a);
                        AddItem(a, b);
                        if (!runOnce.IsValidIds())
                            runOnce.ChangeId(VirtualGetSelectedName(a));
                        _ = await VirtualSaveAll().ConfigureAwait(false);
                    }
                    return true;
                }
                catch (Exception ex) { ex.StatusBarError(); }
                finally { runOnce.End(); }
                return false;
            });
        #endregion

        #region Delete item
        protected virtual void VirtualDeleteItem(T1 acc) { }
        protected async void DeleteItem() {

            if (!runOnce.IsRange(data.Count) || !runOnce.Begin())
                return;

            try {
                string s = data[runOnce.Id].Name;
                if (string.IsNullOrWhiteSpace(s))
                    return;

                if (MessageBox.Query(50, 7,
                    string.Format(RES.GUIACCOUNT_FMT5, RES.TAG_DELETE, s),
                    string.Format(RES.GUIACCOUNT_FMT3, RES.TAG_DELETE, s), RES.TAG_YES, RES.TAG_NO) == 0) {
                    try {
                        T1 a = VirtualGetItem(s);
                        if (a == default)
                            return;

                        DataRemove(VirtualGetSelectedName(a));
                        VirtualDeleteItem(a);
                        _ = await VirtualSaveAll().ConfigureAwait(false);
                    } catch (Exception ex) { ex.StatusBarError(); }
                }
            }
            catch (Exception ex) { ex.StatusBarError(); }
            finally { runOnce.End(); }
        }
        #endregion

        #region Get selected name
        protected virtual string VirtualGetSelectedName(T1 acc) {
            if (acc is VpnAccount vpn)
                return vpn.Name;
            else if (acc is SshAccount ssh)
                return ssh.Name;
            else if (acc is UserAccount usr)
                return usr.Email;
            else return string.Empty;
        }
        #endregion

        #region Import ClipBoard
        protected virtual async Task<T1> VirtualImportClipBoard(string s) => await Task.FromResult(default(T1)).ConfigureAwait(false);
        protected async Task<bool> ImportClipBoard() =>
            await Task.Run(async () => {
                try
                {

                    if (Clipboard.Contents.IsEmpty)
                        return false;

                    T1 a = await VirtualImportClipBoard(Clipboard.Contents.ToString())
                                 .ConfigureAwait(false);
                    if (a != default) {
                        Clean();
                        runOnce.ChangeId(VirtualGetSelectedName(a));
                        VirtualSelectItem(a);
                        return true;
                    }
                    $"{DateTime.Now:mm:ss} - {RES.TAG_IMPORTDATA_WARNING}".StatusBarText();
                }
                catch (Exception ex) { ex.StatusBarError(); }
                return false;
            });
        #endregion

        #region Import File
        protected virtual async Task<T1> VirtualImportFile(string s) => await Task.FromResult(default(T1)).ConfigureAwait(false);
        protected async Task<bool> ImportFile(string[] ss) =>
            await Task.Run(async () => {
                try {
                    if ((ss != null) && (ss.Length > 0)) {
                        bool issave = false;
                        foreach (string s in ss) {
                            try {
                                T1 a = await VirtualImportFile(s)
                                             .ConfigureAwait(false);

                                if (a != default) {
                                    Clean();
                                    AddItem(a, false);
                                    VirtualSelectItem(a);
                                    issave = true;
                                    return true;
                                } else {
                                    runOnce.ResetId();
                                    $"{Path.GetFileName(s)} - {RES.TAG_IMPORTDATA_WARNING}".StatusBarText();
                                }
                            } catch (Exception ex) { ex.StatusBarError(); }
                        }
                        if (issave)
                            _ = await VirtualSaveAll().ConfigureAwait(false);
                    }
                } catch (Exception ex) { ex.StatusBarError(); }
                return false;
            });
        #endregion

        #region Export File
        protected virtual async Task<string> VirtualExport(T1 acc, string s) => await Task.FromResult(string.Empty).ConfigureAwait(false);
        protected async Task<bool> ExportFile(T1 acc, string s) =>
            await Task.Run(async () => {
                try {
                    if ((acc == null) || string.IsNullOrWhiteSpace(s))
                        return false;
                    string res = await VirtualExport(acc, s).ConfigureAwait(false);
                    bool b = !string.IsNullOrWhiteSpace(res);
                    if (b) res.StatusBarText();
                    return b;
                }
                catch (Exception ex) { ex.StatusBarError(); }
                return false;
            });

        #endregion

        #region LoadAccounts
        protected async Task<bool> LoadAccounts(List<T1> list) =>
            await Task.Run(() => {
                try {
                    DataClear(false);
                    foreach (var acc in list) {
                        if (acc is VpnAccount vpn)
                            data.Add(new(vpn.Name, vpn.Enable, vpn.IsExpired));
                        else if (acc is SshAccount ssh)
                            data.Add(new(ssh.Name, ssh.Enable, ssh.IsExpired));
                        else if (acc is UserAccount usr)
                            data.Add(new(usr.Email, usr.Enable));
                    }
                    SetList();
                    Clean();
                } catch (Exception ex) { ex.StatusBarError(); }
                return true;
            });
        #endregion

        #region private event
        private void ListView_RowRender(ListViewRowEventArgs obj) {
            if ((obj == null) || (obj.Row < 0) || (obj.Row >= data.Count))
                return;
            else if (obj.Row == listView.SelectedItem)
                return;
            else if (!data[obj.Row].IsEnable)
                obj.RowAttribute = colorDisabled;
            else if (data[obj.Row].IsExpire)
                obj.RowAttribute = colorExpired;
        }
        private void ListView_OpenSelectedItem(ListViewItemEventArgs obj) => SelectedListItem(obj);
        private void ListView_SelectedItemChanged(ListViewItemEventArgs obj) => SelectedListItem(obj);
        private void SelectedListItem(ListViewItemEventArgs obj)
        {
            if (obj == null)
                return;
            if ((obj.Item >= 0) && (obj.Item < data.Count))
                SelectItem(data[obj.Item].Name, obj.Item);
        }
        #endregion
    }
}

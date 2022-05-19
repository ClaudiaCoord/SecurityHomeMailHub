/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SecyrityMail.Utils;

namespace SecyrityMail.Data
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = false)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class AccountsBase<T1,T2> : MailEvent, IMailEventProxy where T1 : class where T2 : class
    {
        [XmlIgnore]
        public static readonly string FileName = $"{typeof(T1).Name}.conf";

        protected T2 AccountSelected_ = default;
        [XmlIgnore]
        public virtual T2 AccountSelected { get { return AccountSelected_; } protected set { AccountSelected_ = value; OnPropertyChanged(); } }
        [XmlIgnore]
        public virtual bool IsAccountSelected => AccountSelected != default;
        [XmlIgnore]
        public virtual bool IsExpired => false;

        [XmlElement("items")]
        public List<T2> Items { get; set; } = new();
        public bool IsEmpty => Items.Count == 0;
        public T2 this[int i] { get => Items[i]; set => Items[i] = value; }
        public int Count => Items.Count;
        public void Clear() { Items.Clear(); OnPropertyChanged(typeof(T1).Name); }
        public void Add(T2 acc)
        {
            Items.Add(acc);
            OnPropertyChanged(typeof(T1).Name);
        }
        public virtual T2 Find(string login) => throw new NotImplementedException();
        public virtual T1 AddOnce(T2 acc) => throw new NotImplementedException();
        public virtual bool Copy(T1 accs) => throw new NotImplementedException();

        public virtual async Task<bool> RandomSelect() =>
            await Task.Run(() =>
            {
                Random rnd = new Random();
                AccountSelected = Items[rnd.Next(0, Items.Count)];
                return true;
            });

        public async Task<bool> Load() =>
            await Load(Global.GetRootFile(Global.DirectoryPlace.Root, FileName), false);

        public async Task<bool> Load(bool isbackup) =>
            await Load(Global.GetRootFile(Global.DirectoryPlace.Root, FileName), isbackup);

        public async Task<bool> Load(string path, bool isbackup = false) =>
            await Task.Run(() => {
                try {
                    T1 accs = path.BasePathFile(isbackup).DeserializeFromFile<T1>();
                    return Copy(accs);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Load), ex); }
                return false;
            });

        public async Task<bool> Save(T1 clz) =>
            await Save(clz, Global.GetRootFile(Global.DirectoryPlace.Root, FileName), false);

        public async Task<bool> Save(T1 clz, bool isbackup) =>
            await Save(clz, Global.GetRootFile(Global.DirectoryPlace.Root, FileName), isbackup);

        public async Task<bool> Save(T1 clz, string path, bool isbackup = false) =>
            await Task.Run(() => {
                try {
                    path.BasePathFile(isbackup).SerializeToFile<T1>(clz);
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Save), ex); }
                return false;
            });

    }
}

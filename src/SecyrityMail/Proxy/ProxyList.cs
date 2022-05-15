
using System;
using System.Collections.Generic;
using SecyrityMail.Data;

namespace SecyrityMail.Proxy
{
    public class ProxyList : MailEvent, IMailEventProxy
    {
        private int currentIndex_ = 0;
        private List<Tuple<string, int>> Items = new();
        private ProxyType proxyType = ProxyType.None;

        public ProxyType ProxyType { get => proxyType; set { proxyType = value; OnPropertyChanged(); } }
        public int Count => Items.Count;
        public List<Tuple<string, int>> GetItems() => Items;
        public int GetIndex() => (Selected != default) ? Items.IndexOf(Selected) + 1 : 0;
        public int GetIndex(Tuple<string, int> t) => Items.IndexOf(t);
        public int GetActiveIndex() => IsActive ? Items.IndexOf(Active) : -1;
        public void Remove(int i) { if (CheckIndex(i)) Items.RemoveAt(i); OnPropertyChanged(nameof(ProxyList)); }
        public Tuple<string, int> Get(int i) => CheckIndex(i) ? Items[i] : default;
        public Tuple<string, int> Next
        {
            get {
                if ((Count == 0) || (currentIndex_ == Count)) {
                    currentIndex_ = 0;
                    Selected = default;
                }
                else
                    Selected = Items[currentIndex_++];
                OnPropertyChanged(nameof(Selected));
                return Selected;
            }
        }
        public bool IsEmpty => Count == 0;
        public bool IsForeach => (currentIndex_ > 0) && (Selected != default);
        public bool IsActive => Active != default;
        public Tuple<string, int> Selected { get; private set; } = default(Tuple<string, int>);
        public Tuple<string, int> Active { get; set; } = default(Tuple<string, int>);
        public bool CheckIndex(int i) => (Count > i) && (i >= 0);
        public void ActiveSelected()
        {
            Active = Selected;
            OnPropertyChanged(nameof(Active));
        }
        public void ActiveClean()
        {
            Active = default;
            OnPropertyChanged(nameof(Active));
        }
        public void Set(ProxyType type, List<Tuple<string, int>> list)
        {
            Clear();
            ProxyType = type;
            Items.AddRange(list);
            OnPropertyChanged(nameof(ProxyList));
        }
        public void Reset()
        {
            currentIndex_ = 0;
            Selected = default;
        }
        public void Clear()
        {
            currentIndex_ = 0;
            ProxyType = ProxyType.None;
            Selected = Active = default;
            Items.Clear();
            OnPropertyChanged(nameof(ProxyList));
        }
    }
}

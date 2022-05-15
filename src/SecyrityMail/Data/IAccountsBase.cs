
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecyrityMail.Data
{
    public interface IAccountsBase<T1, T2>
        where T1 : class
        where T2 : class
    {
        T2 this[int i] { get; set; }

        T2 AccountSelected { get; }
        int Count { get; }
        bool IsAccountSelected { get; }
        bool IsEmpty { get; }
        bool IsExpired { get; }
        List<T2> Items { get; set; }

        void Add(T2 acc);
        T1 AddOnce(T2 acc);
        T2 Find(string login);
        void Clear();
        bool Copy(T1 accs);
        Task<bool> RandomSelect();
        Task<bool> Load();
        Task<bool> Load(bool isbackup);
        Task<bool> Load(string path, bool isbackup = false);
        Task<bool> Save(bool isbackup = false);
        Task<bool> Save(T1 accs, bool isbackup);
        Task<bool> Save(T1 accs, string path, bool isbackup = false);
    }
}
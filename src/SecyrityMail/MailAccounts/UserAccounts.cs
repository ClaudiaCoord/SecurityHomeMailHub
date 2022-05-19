/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SecyrityMail.Data;

namespace SecyrityMail.MailAccounts
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    [XmlInclude(typeof(AccountsBase<UserAccounts, UserAccount>))]
    public class UserAccounts : AccountsBase<UserAccounts, UserAccount>, IAccountsBase<UserAccounts, UserAccount>, IAutoInit
    {
        public void OnChange() { OnPropertyChanged(nameof(UserAccounts)); }
        public override UserAccounts AddOnce(UserAccount acc) {
            Items.Add(acc);
            OnPropertyChanged(nameof(UserAccounts));
            return this;
        }

        public override bool Copy(UserAccounts accs) {
            if ((accs == null) || (accs.Items == null) || (accs.Items.Count == 0))
                return false;
            Items.Clear();
            Items.AddRange(accs.Items);
            return Items.Count > 0;
        }

        public override UserAccount Find(string login) {
            if (Items.Count == 0)
                return default;
            return (from i in Items where (i.Login.Equals(login) || i.Email.Equals(login)) && i.Enable select i).FirstOrDefault();
        }

        public UserAccount FindFromEmail(string login) {
            if (Items.Count == 0)
                return default;
            return (from i in Items where ((i.Login.Contains('@') && i.Login.Equals(login)) || i.Email.Equals(login)) && i.Enable select i).FirstOrDefault();
        }

        public async Task<bool> Save(bool isbackup = false) => await Save(this, isbackup);
        public async Task AutoInit() => _ = await Load().ConfigureAwait(false);
    }
}

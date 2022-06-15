/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecyrityMail.MailFilters
{
    public class FromFilter : ISpamFilter
    {
        public static FromFilter Create() {
            FromFilter filter = new();
            if (Global.Instance.Config.FilterFromList.Count > 0)
                filter.Set(Global.Instance.Config.FilterFromList);
            return filter;
        }

        public FromFilter() { }
        public FromFilter(List<string> list) => Set(list);

        HashSet<string> _emails = new ();
        HashSet<string> _domain = new ();

        public bool IsEnable => (_emails.Count > 0) || (_domain.Count > 0);
        public bool IsAutoLearn => false;
        private string GetTag(string s) => $"From filter {s}";

        public async Task LearnSpam(SpamFilterData sfd) => await Task.FromResult(0);
        public async Task LearnHam(SpamFilterData sfd) => await Task.FromResult(0);

        public async Task<SpamStatusType> CheckSpam(SpamFilterData sfd) =>
            await Task.Run(() => {
                try {
                    if ((sfd == null) || string.IsNullOrWhiteSpace(sfd.Address) || ((_emails.Count == 0) && (_domain.Count == 0)))
                        return SpamStatusType.UnCheck;

                    bool b = false;
                    if (_emails.Count > 0)
                        b = (from i in _emails
                             where i.Equals(sfd.Address, StringComparison.InvariantCultureIgnoreCase)
                             select i).FirstOrDefault() != null;
                    if (!b && (_domain.Count > 0)) {
                        int x = sfd.Address.IndexOf('@'),
                            len = sfd.Address.Length - x;
                        string ss;
                        if ((x > 0) && (len > 0)) ss = sfd.Address.Substring(x, len);
                        else ss = sfd.Address;
                        b = (from i in _domain
                         where ss.Equals(i, StringComparison.InvariantCultureIgnoreCase)
                         select i).FirstOrDefault() != null;
                    }
#                   if DEBUG
                    if (b)
                        Global.Instance.Log.Add(GetTag(nameof(CheckSpam)), $"Checked message is spam: {sfd.Address}");
#                   endif
                    return b ? SpamStatusType.Spam : SpamStatusType.Ham;
                }
                catch (Exception ex) { Global.Instance.Log.Add(GetTag(nameof(CheckSpam)), ex); }
                return SpamStatusType.Error;
            });

        public void Set(List<string> list) {
            foreach (string s in list) {
                if (s.Contains('@')) _emails.Add(s.Trim());
                else _domain.Add(s.Trim());
           }
        }
    }
}

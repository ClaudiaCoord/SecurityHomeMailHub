/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using MimeKit;
using SecyrityMail.Utils;

namespace SecyrityMail.MailFilters
{
    public enum SpamType : int
    {
        None = 0,
        Error,
        Spam,
        Ham,
        UnCheck
    }

    public class SpamFilter
    {
        private List<Lazy<ISpamFilter>> filters { get; } = new();

        public SpamFilter() {
            filters.Add(new Lazy<ISpamFilter>(() => AkismetFilter.Create()));
        }

        public async Task LearnSpam(SpamFilterData sfd) =>
            await Learn(SpamType.Spam, sfd).ConfigureAwait(false);

        public async Task LearnHam(SpamFilterData sfd) =>
            await Learn(SpamType.Ham, sfd).ConfigureAwait(false);

        public async Task<SpamType> Check(MimeMessage mmsg, EndPoint ip) =>
            await Task.Run(async () => {
                try {
                    SpamType type = SpamType.Error;
                    if (mmsg == null)
                        return type;

                    SpamFilterData sfd = Parse(mmsg, ip);
                    if (sfd.IsEmpty)
                        return type;

                    type = SpamType.Ham;
                    foreach (Lazy<ISpamFilter> filter in filters)
                        if (filter.Value.IsEnable && (type = await filter.Value.CheckSpam(sfd)) == SpamType.Spam) {
                            Global.Instance.Log.Add(filter.Value.GetType().Name, $"message from {sfd.Name} marked as SPAM!");
                            break;
                        }
                    return type;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(SpamFilter), ex); }
                return SpamType.Error;
            });

        private async Task Learn(SpamType type, SpamFilterData sfd) =>
            await Task.Run(async () => {
                try {
                    foreach (Lazy<ISpamFilter> filter in filters)
                        if (filter.Value.IsAutoLearn) {
                            switch (type) {
                                case SpamType.Spam: await filter.Value.LearnSpam(sfd).ConfigureAwait(false); break;
                                case SpamType.Ham: await filter.Value.LearnHam(sfd).ConfigureAwait(false); break;
                            }
                        }
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(SpamFilter), ex); }
            });

        private SpamFilterData Parse(MimeMessage mmsg, EndPoint ip) {
            if (mmsg == null)
                return new();

            SpamFilterData sfd = new();
            MailboxAddress from = ((mmsg.From == null) || (mmsg.From.Count == 0)) ? default : mmsg.From[0] as MailboxAddress;
            sfd.Address = (from == null) ? string.Empty : from.Address;
            sfd.Name = (from == null) ? string.Empty : from.Name;
            sfd.Subject = string.IsNullOrWhiteSpace(mmsg.Subject) ? string.Empty : mmsg.Subject.ToString();
            sfd.Ip = (ip == null) ? string.Empty : ip.ToString();

            if (!string.IsNullOrWhiteSpace(mmsg.TextBody))
                sfd.Body = mmsg.TextBody;
            else if (!string.IsNullOrWhiteSpace(mmsg.HtmlBody))
                sfd.Body = new ConverterHtmlToHtml().ConvertT(mmsg.HtmlBody);
            return sfd;
        }
    }
}

/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System.Threading.Tasks;

namespace SecyrityMail.MailFilters
{
    public interface ISpamFilter
    {
        bool IsEnable { get; }
        bool IsAutoLearn { get; }

        Task<SpamStatusType> CheckSpam(SpamFilterData sfd);
        Task LearnSpam(SpamFilterData sfd);
        Task LearnHam(SpamFilterData sfd);
    }
}

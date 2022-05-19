/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using MimeKit;

namespace SecyrityMail.Messages.Bouncer
{
    public sealed class BounceDetector
    {
        public static BounceDetectResult Detect(MimeMessage message)
        {
            return QmailBounceDetector.Detect(message) ?? StandardBounceDetector.Detect(message);
        }
    }
}

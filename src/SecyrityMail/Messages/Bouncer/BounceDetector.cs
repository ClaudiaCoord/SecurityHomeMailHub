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

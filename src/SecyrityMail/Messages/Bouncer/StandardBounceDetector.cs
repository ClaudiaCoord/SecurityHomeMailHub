using MimeKit;

namespace SecyrityMail.Messages.Bouncer
{
    public sealed class StandardBounceDetector
    {
        public static BounceDetectResult Detect(MimeMessage message)
        {
            var visitor = new Visitor();

            message.Accept(visitor);

            var result = visitor.Result;

            return new BounceDetectResult(
                message,
                result.DeliveryNotificationPart,
                result.DeliveryStatus,
                result.UndeliveredMessagePart
            );
        }

        private sealed class VisitorResult
        {
            public MimeEntity DeliveryNotificationPart { get; set; }
            public MessageDeliveryStatus DeliveryStatus { get; set; }
            public MimeEntity UndeliveredMessagePart { get; set; }
        }

        private sealed class Visitor : MimeVisitor
        {
            public VisitorResult Result { get; } = new VisitorResult();

            protected override void VisitMultipart(Multipart multipart)
            {
                base.VisitMultipart(multipart);

                if (multipart.ContentType.MediaSubtype == "report" && multipart.ContentType.Parameters["report-type"] == "delivery-status")
                {
                    if (multipart.Count > 0)
                    {
                        Result.DeliveryNotificationPart = multipart[0];
                    }

                    if (multipart.Count > 1)
                    {
                        Result.DeliveryStatus = multipart[1] as MessageDeliveryStatus;
                    }

                    if (multipart.Count > 2)
                    {
                        Result.UndeliveredMessagePart = multipart[2];
                    }
                }
            }
        }
    }
}

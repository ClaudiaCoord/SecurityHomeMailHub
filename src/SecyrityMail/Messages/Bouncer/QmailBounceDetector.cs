/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using MimeKit;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SecyrityMail.Messages.Bouncer
{
    public sealed class QmailBounceDetector
    {
        public static BounceDetectResult Detect(MimeMessage message)
        {
            var textPart = message.Body as TextPart;

            if (textPart != null)
            {
                var result = DetectQmailBounce(message, textPart);

                if (result != null)
                    return result;
            }

            var multipart = message.Body as MultipartAlternative;

            return multipart?
                .OfType<TextPart>()
                .Select(p => DetectQmailBounce(message, p))
                .FirstOrDefault();
        }

        private static readonly Regex QmailBounceRegex = new Regex(@"^Hi. This is the .+ (.+)\.\r?\n", RegexOptions.Compiled);
        private static readonly Regex LinesRegex = new Regex(@"\r?\n", RegexOptions.Compiled);
        private static readonly Regex ParagraphRegex = new Regex(@"(?:\r?\n){2,}", RegexOptions.Compiled);
        private static readonly Regex BreakParagraphRegex = new Regex(@"\r?\n-.+?\r?\n\r?\n", RegexOptions.Compiled);
        private static readonly Regex FailureParagraphRegex = new Regex(@"^<(.+?)>:\r?\n(.+)", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex StatusRegex = new Regex(@" (\d\.\d+\.\d+) (.+)", RegexOptions.Compiled);

        private static BounceDetectResult DetectQmailBounce(MimeMessage message, TextPart textPart)
        {
            if (textPart.ContentType.MimeType != "text/plain")
                return null;

            var text = textPart.Text;

            if (!text.StartsWith("Hi. This is the "))
                return null;

            var match = QmailBounceRegex.Match(text);

            if (!match.Success)
                return null;

            var parts = BreakParagraphRegex.Split(text, 2).ToArray();

            if (parts.Length != 2)
                return null;

            var paragraphs = ParagraphRegex.Split(parts[0]);
            var recipientParagraphs = paragraphs.Skip(1).ToArray();
            var undeliveredMessage = ReadMessage(parts[1]);

            var messageDeliveryStatus = new MessageDeliveryStatus();

            messageDeliveryStatus.StatusGroups.Add(
                new HeaderList
                {
                    { "Reporting-MTA", "dns;" + match.Groups[1].Value }
                });

            var failureParagraphs = recipientParagraphs
                .Select(ParseFailureParagraph)
                .Where(p => p != null)
                .ToArray();

            foreach (var failureParagraph in failureParagraphs)
            {
                messageDeliveryStatus.StatusGroups.Add(
                    new HeaderList
                    {
                        { "Action", "failed" },
                        { "Status", failureParagraph.Status },
                        { "Final-Recipient",  "rfc822;" + failureParagraph.RecipientAddress },
                        { "Diagnostic-Code", "X-QMail;" + LinesRegex.Replace(failureParagraph.Message, " ").Trim() },
                    });
            }

            if (failureParagraphs.Length == 0)
            {
                messageDeliveryStatus.StatusGroups.Add(
                   new HeaderList
                   {
                        { "Action", "failed" },
                        { "Status", "5.3.0" },
                        { "Diagnostic-Code", "X-QMail; No failure paragraphs found" },
                   });
            }

            return new BounceDetectResult(
                message,
                new TextPart("plain", parts[0]),
                messageDeliveryStatus,
                new MessagePart("rfc822", undeliveredMessage));
        }

        private static FailureParagraph ParseFailureParagraph(string text)
        {
            var match = FailureParagraphRegex.Match(text);

            if (!match.Success)
                return null;

            var message = match.Groups[2].Value;

            var status = LinesRegex
                .Split(message)
                .Select(ParseStatusLine)
                .FirstOrDefault(s => s != null);

            return new FailureParagraph
            {
                RecipientAddress = match.Groups[1].Value,
                Message = message,
                Status = status ?? "5.3.0",
            };
        }

        private static string ParseStatusLine(string text)
        {
            var match = StatusRegex.Match(text);

            return match.Success
                ? match.Groups[1].Value
                : null;
        }

        private class FailureParagraph
        {
            public string RecipientAddress { get; set; }
            public string Message { get; set; }
            public string Status { get; set; }
        }

        private static MimeMessage ReadMessage(string text)
        {
            using (var data = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                return MimeMessage.Load(data);
            }
        }
    }
}

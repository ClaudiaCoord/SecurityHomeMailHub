/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System.Text;
using MimeKit;
using MimeKit.Text;

namespace SecyrityMail.Utils
{
    public class ConverterTextToHtml
    {
        public ConverterTextToHtml(Encoding enc) =>
            EncodingText = enc;
        public ConverterTextToHtml() =>
            EncodingText = new UTF8Encoding(false);

        public Encoding EncodingText { get; set; } = default(Encoding);

        public string Convert(string s)
        {
            do {
                if (string.IsNullOrEmpty(s))
                    break;
                try {
                    TextToHtml converter = new() {
                        OutputHtmlFragment = true,
                        InputEncoding = EncodingText,
                        OutputEncoding = EncodingText
                    };
                    return converter.Convert(s);
                } catch { }

            } while (false);
            return s;
        }

        public TextPart ConvertToTextPart(string s)
        {
            do {
                if (string.IsNullOrEmpty(s))
                    break;
                try {
                    TextToHtml converter = new() {
                        OutputHtmlFragment = true,
                        InputEncoding = EncodingText,
                        OutputEncoding = EncodingText
                    };
                    return new TextPart(TextFormat.Html) { Text = converter.Convert(s) };
                } catch { }

            } while (false);
            return new TextPart(TextFormat.Plain) { Text = "---" };
        }
    }
}

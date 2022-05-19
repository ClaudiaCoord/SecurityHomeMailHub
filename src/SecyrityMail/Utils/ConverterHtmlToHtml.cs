/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using MimeKit;
using MimeKit.Text;
using HtmlAttribute = HtmlAgilityPack.HtmlAttribute;

namespace SecyrityMail.Utils
{
    public class ConverterHtmlToHtml
    {
        private static readonly char[] TrimChars = new char[] { '\r', '\n', '\t', ' ', '"' };
        private static readonly string HtmlNewLine = " \r\n";

        public ConverterHtmlToHtml(Encoding enc) =>
            EncodingText = enc;
        public ConverterHtmlToHtml() =>
            EncodingText = new UTF8Encoding(false);

        public Encoding EncodingText { get; set; } = default(Encoding);

        public string Convert(MimeMessage mmsg)
        {
            if (mmsg == default)
                return string.Empty;
            if (mmsg.HtmlBody == null)
                return (mmsg.TextBody == null) ? string.Empty : mmsg.TextBody;
            return Convert(mmsg.HtmlBody);
        }

        public string Convert(string s)
        {
            try {
                if (string.IsNullOrEmpty(s))
                    return string.Empty;

                string text = Convert_(s);
                if (string.IsNullOrEmpty(text))
                    return s;

                TextToHtml converter = new() {
                    OutputHtmlFragment = true,
                    InputEncoding = EncodingText,
                    OutputEncoding = EncodingText
                };
                return converter.Convert(text);
            } catch { return string.Empty; }
        }

        public string ConvertT(string s)
        {
            try {
                if (string.IsNullOrEmpty(s))
                    return string.Empty;
                return Convert_(s);
            } catch { return string.Empty; }
        }

        public TextPart ConvertToTextPart(MimeMessage mmsg)
        {
            try {
                string html = Convert(mmsg);
                if (!string.IsNullOrEmpty(html))
                    return new TextPart(TextFormat.Html) { Text = html };
            } catch { }
            return new TextPart(TextFormat.Plain) { Text = "---" };
        }

        public TextPart ConvertToTextPart(string s)
        {
            try {
                string html = Convert(s);
                if (!string.IsNullOrEmpty(html))
                    return new TextPart(TextFormat.Html) { Text = html };
            } catch { }
            return new TextPart(TextFormat.Plain) { Text = "---" };
        }

        private string Convert_(string s)
        {
            try {
                if (string.IsNullOrEmpty(s))
                    return string.Empty;

                string html = HttpUtility.HtmlDecode(s);
                if (string.IsNullOrEmpty(html))
                    html = s;

                try {
                    Match m = Regex.Match(html, @"<style .+?>(.+?)</style>(.*)",
                        RegexOptions.CultureInvariant |
                        RegexOptions.Singleline |
                        RegexOptions.IgnoreCase |
                        RegexOptions.Compiled);

                    if (m.Success && (m.Groups.Count == 3)) {
                        string html_ = m.Groups[2].Value;
                        if (!string.IsNullOrEmpty(html_))
                            html = html_;
                    }
                } catch { }

                StringBuilder sb = new();
                HtmlDocument doc = new();
                doc.OptionWriteEmptyNodes = true;
                doc.LoadHtml(html.Replace("<br />", HtmlNewLine)
                                 .Replace("<br/>", HtmlNewLine)
                                 .Replace("<br>", HtmlNewLine));

                if (string.IsNullOrWhiteSpace(doc.DocumentNode.InnerText))
                    return html;

                string[] ss = doc.DocumentNode.InnerText
                                 .Split(new char[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if ((ss == null) || (ss.Length == 0))
                    return doc.DocumentNode.InnerText;
                foreach (string a in ss)
                    sb.Append(string.IsNullOrWhiteSpace(a.Trim()) ? "" : $"{a}{HtmlNewLine}");

                List<Tuple<string, string>> links = GetHtmlLinks(doc, "//a");
                List<Tuple<string, string>> images = GetHtmlImages(doc, "//img");

                if (images.Count > 0)
                    foreach (Tuple<string, string> img in images)
                        sb.AppendFormat("{0}: {1}{2}",
                            string.IsNullOrWhiteSpace(img.Item2) ? "External Image" : img.Item2, img.Item1, HtmlNewLine);
                if (links.Count > 0)
                    foreach (Tuple<string, string> link in links)
                        sb.AppendFormat("{0}: {1}{2}",
                            string.IsNullOrWhiteSpace(link.Item2) ? "External Link" : link.Item2, link.Item1, HtmlNewLine);

                return sb.ToString();
            }
            catch { return string.Empty; }
        }

        static List<Tuple<string, string>> GetHtmlLinks(HtmlDocument doc, string a) {
            List<Tuple<string, string>> list = new();
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(a);
            if ((nodes == null) || (nodes.Count == 0))
                return list;

            foreach (var item in nodes) {
                HtmlAttribute ahref = item.Attributes.Where(x => x.Name == "href").FirstOrDefault();
                if ((ahref == null) || (ahref.Value == null))
                    continue;

                string desc = NormalizeDescription(item.InnerText);
                list.Add(new(NormalizeValue(ahref.Value), desc));
            }
            if (list.Count > 0)
                return list.Distinct(new ConverterComparer()).ToList();
            return list;
        }

        static List<Tuple<string, string>> GetHtmlImages(HtmlDocument doc, string a) {
            List<Tuple<string, string>> list = new();
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(a);
            if ((nodes == null) || (nodes.Count == 0))
                return list;

            foreach (var item in nodes) {
                HtmlAttribute asrc = item.Attributes.Where(x => x.Name == "src").FirstOrDefault();
                if ((asrc == null) || (asrc.Value == null))
                    continue;

                HtmlAttribute aalt = item.Attributes.Where(x => x.Name == "alt").FirstOrDefault();
                string desc = string.Empty;
                if ((aalt != null) && (aalt.Value != null)) 
                    desc = NormalizeDescription(aalt.Value);
                list.Add(new(NormalizeValue(asrc.Value), desc));
            }
            if (list.Count > 0)
                return list.Distinct(new ConverterComparer()).ToList();
            return list;
        }

        static string NormalizeValue(string s) =>
            string.IsNullOrWhiteSpace(s) ? string.Empty : s.Replace("\"", "");

        static string NormalizeDescription(string s) {
            if (!string.IsNullOrWhiteSpace(s)) {
                string desc = s.Trim(TrimChars).Replace("<", "").Replace(">", "");
                if (desc.IndexOf(@"://") != -1) {
                    try {
                        Uri uri = new(desc);
                        return uri.DnsSafeHost; } catch { }
                }
                return string.Format("{0}{1}",
                    desc.Substring(0, (desc.Length > 16) ? 16 : desc.Length), (desc.Length > 16) ? ".." : "");
            }
            return string.Empty;
        }
    }
}


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using HtmlAgilityPack;
using SecyrityMail;

namespace HomeMailHub.Version
{
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "http://www.w3.org/2005/Atom")]
    [XmlRoot(ElementName = "feed", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
    public partial class GitFeed
    {
        [XmlElement("updated")]
        public DateTime Updated { get; set; } = DateTime.MinValue;
        [XmlElement("link")]
        public List<GitLink> Link { get; set; } = new();
        [XmlElement("entry")]
        public List<GitEntry> Entry { get; set; } = new();
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "entry", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
    public partial class GitEntry
    {
        private string _Id = string.Empty;
        private string _Title = string.Empty;

        [XmlElement("updated")]
        public DateTime Updated { get; set; } = DateTime.MinValue;
        [XmlElement("link")]
        public GitLink Link { get; set; } = default;
        [XmlElement("content")]
        public GitContent Content { get; set; } = default;
        [XmlElement("title")]
        public string Title {
            get => _Title;
            set => _Title = !string.IsNullOrWhiteSpace(value) ? value.Trim() : string.Empty;
        }
        [XmlElement("id")]
        public string Id {
            get => _Id;
            set {
                if (string.IsNullOrWhiteSpace(value)) {
                    _Id = string.Empty;
                    return;
                }
                int idx = value.LastIndexOf('/');
                if (idx++ > 0)
                    _Id = value.Substring(idx, value.Length - idx);
            }
        }
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
    public partial class GitLink
    {
        [XmlAttribute("rel")]
        public string Rel { get; set; } = string.Empty;
        [XmlAttribute("type")]
        public string Types { get; set; } = string.Empty;
        [XmlAttribute("href")]
        public string Href { get; set; } = string.Empty;
    }

    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "content", Namespace = "http://www.w3.org/2005/Atom", IsNullable = false)]
    public partial class GitContent
    {
        private string _Value = string.Empty;

        [XmlAttribute("type")]
        public string Types { get; set; } = string.Empty;
        [XmlTextAttribute()]
        public string Value {
            get => _Value;
            set => _Value = ParseDescription(value);
        }

        private string ParseDescription(string src) {
            try {
                if (string.IsNullOrWhiteSpace(src))
                    return string.Empty;

                string s = HttpUtility.HtmlDecode(src).Trim();
                StringBuilder sb = new();
                HtmlDocument doc = new();
                doc.OptionWriteEmptyNodes = false;
                doc.LoadHtml(s.Replace("<br />", Environment.NewLine)
                              .Replace("<br/>", Environment.NewLine)
                              .Replace("<br>", Environment.NewLine));

                if (string.IsNullOrWhiteSpace(doc.DocumentNode.InnerText))
                    return src;

                string[] ss = doc.DocumentNode.InnerText
                                 .Split(new char[] { '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if ((ss == null) || (ss.Length == 0))
                    return doc.DocumentNode.InnerText;
                foreach (string a in ss)
                    if (!string.IsNullOrWhiteSpace(a))
                        sb.Append($"{a.Trim()}{Environment.NewLine}");
                return sb.ToString();
            } catch { }
            return src;
        }
    }

    internal class GithubFeed
    {
        private static string url = "https://github.com/ClaudiaCoord/SecurityHomeMailHub/releases.atom";

        public GitFeed Feed { get; private set; } = default;
        public bool IsEmpty => (Feed == default) || (Feed.Entry.Count == 0) || (Feed.Link.Count == 0);
        public string GetVersion => !IsEmpty ? Feed.Entry[0]?.Id : string.Empty;
        public string GetReleasesUrl => !IsEmpty ? Feed.Entry[0]?.Link.Href : string.Empty;
        public string GetDescription => !IsEmpty ? Feed.Entry[0]?.Content.Value : string.Empty;
        public bool CompareVersion(string s) {
            if (IsEmpty) return false;
            int idx = s.LastIndexOf('.');
            if (idx <= 0) return false;
            return (bool)GetVersion?.Equals(s.Substring(0, idx));
        }

        public async Task<bool> GetReleaseVersion() =>
            await Task.Run(async () => {
                try {
                    using HttpClient client = new HttpClient();
                    using Stream stream = await client.GetStreamAsync(url);
                    XmlSerializer xml = new(typeof(GitFeed));
                    if (xml.Deserialize(stream) is GitFeed val) Feed = val;
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(GetReleaseVersion), ex); }
                return !IsEmpty;
            });
    }
}

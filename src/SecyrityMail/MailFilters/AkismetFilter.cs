/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SecyrityMail.MailFilters
{
    public class AkismetFilter : ISpamFilter {

        public static AkismetFilter Create() {
            AkismetFilter filter = new ();
            if (filter.IsEnable)
                CheckApi(filter);
            return filter;
        }
        private static async void CheckApi(AkismetFilter filter) =>
            await filter.CheckApiKey().ConfigureAwait(false);

        private enum UriTypes : int {
            CommentCheck = 0,
            LearnSpam,
            LearnHam,
            KeyVerify,
            BlogUrl
        }

        private string GetTag(string s) => $"Akismet {s}";

        private readonly Uri[] _uris;
        private readonly string[] _urls = new string[] {
            "https://{0}.rest.akismet.com/1.1/comment-check",
            "https://{0}.rest.akismet.com/1.1/submit-spam",
            "https://{0}.rest.akismet.com/1.1/submit-ham",
            "https://rest.akismet.com/1.1/verify-key",
            "http://www.blogurl.com"
        };
        public string ApiKey { get; private set; }
        public bool IsEnable => Global.Instance.Config.IsSpamCheckAkismet && !string.IsNullOrWhiteSpace(ApiKey);
        public bool IsAutoLearn => Global.Instance.Config.IsAkismetLearn && !string.IsNullOrWhiteSpace(ApiKey);
        public bool IsApiKeyValid { get; private set; } = false;

        public AkismetFilter()
          : this(Global.Instance.Config.SpamCheckAkismetKey) { }

        public AkismetFilter(string apiKey) {
            ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _uris = new Uri[] {
                new Uri(string.Format(_urls[(int)UriTypes.CommentCheck], ApiKey)),
                new Uri(string.Format(_urls[(int)UriTypes.LearnSpam], ApiKey)),
                new Uri(string.Format(_urls[(int)UriTypes.LearnHam], ApiKey)),
                new Uri(_urls[(int)UriTypes.KeyVerify])
            };
        }

        public async Task<bool> CheckApiKey() {

            HttpResponseMessage hmsg = default;
            try {
                var dic = new Dictionary<string, string> {
                    {"key", HttpUtility.UrlEncode(ApiKey)},
                    {"blog", HttpUtility.UrlEncode(_urls[(int)UriTypes.BlogUrl])}
                };
                HttpClient client = Global.ClientHTTP.Value;
                var content = new FormUrlEncodedContent(dic);
                hmsg = await client.PostAsync(_uris[(int)UriTypes.KeyVerify], content)
                                   .ConfigureAwait(false);
                hmsg.EnsureSuccessStatusCode();

                string response = await hmsg.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(response)) {
#                   if DEBUG
                    Global.Instance.Log.Add(GetTag(nameof(CheckApiKey)), response);
#                   endif
                    IsApiKeyValid = "valid".Equals(response.Trim(), StringComparison.InvariantCultureIgnoreCase);
                }
                return IsApiKeyValid;
            }
#           if DEBUG
            catch (Exception ex) { Global.Instance.Log.Add(GetTag(nameof(CheckApiKey)), ex); }
#           else
            catch { }
#           endif
            finally {
                if (hmsg != null)
                    hmsg.Dispose();
            }
            return false;
        }

        public async Task LearnSpam(SpamFilterData sfd) =>
            await LearnSpam(new AkismetData(sfd)).ConfigureAwait(false);

        public async Task LearnSpam(AkismetData message) =>
            await Submit(message, _uris[(int)UriTypes.LearnSpam]).ConfigureAwait(false);

        public async Task LearnHam(SpamFilterData sfd) =>
            await LearnHam(new AkismetData(sfd)).ConfigureAwait(false);

        public async Task LearnHam(AkismetData message) =>
            await Submit(message, _uris[(int)UriTypes.LearnHam]).ConfigureAwait(false);

        public async Task<SpamType> CheckSpam(SpamFilterData sfd) {
            AkismetData message = new ();
            if (!message.Copy(sfd))
                return SpamType.UnCheck;

            bool b = await Submit(message, _uris[(int)UriTypes.CommentCheck]).ConfigureAwait(false) == "true";
            return b ? SpamType.Spam : SpamType.Ham;
        }

        private async Task<string> Submit(AkismetData message, Uri uri)
        {
            HttpResponseMessage hmsg = default;
            try {
                if (message.IsEmpty)
                    return string.Empty;

                var blog = message.Blog ?? _urls[(int)UriTypes.BlogUrl];
                var user_ip = message.UserIp ?? throw new ArgumentNullException(nameof(message.UserIp));
                var user_agent = message.UserAgent ?? throw new ArgumentNullException(nameof(message.UserAgent));
                var comment_type = message.CommentType ?? "message";
                var comment_author = message.Author ?? "";
                var comment_author_email = message.AuthorEmail ?? "";
                var comment_content = message.Content ?? "";
                var comment_date_gmt = message.DateGmt ?? "";
                var blog_charset = message.BlogCharset ?? "";
                var user_role = message.UserRole ?? "";
#               if UNUSED_API_AKISMET
                var referrer = message.Referrer ?? "";
                var permalink = message.Permalink ?? "";
                var comment_author_url = message.AuthorUrl ?? "";
                var comment_post_modified_gmt = message.PostModifiedTimeGmt ?? "";
                var blog_lang = message.BlogLang ?? "";
                var testmode = message.TestMode ?? "";
#               endif

                var parameters = new Dictionary<string, string> {
                    {"blog", HttpUtility.UrlEncode(blog)},
                    {"user_ip", HttpUtility.UrlEncode(user_ip)},
                    {"user_agent", HttpUtility.UrlEncode(user_agent)},
                    {"comment_type", HttpUtility.UrlEncode(comment_type)},
                    {"comment_author", HttpUtility.UrlEncode(comment_author)},
                    {"comment_author_email", HttpUtility.UrlEncode(comment_author_email)},
                    {"comment_content", HttpUtility.UrlEncode(comment_content)},
                    {"comment_date_gmt", HttpUtility.UrlEncode(comment_date_gmt)},
                    {"blog_charset", HttpUtility.UrlEncode(blog_charset)},
                    {"user_role", HttpUtility.UrlEncode(user_role)},
#                   if UNUSED_API_AKISMET
                    {"referrer", HttpUtility.UrlEncode(referrer)},
                    {"permalink", HttpUtility.UrlEncode(permalink)},
                    {"comment_author_url", HttpUtility.UrlEncode(comment_author_url)},
                    {"comment_post_modified_gmt", HttpUtility.UrlEncode(comment_post_modified_gmt)},
                    {"blog_lang", HttpUtility.UrlEncode(blog_lang)},
                    {"is_test", HttpUtility.UrlEncode(testmode)}
#                   endif
                };
                HttpClient client = Global.ClientHTTP.Value;
                FormUrlEncodedContent content = new(parameters);
                hmsg = await client.PostAsync(uri, content).ConfigureAwait(false);
                hmsg.EnsureSuccessStatusCode();
                string response = await hmsg.Content.ReadAsStringAsync().ConfigureAwait(false);
#               if DEBUG
                Global.Instance.Log.Add(GetTag(nameof(Submit)), $"Check message is spam: {response}");
#               endif
                return response;
            }
#           if DEBUG
            catch (Exception ex) { Global.Instance.Log.Add(GetTag(nameof(Submit)), ex); }
#           else
            catch { }
#           endif
            finally {
                if (hmsg != null)
                    hmsg.Dispose();
            }
            return string.Empty;
        }
    }
}


using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MimeKit;

namespace SecyrityMail.Servers
{
    internal static class ServerExtension
    {
        public static bool CheckDateTime(this DateTime dt) =>
            (dt != default) && (dt != DateTime.MinValue);

        public static bool CheckDateTimeOffset(this DateTimeOffset dt) =>
            (dt != default) && (dt != DateTimeOffset.MinValue);

        public static string EncodeMD5B64(this string token)
        {
            MD5CryptoServiceProvider crypt = default;
            try {
                crypt = new();
                return crypt.ComputeHash(UTF8Encoding.UTF8.GetBytes(token))
                    .Select<byte, string>(a => a.ToString("x2"))
                    .Aggregate<string>((a, b) => string.Format("{0}{1}", a, b));
            }
            finally {
                if (crypt != default)
                    crypt.Dispose();
            }
        }

        public static string HashHMACMD5(this string b64, string key)
        {
            HMACMD5 hmac = default;
            try {
                hmac = new(Encoding.UTF8.GetBytes(key));
                return hmac.ComputeHash(Convert.FromBase64String(b64))
                    .Select<byte, string>(a => a.ToString("x2"))
                    .Aggregate<string>((a, b) => string.Format("{0}{1}", a, b));
            }
            finally {
                if (hmac != default)
                    hmac.Dispose();
            }
        }

        public static async Task<string> CheckMessageId(this FileInfo f) =>
            await Task<bool>.Run(async () => {
                try {
                    MimeMessage msg = MimeMessage.Load(f.FullName);
                    string s = msg.MessageId;
                    if (string.IsNullOrWhiteSpace(s))
                    {
                        string domain = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                        s = $"{Guid.NewGuid().ToString().Replace("-", "")}@{domain}";
                        msg.MessageId = s;
                        await msg.WriteToAsync(f.FullName).ConfigureAwait(false);
                    }
                    try { msg.Dispose(); } catch { }
                    return s;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(CheckMessageId), ex); }
                return string.Empty;
            });
    }
}

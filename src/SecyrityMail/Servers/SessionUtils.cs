
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SecyrityMail.Messages;
using SecyrityMail.Servers.POP3;

namespace SecyrityMail.Servers
{
    internal static class SessionUtils
    {
        private static string[] rules = new string[]
        {
            @"^(\w+)\s([-+=@.\[\]\w]+)\s(.+)\r?\n?$|^(\w+)\s(.+)\r?\n?$|^([-+=@.\[\]\w]+)\r?\n?$",
            @"^([\w]+)\s([-\w]+):\s?<(.*)>\sORCPT=rfc822;(.*)|^([\w]+)\s([-\w]+):\s?<(.*)>|^(\w+)\s([-+=@.\[\]\w]+)\s(.+)|^(\w+)\s([-+=@.\[\]\w]+)|^([-+=@.\[\]\w]+)"
        };
        internal static string [] ParsePop3Command(this string s) => ParseCommandRequest(s, rules[0]);
        internal static string [] ParseSmtpCommand(this string s) => ParseCommandRequest(s, rules[1]);
        internal static string [] ParseCommandRequest(this string s, string r)
        {
            try {
                Match m = Regex.Match(s, r,
                    RegexOptions.CultureInvariant |
                    RegexOptions.Singleline |
                    RegexOptions.IgnoreCase |
                    RegexOptions.Compiled);

                if ((!m.Success) && (m.Groups.Count > 0))
                    return default;

                int aidx = 0,
                    cidx = m.Groups.Count;
                string[] ss = new string[(cidx - 1)];
                for (int i = 1; i < cidx; i++) {
                    if (string.IsNullOrWhiteSpace(m.Groups[i].Value))
                        continue;
                    ss[aidx++] = m.Groups[i].Value.Trim();
                }
                Array.Resize(ref ss, aidx);
                return ss;
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(ParseCommandRequest), ex); }
            return default;
        }

        internal static int ParseIntGetArg(this string[] args, int index)
        {
            do {
                if (args.Length < index)
                    break;
                if (string.IsNullOrWhiteSpace(args[index - 1]))
                    break;
                if (!int.TryParse(args[index - 1], out int n))
                    break;
                if (n < 0)
                    break;
                return n;
            } while (false);
            return -1;
        }

        internal static string ParseStringGetArg(this string[] args, int index)
        {
            do {
                if (args.Length < index)
                    break;
                if (string.IsNullOrWhiteSpace(args[index - 1]))
                    break;
                return args[index - 1].Trim();
            } while (false);
            return string.Empty;
        }

        internal static async Task<bool> RunCommand(
            this StreamSession stream, Func<StreamSession, MailMessages, string[], FileStream, Task<bool>> func, MailMessages data, string[] scmd, Func<string> ferr, FileStream fslog)
        {
            try {
                bool b = await func.Invoke(stream, data, scmd, fslog);
                if (b) return true;

                await stream.SendClient(ferr.Invoke(), fslog)
                      .ConfigureAwait(false);
            }
            catch (Exception ex) {
                await stream.SendClient(Pop3ResponseId.ErrorArgs.Pop3Response(ex.Message), fslog)
                      .ConfigureAwait(false);
            }
            return false;
        }

        internal static async Task SendClient(
            this StreamSession stream, string response) =>
            await SendClient(stream, response, false, default);

        internal static async Task SendClient(
            this StreamSession stream, string response, FileStream fslog) =>
            await SendClient(stream, response, false, fslog);

        internal static async Task SendClient(
            this StreamSession stream, string response, bool isendtransfer, FileStream fslog = default)
        {
#           if DEBUG_PRINT
            Global.Instance.Log.Add($"{nameof(SendClient)} {isendtransfer}/{stream.IsEnable}/{stream.IsSecure} -> {response}");
#           endif

            if (!stream.IsEnable)
                return;

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (fslog != default)
                    await fslog.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (isendtransfer)
                    await stream.SendClient(Pop3ResponseId.EndTransfer.Pop3Response(), fslog).ConfigureAwait(false);
            } catch { }
        }
    }
}

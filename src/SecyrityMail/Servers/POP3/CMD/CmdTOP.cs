
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SecyrityMail.Messages;

#if TOP_HEADERS_ONLY
using MimeKit;
#endif

namespace SecyrityMail.Servers.POP3.CMD
{
    internal static class CmdTOP
    {
        internal static async Task<bool> Function(StreamSession stream, MailMessages data, string[] scmd, FileStream fslog) =>
            await Task<bool>.Run(async () => {
                do {
                    if (data.Count == 0)
                        break;
                    int n, l;
                    if ((n = scmd.ParseIntGetArg(2)) <= 0)
                        break;
                    if ((l = scmd.ParseIntGetArg(3)) < 0)
                        break;

                    MailMessage msg = data.Get(n);
                    if (msg == default)
                        break;

                    StringBuilder sb = new();
                    try {
#                       if TOP_HEADERS_ONLY
                        MimeMessage mmsg = MimeMessage.Load(msg.FilePath);
                        for (int i = 0; i < mmsg.Headers.Count; i++) {
                            if ((l > 0) && (l <= i))
                                break;
                            sb.Append($"{mmsg.Headers[i]}\r\n");
                        }
                        mmsg.Dispose();
#                       else
                        string[] ss = File.ReadAllLines(msg.FilePath);
                        if ((ss == null) || (ss.Length == 0))
                            break;
                        for (int i = 0; i < ss.Length; i++) {
                            if ((l > 0) && (l <= i))
                                break;
                            sb.Append($"{ss[i]}\r\n");
                        }
#                       endif
                    } catch { break; }
                    if (sb.Length == 0)
                        break;

                    await stream.SendClient(Pop3ResponseId.TopArgs.Pop3Response($"{n}/{l}"), fslog)
                          .ConfigureAwait(false);
                    await stream.SendClient(sb.ToString().Trim(), true, fslog)
                          .ConfigureAwait(false);
                    return true;

                } while (false);
                return false;
            });
    }
}

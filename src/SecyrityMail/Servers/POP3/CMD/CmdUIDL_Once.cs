
using System.IO;
using System.Threading.Tasks;
using SecyrityMail.Messages;

namespace SecyrityMail.Servers.POP3.CMD
{
    internal static class CmdUIDL_One
    {
        internal static async Task<bool> Function(StreamSession stream, MailMessages data, string[] scmd, FileStream fslog) =>
            await Task<bool>.Run(async () => {
                do {
                    if (data.Count == 0)
                        break;

                    int n;
                    if ((n = scmd.ParseIntGetArg(2)) <= 0)
                        break;

                    MailMessage msg = data.Get(n);
                    if (msg == default)
                        break;

                    if (!string.IsNullOrWhiteSpace(msg.MsgId))
                        await stream.SendClient(Pop3ResponseId.OkArgs.Pop3Response($"{msg.Id} {msg.MsgId}"), fslog)
                              .ConfigureAwait(false);
                    return true;

                } while (false);
                return false;
            });
    }
}


using System.IO;
using System.Threading.Tasks;
using SecyrityMail.Messages;

namespace SecyrityMail.Servers.POP3.CMD
{
    internal static class CmdDELE
    {
        internal static async Task<bool> Function(StreamSession stream, MailMessages data, string[] scmd, FileStream fslog) =>
            await Task<bool>.Run(async () => {
                do {
                    if (data.Count == 0)
                        break;

                    int n;
                    if ((n = scmd.ParseIntGetArg(2)) <= 0)
                        break;

                    await data.DeleteMessage(n).ConfigureAwait(false);
                    await stream.SendClient(Pop3ResponseId.DeleteArgs.Pop3Response(n.ToString()), fslog)
                          .ConfigureAwait(false);
                    return true;

                } while (false);
                return false;
            });
    }
}

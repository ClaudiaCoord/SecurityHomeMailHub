/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System.IO;
using System.Text;
using System.Threading.Tasks;
using SecyrityMail.Messages;

namespace SecyrityMail.Servers.POP3.CMD
{
    internal static class CmdLIST_All
    {
        internal static async Task<bool> Function(StreamSession stream, MailMessages data, string[] scmd, FileStream fslog) =>
            await Task<bool>.Run(async () => {
                if (data.Count == 0)
                {
                    await stream.SendClient(Pop3ResponseId.EndTransfer.Pop3Response(), fslog)
                          .ConfigureAwait(false);
                    return false;
                }
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < data.Count; i++)
                {
                    MailMessage msg = data.Get(i + 1);
                    if (msg == default)
                        continue;
                    sb.AppendFormat("{0} {1}{2}", msg.Id, msg.Size, (i == data.Count - 1) ? "\r\n.\r\n" : "\r\n");
                }
                await stream.SendClient(sb.ToString(), fslog)
                      .ConfigureAwait(false);
                return true;
            });
    }
}

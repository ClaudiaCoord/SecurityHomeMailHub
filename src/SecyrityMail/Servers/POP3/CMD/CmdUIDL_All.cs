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
    internal static class CmdUIDL_All
    {
        internal static async Task<bool> Function(StreamSession stream, MailMessages data, string[] scmd, FileStream fslog) =>
            await Task<bool>.Run(async () => {
                do {
                    if (data.Count == 0)
                        break;

                    StringBuilder sb = new();
                    for (int i = 0; i < data.Count; i++) {

                        MailMessage msg = data[i];
                        if (msg == default)
                            continue;

                        if (!string.IsNullOrWhiteSpace(msg.MsgId))
                            sb.AppendFormat("{0} {1}{2}", msg.Id, msg.MsgId, (i == data.Count - 1) ? "\r\n.\r\n" : "\r\n");
                    }
                    if (sb.Length == 0)
                        break;

                    await stream.SendClient(Pop3ResponseId.Ok.Pop3Response(), fslog)
                          .ConfigureAwait(false);
                    await stream.SendClient(sb.ToString(), fslog)
                          .ConfigureAwait(false);
                    return true;

                } while (false);
                return false;
            });
    }
}

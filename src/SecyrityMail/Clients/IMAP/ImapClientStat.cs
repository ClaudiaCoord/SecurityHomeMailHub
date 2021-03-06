/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using SecyrityMail.Data;

namespace SecyrityMail.Clients.IMAP
{
    public class ImapClientStat : MailEvent, IMailEventProxy
    {
        public int ImapLastMessageReceive { get; set; } = 0;
        public int ImapLastMessageRecent { get; set; } = 0;
        public int ImapLastMessageDelete { get; set; } = 0;

        public void Reset() {
            ImapLastMessageReceive =
            ImapLastMessageRecent =
            ImapLastMessageDelete = 0;
        }
    }
}

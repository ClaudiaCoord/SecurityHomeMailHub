/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using SecyrityMail.Data;

namespace SecyrityMail.Clients.SMTP
{
    public class SmtpClientStat : MailEvent, IMailEventProxy
    {
        public int SmtpLastMessageTotal { get; set; } = 0;
        public int SmtpLastMessageSend { get; set; } = 0;

        public void Reset()
        {
            SmtpLastMessageTotal =
            SmtpLastMessageSend = 0;
        }
    }
}

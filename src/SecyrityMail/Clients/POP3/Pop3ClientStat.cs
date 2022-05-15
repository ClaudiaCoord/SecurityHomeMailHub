
using SecyrityMail.Data;

namespace SecyrityMail.Clients.POP3
{
    public class Pop3ClientStat : MailEvent, IMailEventProxy
    {
        public int Pop3LastMessageTotal { get; set; } = 0;
        public int Pop3LastMessageReceive { get; set; } = 0;
        public int Pop3LastMessageDelete { get; set; } = 0;

        public void Reset() {
            Pop3LastMessageTotal =
            Pop3LastMessageReceive =
            Pop3LastMessageDelete = 0;
        }
    }
}

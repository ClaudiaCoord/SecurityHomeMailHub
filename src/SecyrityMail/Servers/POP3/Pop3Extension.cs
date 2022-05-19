/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


namespace SecyrityMail.Servers.POP3
{
    public enum Pop3ResponseId : int
    {
        EndTransfer = 0,
        ReadyArgs = 1,
        Ok = 2,
        Error = 3,
        LogOut = 4,
        Reset = 5,
        Capa = 6,
        OkArgs = 7,
        AcceptedArgs = 8,
        OctetsArgs = 9,
        DeleteArgs = 10,
        TopArgs = 11,
        HelpArgs = 12,
        StatMsgArgs = 13,
        StatMsgExtArgs = 14,
        ErrorArgs = 15,
        NoMessageArgs = 16,
        NoAccount = 17,
        WrongAccount = 18,
        BadCommand = 19,
        NotSupport = 20,
        AlreadyLogged = 21,
        NeededLogged = 22,
        StorageWait = 23,
        StorageError = 24,
        StartTls = 25,
        TlsAlready = 26,
        AuthCap = 27,
        IpAccessDenied = 28,
        Hello = ReadyArgs
    }

    internal static class Pop3Extension
    {
        private static readonly string[] responses = new string[]
        {
            /* 0 */  "\r\n.\r\n",
            /* 1 */  "POP3 service ready",
            /* 2 */  "+OK",
            /* 3 */  "-ERR",
            /* 4 */  "bye",
            /* 5 */  "reset user state",
            /* 6 */  "+OK Capability list follows \r\nTOP\r\nUSER\r\nUIDL\r\nAPOP\r\nSTLS\r\nSTARTTLS\r\nSASL PLAIN LOGIN USER APOP\r\nEXPIRE 60\r\nLOGIN-DELAY 900\r\n.",
            /* 7 */  "{0}",
            /* 8 */  "{0} accepted",
            /* 9 */  "{0} octets",
            /* 10 */ "message {0} deleted",
            /* 11 */ "top of {0} message follows",
            /* 12 */ "valid commands: {0}",
            /* 13 */ "{0} {1} {2}",
            /* 14 */ "{0} {1} messages ({2} bytes)",
            /* 15 */ "{0}",
            /* 16 */ "no such message, only {0} messages in mailbox\r\n.",
            /* 17 */ "sorry, no such account here",
            /* 18 */ "wrong login/password",
            /* 19 */ "bad command",
            /* 20 */ "not support command",
            /* 21 */ "already logged",
            /* 22 */ "needed logging",
            /* 23 */ "storage wait ready, try again later",
            /* 24 */ "storage internal error, try again later",
            /* 25 */ "+OK Begin TLS negotiation",
            /* 26 */ "-ERR TLS already active",
            /* 27 */ "+OK \r\nUSER\r\nPLAIN\r\nAPOP\r\n.",
            /* 28 */ "-ERR ip range access denied"
        };

        internal static string Pop3Response(this Pop3ResponseId i, string s = default, string ss = default) {
            int idx = (int)i;
            switch (i) {
                case Pop3ResponseId.EndTransfer: return responses[0];
                case Pop3ResponseId.ReadyArgs: return string.Format("{0} {1} <{2}.local>\r\n", responses[2], responses[1], s);

                case Pop3ResponseId.Ok:
                case Pop3ResponseId.Capa:
                case Pop3ResponseId.Error:
                case Pop3ResponseId.AuthCap:
                case Pop3ResponseId.StartTls:
                case Pop3ResponseId.TlsAlready: return $"{responses[idx]}\r\n";

                case Pop3ResponseId.Reset:
                case Pop3ResponseId.LogOut:
                case Pop3ResponseId.StorageWait:
                case Pop3ResponseId.StorageError: return string.Format("{0} {1}\r\n", responses[2], responses[idx]);

                case Pop3ResponseId.OkArgs:
                case Pop3ResponseId.TopArgs:
                case Pop3ResponseId.DeleteArgs:
                case Pop3ResponseId.AcceptedArgs:
                case Pop3ResponseId.NoMessageArgs:
                case Pop3ResponseId.HelpArgs: return string.Format($"{responses[2]} {responses[idx]}\r\n", s);

                case Pop3ResponseId.StatMsgArgs:
                case Pop3ResponseId.StatMsgExtArgs: return string.Format($"{responses[idx]}\r\n", responses[2], s, ss);

                default: return (s == default) ?
                        string.Format("{0} {1}\r\n", responses[3], responses[idx]) :
                        string.Format($"{responses[3]} {responses[idx]}\r\n", responses[3], s);
            }
        }
    }
}

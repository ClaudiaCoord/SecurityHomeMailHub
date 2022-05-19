/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;

namespace SecyrityMail.Servers.SMTP
{
    public enum SmtpResponseId : int
    {
        EndTransfer = 0,
        Ok = 1,
        Error = 2,
        Begin = 3,
        Hello = 4,
        EHello = 5,
        LogOut = 6,
        DataBegin = 7,
        DataEndArgs = 8,
        BadCmdArgs = 9,
        NotParamArgs = 10,
        NotSupport = 11,
        NotSupportArgs = 12,
        NotMailbox = 13,
        BadMailbox = 14,
        OkArgs = 15,
        ErrorArgs = 16,
        HelpArgs = 17,
        AuthErrorArgs = 18,
        AuthUserArgs = 19,
        AuthUser = 20,
        AuthPassword = 21,
        AuthOk = 22,
        AlreadyLogged = 23,
        NeededLogged = 24,
        StartTls = 25,
        TlsAlready = 26,
        SenderErrorFrom = 27,
        IpAccessDenied = 28
    }

    internal static class SmtpExtension
    {
        private static readonly string[] responses = new string[]
        {
            /* 0 */  "\r\n.\r\n",
            /* 1 */  "250 OK",
            /* 2 */  "451 ERR",
            /* 3 */  "220 OK <{0}.local>",
            /* 4 */  "250 OK {0}.local is ready",
            /* 5 */  "250-{0}.local Hello {1} \r\n250-8BITMIME \r\n250-STARTTLS \r\n250-SMTPUTF8 \r\n250-AUTH LOGIN PLAIN CRAM-MD5 \r\n250 HELO",
            /* 6 */  "221 OK {0}.local is closing transmission channel",
            /* 7 */  "354 Start mail input; end with <CRLF>.<CRLF>",
            /* 8 */  "250 {0} bytes, message accepted for delivery",
            /* 9 */  "500 {0}",
            /* 10 */ "501 {0}",
            /* 11 */ "502 not support command",
            /* 12 */ "504 not support arguments: {0}",
            /* 13 */ "550 not available requestted mailbox",
            /* 14 */ "553 bad mailbox name",
            /* 15 */ "250 OK {0}",
            /* 16 */ "451 Error {0}",
            /* 17 */ "214 available command: {0}",
            /* 18 */ "535 5.7.8 Error, authentication failed: {0}",
            /* 19 */ "334 {0}",
            /* 20 */ "334 VXNlcm5hbWU6",
            /* 21 */ "334 UGFzc3dvcmQ6",
            /* 22 */ "235 2.7.0 Authentication successful",
            /* 23 */ "252 already logged",
            /* 24 */ "530 needed logging",
            /* 25 */ "220 2.0.0 Ready to start TLS",
            /* 26 */ "451 TLS already active",
            /* 27 */ "521 sender not equals session From address",
            /* 28 */ "530 ip range access denied"
        };

        internal static string SmtpResponse(this SmtpResponseId i, string s = default, string ss = default)
        {
            int idx = (int)i;
            switch (i)
            {
                case SmtpResponseId.EndTransfer: return responses[0];
                case SmtpResponseId.Ok:
                case SmtpResponseId.Error:
                case SmtpResponseId.AuthOk:
                case SmtpResponseId.StartTls:
                case SmtpResponseId.AuthUser:
                case SmtpResponseId.DataBegin:
                case SmtpResponseId.NotSupport:
                case SmtpResponseId.NotMailbox:
                case SmtpResponseId.BadMailbox:
                case SmtpResponseId.TlsAlready:
                case SmtpResponseId.AuthPassword:
                case SmtpResponseId.NeededLogged:
                case SmtpResponseId.AlreadyLogged:
                case SmtpResponseId.IpAccessDenied:
                case SmtpResponseId.SenderErrorFrom: return $"{responses[idx]}\r\n";
                case SmtpResponseId.Begin:
                case SmtpResponseId.Hello:
                case SmtpResponseId.LogOut: return string.Format(responses[idx] + "\r\n", Environment.MachineName);
                case SmtpResponseId.OkArgs:
                case SmtpResponseId.HelpArgs:
                case SmtpResponseId.ErrorArgs:
                case SmtpResponseId.BadCmdArgs:
                case SmtpResponseId.DataEndArgs:
                case SmtpResponseId.NotParamArgs:
                case SmtpResponseId.AuthUserArgs:
                case SmtpResponseId.AuthErrorArgs:
                case SmtpResponseId.NotSupportArgs: return string.Format(responses[idx] + "\r\n", s);
                case SmtpResponseId.EHello: return string.Format(responses[idx] + "\r\n", Environment.MachineName, s);
                default: return string.Empty;
            }
        }
    }
}

#region Doc
/*
    HELO <SP> <domain> <CRLF>
    MAIL <SP> FROM:<reverse-path> <CRLF> 
    RCPT <SP> TO:<forward-path> <CRLF> 
    DATA <CRLF>
    RSET <CRLF> 
    SEND <SP> FROM:<reverse-path> <CRLF> 
    SOML <SP> FROM:<reverse-path> <CRLF> 
    SAML <SP> FROM:<reverse-path> <CRLF> 
    VRFY <SP> <string> <CRLF> 
    EXPN <SP> <string> <CRLF> 
    HELP <SP> <string> <CRLF> 
    NOOP <CRLF> 
    QUIT <CRLF>

    mailbox_unavailable_no_mail_action = 450,
    mailbox_unavailable_no_action = 550,
    mailbox_name_bad = 553,
    usr_not_local = 551, 
    no_mail_accept = 521,
    access_denied = 530,

    nonstandard_success = 200,
    sys_status = 211,
    help = 214,
    service_ready = 220,
    service_closing = 221,
    action_ok = 250,
    will_forward = 251,
    cannot_verify_but_ok = 252,
    start_mail = 354,
    service_not_available = 421,
    mailbox_unavailable_no_mail_action = 450,
    local_error = 451,
    insufficient_storage = 452,
    bad_command = 500,
    params_error = 501,
    command_not_implemented = 502,
    bad_sequence = 503,
    param_not_implemented = 504,
    no_mail_accept = 521,
    access_denied = 530,
    mailbox_unavailable_no_action = 550,
    usr_not_local = 551, 
    exceeded_storage_allocation = 552,
    mailbox_name_bad = 553,
    transaction_failed = 554
 */
#endregion
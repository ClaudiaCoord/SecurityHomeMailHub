
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MimeKit;
using SecyrityMail.Data;
using SecyrityMail.Utils;

namespace SecyrityMail.Servers.SMTP
{
    public enum SmtpCommand : int
    {
        HELO,
        EHLO,
        MAIL,
        RCPT,
        DATA,
        RSET,
        SEND,
        SOML,
        SAML,
        VRFY,
        EXPN,
        HELP,
        NOOP,
        QUIT,
        AUTH,
        PLAIN,
        LOGIN,
        CRAMMD5,
        DIGESTMD5,
        STARTTLS,
        FROM = MAIL,
        TO = RCPT,
        HLO = EHLO
    }

    class SmtpSession : MailEvent, IDisposable
    {
        StreamSession stream;
        TokenSafe token;
        FileStream fslog = default;
        StringBuilder sbmsg = new StringBuilder();
        CredentialsData data = new();

        private bool IsData { get; set; } = false;
        private long SizeData { get; set; } = 0L;
        private SmtpCommand LastCommand { get; set; } = SmtpCommand.NOOP;
        private Action<MailEvent> UnsubsribeEvent;
        private Action<EndPoint>  AddAuthFilter;

        public double ClientIdle { get; set; } = 15.0;
        public bool IsSecure => stream.IsSecure;
        public bool IsLog { get; set; } = false;
        public bool IsDeliveryLocal { get; set; } = false;
        public EndPoint IpEndPoint => (stream == default) ? default : stream.IpEndPoint;
        public Stream Stream => stream;

        public SmtpSession(TcpClient tcpClient, Action<MailEvent> unsubsribe, Action<EndPoint> afilter, TokenSafe t) {
            UnsubsribeEvent = unsubsribe;
            AddAuthFilter = afilter;
            Init(t, tcpClient, false, false, string.Empty);
        }
        public SmtpSession(TcpClient tcpClient, Action<MailEvent> unsubsribe, Action<EndPoint> afilter, TokenSafe t, bool isSecure, bool islog, string logpath) {
            UnsubsribeEvent = unsubsribe;
            AddAuthFilter = afilter;
            Init(t, tcpClient, isSecure, islog, logpath);
        }
        ~SmtpSession() => DisposeFinal();

        private void Init(TokenSafe t, TcpClient tcpClient, bool isSecure, bool islog, string logpath)
        {
            token = t;
            IsLog = islog;

            if ((tcpClient == null) || !tcpClient.Connected)
                throw new Exception(nameof(TcpClient));
            if (IsLog)
                fslog = new FileStream(
                    Path.Combine(logpath,
                        $"{nameof(SmtpSession)}-{tcpClient.Client.RemoteEndPoint.ToString().Replace(':', '-')}.log"),
                    FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, 4096, true);
            stream = new StreamSession(tcpClient, isSecure);
        }

        public void Dispose() => stream?.Dispose();
        public void DisposeFinal()
        {
            stream?.Dispose();
            FileStream fs = fslog;
            fslog = default;
            if (fs != default) {
                try { fs.Close(); } catch { }
                try { fs.Dispose(); } catch { }
            }
        }

        #region New Session
        public async void NewSession()
        {
            try {
                if (!stream.IsEnable)
                    return;

                OnCallEvent(MailEventId.BeginCall, "SMTP-IN");
                await stream.SendClient(SmtpResponseId.Begin.SmtpResponse(), fslog)
                      .ConfigureAwait(false);

                DateTime dt = default;
                byte[] buffer = new byte[2048];
                while (true) {

                    if (!stream.IsEnable)
                        break;

                    token.ThrowIfCancellationRequested();
                    await Task.Delay(150).ConfigureAwait(false);

                    int count;
                    StringBuilder sb = new StringBuilder();
                    while (stream.IsDataAvailable) {
                        count = stream.Read(buffer, 0, buffer.Length);
                        if (count > 0)
                        {

                            sb.Append(Encoding.UTF8.GetString(buffer, 0, count));
                            if (IsLog)
                                await fslog.WriteAsync(buffer, 0, count).ConfigureAwait(false);
                        }
                        else if (stream.IsDataAvailable) continue;
                        else if (count < 0) break;
                    }
                    if (sb.Length > 0) {
                        if (IsData) {
                            do {
                                if (sb.Length == 0)
                                    break;

                                SizeData += sb.Length;
                                string stmp = sb.ToString();
                                int idx = stmp.IndexOf(SmtpResponseId.EndTransfer.SmtpResponse());
                                if (idx == -1) {
                                    sbmsg.Append(stmp);
                                    break;
                                }
                                sbmsg.Append(stmp.Substring(0, idx));
                                try {
                                    using MemoryStream ms = new(Encoding.UTF8.GetBytes(sbmsg.ToString()));
                                    MimeMessage mmsg = await MimeMessage.LoadAsync(ms).ConfigureAwait(false);
                                    await MessageStore(mmsg).ConfigureAwait(false);
                                }
                                catch (Exception ex) { Global.Instance.Log.Add(nameof(NewSession), ex); }

                                await stream.SendClient(SmtpResponseId.DataEndArgs.SmtpResponse(SizeData.ToString()), fslog)
                                      .ConfigureAwait(false);

                                SizeData = 0L;
                                IsData = false;
                                sbmsg.Clear();

                            } while (false);
                        } else {
                            if (sb.Length > 0)
                                ParseCommand(sb.ToString());
                        }
                        dt = DateTime.Now.AddSeconds(ClientIdle);
                    } else {
                        if (dt.CheckDateTime() && (dt < DateTime.Now))
                            break;
                        await Task.Delay(150).ConfigureAwait(false);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                await stream.SendClient(SmtpResponseId.Error.SmtpResponse(), fslog)
                      .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await stream.SendClient(SmtpResponseId.Error.SmtpResponse(), fslog)
                      .ConfigureAwait(false);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(SmtpSession), ex); }
            finally
            {
                OnCallEvent(MailEventId.EndCall, "SMTP-IN");
                if (IsLog)
                    await fslog?.FlushAsync();

                UnsubsribeEvent.Invoke(this);
                UnsubsribeEvent = (a) => { };
                Dispose();
                data = default;
                GC.Collect();
            }
        }
        #endregion

        #region Parse Command
        private async void ParseCommand(string request)
        {
#           if DEBUG_PRINT
            Global.Instance.Log.Add($"{nameof(SmtpSession)}:{nameof(ParseCommand)}(1) -> {request}");
#           endif
            if (string.IsNullOrWhiteSpace(request)) {
                await stream.SendClient(SmtpResponseId.Error.SmtpResponse(), fslog)
                      .ConfigureAwait(false);
                return;
            }
            string[] scmd = request.ParseSmtpCommand();
            if ((scmd == default) || (scmd.Length == 0)) {
                await stream.SendClient(SmtpResponseId.Error.SmtpResponse(), fslog)
                      .ConfigureAwait(false);
                return;
            }
            if (!Enum.TryParse(scmd[0], true, out SmtpCommand cmd)) {
                switch (LastCommand) {
                    case SmtpCommand.LOGIN:
                    case SmtpCommand.PLAIN:
                    case SmtpCommand.CRAMMD5: cmd = LastCommand; break;
                    default: {
                            await stream.SendClient(SmtpResponseId.NotSupport.SmtpResponse(), fslog)
                                        .ConfigureAwait(false);
                            return;
                        }
                }
            }
            switch (cmd) {
                case SmtpCommand.HELP:
                case SmtpCommand.HELO:
                case SmtpCommand.EHLO:
                case SmtpCommand.NOOP:
                case SmtpCommand.QUIT:
                case SmtpCommand.STARTTLS: break;
                case SmtpCommand.AUTH:
                case SmtpCommand.LOGIN:
                case SmtpCommand.PLAIN:
                case SmtpCommand.CRAMMD5: {
                        if (data.IsAuthorize) {
                            await stream.SendClient(SmtpResponseId.AlreadyLogged.SmtpResponse(), fslog)
                                  .ConfigureAwait(false);
                            return;
                        }
                        break;
                    }
                case SmtpCommand.TO:
                case SmtpCommand.FROM:
                case SmtpCommand.DATA: {
                        if (IsDeliveryLocal)
                            break;
                        if (!data.IsAuthorize) {
                            await stream.SendClient(SmtpResponseId.NeededLogged.SmtpResponse(), fslog)
                                  .ConfigureAwait(false);
                            return;
                        }
                        LastCommand = SmtpCommand.NOOP;
                        break;
                    }
                default: {
                        if (!data.IsAuthorize) {
                            await stream.SendClient(SmtpResponseId.NeededLogged.SmtpResponse(), fslog)
                                  .ConfigureAwait(false);
                            return;
                        }
                        LastCommand = SmtpCommand.NOOP;
                        break;
                    }
            }
            await ParseCommand_(cmd, scmd).ConfigureAwait(false);
        }
        private async Task ParseCommand_(SmtpCommand cmd, string[] scmd)
        {
#           if DEBUG_PRINT
            Global.Instance.Log.Add($"{nameof(SmtpSession)}:{nameof(ParseCommand)}(2) -> {data.IsAuthorize}/{cmd}/{scmd.Length} = {string.Join(",", scmd)}");
#           endif
            switch (cmd)
            {
                case SmtpCommand.STARTTLS:
                    {
                        if (stream.IsSecure) {
                            await stream.SendClient(SmtpResponseId.TlsAlready.SmtpResponse(), fslog)
                                        .ConfigureAwait(false);
                            break;
                        }
                        await stream.SendClient(SmtpResponseId.StartTls.SmtpResponse(), fslog)
                                    .ConfigureAwait(false);
                        await stream.StartTls().ConfigureAwait(false);
                        OnCallEvent(MailEventId.StartTls, (IpEndPoint == default) ? "no IP address!" : IpEndPoint.ToString(), IpEndPoint);
                        break;
                    }
                case SmtpCommand.CRAMMD5:
                    {
                        try {
                            if ((scmd.Length == 1) && !string.IsNullOrEmpty(scmd[0])) {

                                if (data.CRAMMD5Credentials(scmd[0])) {
                                    await stream.SendClient(SmtpResponseId.AuthOk.SmtpResponse(), fslog)
                                          .ConfigureAwait(false);
                                    LastCommand = SmtpCommand.NOOP;
                                    AuthToEvent();
                                    break;
                                }
                            }
                            await stream.SendClient(SmtpResponseId.AuthErrorArgs.SmtpResponse(cmd.ToString()), fslog)
                                  .ConfigureAwait(false);
                            AddAuthFilter.Invoke(IpEndPoint);
                        }
                        catch (Exception ex) {
                            await stream.SendClient(SmtpResponseId.ErrorArgs.SmtpResponse(ex.Message), fslog)
                                  .ConfigureAwait(false);
                        }
                        LastCommand = SmtpCommand.NOOP;
                        break;
                    }
                case SmtpCommand.PLAIN:
                    {
                        try {
                            if (scmd.Length == 1) {
                                if ((!data.IsAuthorize) && data.PlainCredentials(scmd[0])) {
                                    await stream.SendClient(SmtpResponseId.AuthOk.SmtpResponse(), fslog)
                                          .ConfigureAwait(false);
                                    LastCommand = SmtpCommand.NOOP;
                                    AuthToEvent();
                                    break;
                                }
                            }
                            await stream.SendClient(SmtpResponseId.AuthErrorArgs.SmtpResponse(cmd.ToString()), fslog)
                                  .ConfigureAwait(false);
                            AddAuthFilter.Invoke(IpEndPoint);
                        }
                        catch (Exception ex) {
                            await stream.SendClient(SmtpResponseId.ErrorArgs.SmtpResponse(ex.Message), fslog)
                                  .ConfigureAwait(false);
                        }
                        LastCommand = SmtpCommand.NOOP;
                        break;
                    }
                case SmtpCommand.LOGIN:
                    {
                        try {
                            if (scmd.Length == 1) {
                                if ((!data.IsLogin) && data.LoginCredentialsB64(CredentialsCheckId.Login, scmd[0])) {
                                    await stream.SendClient(SmtpResponseId.AuthPassword.SmtpResponse(), fslog)
                                          .ConfigureAwait(false);
                                    LastCommand = SmtpCommand.LOGIN;
                                    break;
                                }
                                else if ((!data.IsPassword) && data.LoginCredentialsB64(CredentialsCheckId.Password, scmd[0])) {
                                    await stream.SendClient(SmtpResponseId.AuthOk.SmtpResponse(), fslog)
                                          .ConfigureAwait(false);
                                    LastCommand = SmtpCommand.NOOP;
                                    AuthToEvent();
                                    break;
                                }
                            }
                            await stream.SendClient(SmtpResponseId.AuthErrorArgs.SmtpResponse(cmd.ToString()), fslog)
                                  .ConfigureAwait(false);
                            AddAuthFilter.Invoke(IpEndPoint);
                        }
                        catch (Exception ex) {
                            await stream.SendClient(SmtpResponseId.ErrorArgs.SmtpResponse(ex.Message), fslog)
                                  .ConfigureAwait(false);
                        }
                        LastCommand = SmtpCommand.NOOP;
                        break;
                    }
                case SmtpCommand.AUTH:
                    {
                        try {
                            if (scmd.Length < 2) {
                                await stream.SendClient(SmtpResponseId.NotParamArgs.SmtpResponse("invalid arguments count"), fslog)
                                      .ConfigureAwait(false);
                                break;
                            }
                            if (!Enum.TryParse(scmd[1].Replace("-","").ToUpperInvariant(), true, out SmtpCommand opt)) {
                                await stream.SendClient(SmtpResponseId.BadCmdArgs.SmtpResponse($"invalid sub-command: {scmd[1]}"), fslog)
                                      .ConfigureAwait(false);
                                break;
                            }

                            switch (opt) {
                                case SmtpCommand.LOGIN:
                                case SmtpCommand.PLAIN: {
                                        if (scmd.Length == 2) {
                                            LastCommand = opt;
                                            await stream.SendClient(SmtpResponseId.AuthUser.SmtpResponse(), fslog)
                                                  .ConfigureAwait(false);
                                            break;
                                        }
                                        else if (scmd.Length == 3) {
                                            LastCommand = opt;
                                            await ParseCommand_(opt, new string[] { scmd[2] })
                                                  .ConfigureAwait(false);
                                            break;
                                        }

                                        await stream.SendClient(SmtpResponseId.AuthErrorArgs.SmtpResponse(opt.ToString()), fslog)
                                              .ConfigureAwait(false);
                                        AddAuthFilter.Invoke(IpEndPoint);
                                        LastCommand = SmtpCommand.NOOP;
                                        break;
                                    }
                                case SmtpCommand.CRAMMD5: {
                                        if (scmd.Length != 2) {
                                            await stream.SendClient(SmtpResponseId.AuthErrorArgs.SmtpResponse(opt.ToString()), fslog)
                                                  .ConfigureAwait(false);
                                            AddAuthFilter.Invoke(IpEndPoint);
                                            break;
                                        }
                                        LastCommand = opt;
                                        string s = data.CRAMMD5Credentials();
                                        await stream.SendClient(SmtpResponseId.AuthUserArgs.SmtpResponse(s), fslog)
                                              .ConfigureAwait(false);
                                        break;
                                    }
                                default: {
                                        await stream.SendClient(SmtpResponseId.NotSupportArgs.SmtpResponse(scmd[1]), fslog)
                                              .ConfigureAwait(false);
                                        break;
                                    }
                            }
                        }
                        catch (Exception ex) {
                            await stream.SendClient(SmtpResponseId.ErrorArgs.SmtpResponse(ex.Message), fslog)
                                  .ConfigureAwait(false);
                        }
                        break;
                    }
                case SmtpCommand.HELO:
                    {
                        if (scmd.Length >= 2) data.Domain = scmd[1];
                        await stream.SendClient(SmtpResponseId.Hello.SmtpResponse(), fslog)
                              .ConfigureAwait(false);
                        break;
                    }
                case SmtpCommand.EHLO:
                    {
                        if (scmd.Length >= 2) data.Domain = scmd[1];
                        await stream.SendClient(SmtpResponseId.EHello.SmtpResponse(data.Domain), fslog)
                              .ConfigureAwait(false);
                        break;
                    }
                case SmtpCommand.FROM:
                    {
                        if (scmd.Length >= 3) {
                            if (Global.Instance.Config.IsSmtpCheckFrom) {
                                if (!data.CheckFrom(scmd[2])) {
                                    await stream.SendClient(SmtpResponseId.SenderErrorFrom.SmtpResponse(), fslog)
                                          .ConfigureAwait(false);
                                    AddAuthFilter.Invoke(IpEndPoint);
                                    Dispose();
                                    break;
                                }
                            } else
                                data.From = scmd[2].Trim();
                        }
                        await stream.SendClient(SmtpResponseId.Ok.SmtpResponse(), fslog)
                              .ConfigureAwait(false);
                        break;
                    }
                case SmtpCommand.TO:
                    {
                        if (scmd.Length >= 3) {
                            string originalto = (scmd.Length >= 4) ? scmd[3].Trim() : string.Empty;

                            if (data.IsAuthorize) {
                                data.To = scmd[2].Trim();
                                data.OriginalTo = originalto;

                            } else if (IsDeliveryLocal) {
                                if (!data.CheckTo(scmd[2], originalto)) {
                                    await stream.SendClient(SmtpResponseId.BadMailbox.SmtpResponse(), fslog)
                                          .ConfigureAwait(false);
                                    AddAuthFilter.Invoke(IpEndPoint);
                                    Dispose();
                                    break;
                                }
                            } else {
                                await stream.SendClient(SmtpResponseId.NeededLogged.SmtpResponse(), fslog)
                                      .ConfigureAwait(false);
                                AddAuthFilter.Invoke(IpEndPoint);
                                Dispose();
                                break;
                            }
                        }
                        await stream.SendClient(SmtpResponseId.Ok.SmtpResponse(), fslog)
                              .ConfigureAwait(false);
                        break;
                    }
                case SmtpCommand.DATA:
                    {
                        await stream.SendClient(SmtpResponseId.DataBegin.SmtpResponse(), fslog)
                              .ConfigureAwait(false);
                        IsData = true;
                        break;
                    }
                case SmtpCommand.VRFY:
                    {
                        await stream.SendClient(SmtpResponseId.OkArgs.SmtpResponse(
                            !data.IsLogin ? "<not-login>" : $"<{data.Domain}>"),
                            fslog).ConfigureAwait(false);
                        break;
                    }
                case SmtpCommand.RSET:
                    {
                        data.ClearCredentials();
                        await stream.SendClient(SmtpResponseId.Ok.SmtpResponse(), fslog)
                              .ConfigureAwait(false);
                        break;
                    }
                case SmtpCommand.NOOP:
                    {
                        await stream.SendClient(SmtpResponseId.Ok.SmtpResponse(), fslog)
                              .ConfigureAwait(false);
                        break;
                    }
                case SmtpCommand.HELP:
                    {
                        string[] ss = Enum.GetNames(typeof(SmtpCommand));
                        await stream.SendClient(SmtpResponseId.HelpArgs.SmtpResponse(string.Join(",", ss)), fslog)
                              .ConfigureAwait(false);
                        break;
                    }
                case SmtpCommand.QUIT:
                    {
                        await stream.SendClient(SmtpResponseId.LogOut.SmtpResponse(), fslog)
                              .ConfigureAwait(false);
                        Dispose();
                        break;
                    }
                default:
                    {
                        await stream.SendClient(SmtpResponseId.NotSupport.SmtpResponse(), fslog)
                                    .ConfigureAwait(false);
                        break;
                    }
            }
        }

        private void AuthToEvent() {
            string ip = (IpEndPoint == default) ? "" : IpEndPoint.ToString();
            OnCallEvent(MailEventId.UserAuth, $"{data.UserAccount.Email}/{ip}", data.UserAccount);
        }
        #endregion

        #region Message Store
        private async Task MessageStore(MimeMessage mmsg)
        {
            try {
                if (mmsg == default) {
                    Global.Instance.Log.Add(
                        nameof(MessageStore),
                        $"Message is empty, message recipient cannot be determined, may be: {data.From} -> {data.To}, abort");
                    return;
                }
                if (data.IsAuthorize && string.IsNullOrWhiteSpace(data.UserAccount.UserRoot)) {
                    Global.Instance.Log.Add(
                        nameof(MessageStore),
                        "Credentials user path empty, abort");
                    return;
                }
                if (!data.IsAuthorize && !data.MessageRoute.IsDeliveryLocal) {
                    Global.Instance.Log.Add(
                        nameof(MessageStore),
                        "Unauthorized user did not request local mailbox to send, abort");
                    return;
                }
                if (Global.Instance.Config.IsSmtpCheckFrom &&
                    !string.IsNullOrWhiteSpace(data.From) &&
                    (mmsg.From != default) &&
                    (mmsg.From.Count > 0)) {

                    bool[] b = new bool[] {
                        (from i in mmsg.From.Mailboxes
                         where i.Address.Equals(data.From) select i).FirstOrDefault() != default,
                        !string.IsNullOrWhiteSpace(data.UserAccount?.Email) && data.From.Equals(data.UserAccount.Email)
                    };
                    if (!b[0] && !b[1]) {
                        Global.Instance.Log.Add(
                            nameof(MessageStore),
                            $"Message From not equals session From: '{data.From}', abort");
                        return;
                    }
                    if (!b[1] && !data.IsAuthorize)
                        Global.Instance.Log.Add(
                            nameof(MessageStore),
                            $"Receive anonimous message from '{data.From}' - {IpEndPoint}");
                    else if (!b[1])
                        Global.Instance.Log.Add(
                            nameof(MessageStore),
                            $"Receive SPAM message ?, from '{data.From}'/'{data.UserAccount?.Email}' - {IpEndPoint}");
                }
                try {
                    MessageStoreReturn msr = await data.MessageRoute.MessageStore(mmsg, OnCallEvent)
                                                                    .ConfigureAwait(false);
                    switch (msr) {
                        case MessageStoreReturn.MessageNull:
                        case MessageStoreReturn.MessageErrorDelivery: return;
                        case MessageStoreReturn.MessageDelivered: break;
                    }
                    OnCallEvent(MailEventId.EndCall, nameof(MessageStore));
                }
                catch (Exception ex) {
                    Global.Instance.Log.Add(nameof(MessageStore), ex);
                }
                finally {
                    data.ResetDelivery();
                    if (mmsg != default) try { mmsg.Dispose(); } catch { }
                }
            } catch (Exception ex) { Global.Instance.Log.Add(nameof(MessageStore), ex); }
        }
        #endregion
    }
}

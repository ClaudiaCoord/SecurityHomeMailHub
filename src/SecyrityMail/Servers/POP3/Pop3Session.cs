/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SecyrityMail.Data;
using SecyrityMail.Messages;
using SecyrityMail.Servers.POP3.CMD;
using SecyrityMail.Utils;

namespace SecyrityMail.Servers.POP3
{
    public enum Pop3Command : int
    {
        TOP,
        APOP,
        AUTH,
        CAPA,
        USER,
        PASS,
        STAT,
        RSET,
        RETR,
        LIST,
        LOGIN,
        PLAIN,
        DELE,
        NOOP,
        UIDL,
        QUIT,
        HELP,
        STLS,
        STAT_EXTENDED,
        LIST_ONE_INTERNAL,
        LIST_ALL_INTERNAL,
        UIDL_ONE_INTERNAL,
        UIDL_ALL_INTERNAL,
        STARTTLS = STLS
    }

    class Pop3Session : MailEvent, IDisposable
    {
        StreamSession stream;
        TokenSafe token;
        MailMessages storage = default;
        FileStream fslog = default;
        CredentialsData data = new();
        MessagesCacheOpener cacheOpener;

        public bool IsDeleteAllMessages { get; set; } = false;
        public bool IsLog { get; set; } = false;
        public bool IsSecure => stream.IsSecure;
        public string SessionId { get; private set; }
        public double ClientIdle { get; set; } = 20.0;
        public EndPoint IpEndPoint => (stream == default) ? default : stream.IpEndPoint;
        public Stream Stream => stream;
        private Action<MailEvent> UnsubsribeEvent;
        private Action<EndPoint> AddAuthFilter;
        private Pop3Command LastCommand { get; set; } = Pop3Command.NOOP;

        public Pop3Session(TcpClient tcpClient, Action<MailEvent> unsubsribe, Action<EndPoint> afilter, TokenSafe t) {
            UnsubsribeEvent = unsubsribe;
            AddAuthFilter = afilter;
            Init(t, tcpClient, false, false, string.Empty);
        }
        public Pop3Session(TcpClient tcpClient, Action<MailEvent> unsubsribe, Action<EndPoint> afilter, TokenSafe t, bool isSecure, bool islog, string logpath) {
            UnsubsribeEvent = unsubsribe;
            AddAuthFilter = afilter;
            Init(t, tcpClient, isSecure, islog, logpath);
        }
        public Pop3Session(TcpClient tcpClient, Action<MailEvent> unsubsribe, Action<EndPoint> afilter, TokenSafe t, bool isSecure, bool islog, string logpath, bool isdelete) {
            UnsubsribeEvent = unsubsribe;
            AddAuthFilter = afilter;
            IsDeleteAllMessages = isdelete;
            Init(t, tcpClient, isSecure, islog, logpath);
        }
        ~Pop3Session() => DisposeFinal();

        private void Init(TokenSafe t, TcpClient tcpClient, bool isSecure, bool islog, string logpath)
        {
            token = t;
            IsLog = islog;

            if ((tcpClient == null) || !tcpClient.Connected)
                throw new Exception(nameof(TcpClient));

            cacheOpener = CacheOpener.Build(this.GetType());

            if (IsLog)
                fslog = new FileStream(
                    Path.Combine(logpath,
                        $"{nameof(Pop3Session)}-{tcpClient.Client.RemoteEndPoint.ToString().Replace(':','-')}.log"),
                    FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, 4096, true);
            stream = new(tcpClient, isSecure);
            storage = default;
            SessionId = Guid.NewGuid().ToString().Replace("-", "");
        }

        public void Dispose() => stream?.Dispose();
        public void DisposeFinal() {
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

                OnCallEvent(MailEventId.BeginCall, "POP3-IN");
                await stream.SendClient(Pop3ResponseId.Hello.Pop3Response(SessionId), fslog)
                      .ConfigureAwait(false);

                DateTime dt = default;
                byte[] buffer = new byte[2048];
                while (true)
                {
                    if (!stream.IsEnable)
                        break;

                    token.ThrowIfCancellationRequested();

                    StringBuilder sb = new StringBuilder();
                    while (stream.IsDataAvailable) {
                        int count = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (count > 0) {
                            sb.Append(Encoding.ASCII.GetString(buffer, 0, count));
                            if (IsLog)
                                fslog.Write(buffer, 0, count);
                        }
                        else if (stream.IsDataAvailable) continue;
                        else if (count < 0) break;
                    }
                    if (sb.Length > 0) {
                        ParseCommand(sb.ToString());
                        dt = DateTime.Now.AddSeconds(ClientIdle);
                    }
                    else {
                        if (dt.CheckDateTime() && (dt < DateTime.Now))
                            break;
                        await Task.Delay(200).ConfigureAwait(false);
                    }
                }
            }
            catch (TaskCanceledException) {
                await stream.SendClient(Pop3ResponseId.Error.Pop3Response(), fslog)
                      .ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                await stream.SendClient(Pop3ResponseId.Error.Pop3Response(), fslog)
                      .ConfigureAwait(false);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(NewSession), ex); }
            finally {

                if ((storage != default) && (data != default) && !string.IsNullOrWhiteSpace(data.UserAccount.Email))
                    await cacheOpener.Close(data.UserAccount.Email);

                OnCallEvent(MailEventId.EndCall, "POP3-IN");
                UnsubsribeEvent.Invoke(this);
                UnsubsribeEvent = (a) => { };

                if (IsLog)
                    await fslog?.FlushAsync();
                Dispose();
                data = default;
                storage = default;
                GC.Collect();
            }
        }
        #endregion

        #region Parse Command
        private async void ParseCommand(string request)
        {
#           if DEBUG_PRINT
            Global.Instance.Log.Add($"{nameof(Pop3Session)}:{nameof(ParseCommand)}(1) -> {request}");
#           endif
            if (string.IsNullOrWhiteSpace(request))
            {
                await stream.SendClient(Pop3ResponseId.Error.Pop3Response(), fslog)
                      .ConfigureAwait(false);
                return;
            }
            string[] scmd = request.ParsePop3Command();
            if ((scmd == default) || (scmd.Length == 0))
            {
                await stream.SendClient(Pop3ResponseId.Error.Pop3Response(), fslog)
                      .ConfigureAwait(false);
                return;
            }
            if (!Enum.TryParse(scmd[0], true, out Pop3Command cmd))
            {
                if (LastCommand == Pop3Command.PLAIN)
                    cmd = LastCommand;
                else {
                    await stream.SendClient(Pop3ResponseId.NotSupport.Pop3Response(), fslog)
                                .ConfigureAwait(false);
                    return;
                }
            }
            switch (cmd) {
                case Pop3Command.HELP:
                case Pop3Command.CAPA:
                case Pop3Command.STLS:
                case Pop3Command.NOOP:
                case Pop3Command.QUIT: break;
                case Pop3Command.AUTH: {
                        if ((scmd.Length >= 2) &&
                            Enum.TryParse(scmd[1], true, out Pop3Command cmd2) && (cmd2 == Pop3Command.PLAIN))
                            cmd = LastCommand = Pop3Command.PLAIN;
                        break;
                    }
                case Pop3Command.APOP:
                case Pop3Command.USER:
                case Pop3Command.PASS:
                case Pop3Command.PLAIN:
                case Pop3Command.LOGIN: {
                        if (data.IsAuthorize) {
                            await stream.SendClient(Pop3ResponseId.AlreadyLogged.Pop3Response(), fslog)
                                        .ConfigureAwait(false);
                            return;
                        }
                        break;
                    }
                case Pop3Command.TOP:
                case Pop3Command.UIDL:
                case Pop3Command.RETR:
                case Pop3Command.DELE:
                case Pop3Command.LIST:
                case Pop3Command.STAT: {
                        if (storage == null) {
                            CancellationTokenSource cts = default;
                            try {
                                cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                                CancellationToken token = cts.Token;
                                while (storage == null) {
                                    await Task.Delay(150);
                                    if (token.IsCancellationRequested)
                                        break;
                                }
                            } catch {
                            } finally { if (cts != default) cts.Dispose(); }
                            if (storage == null)
                                await stream.SendClient(Pop3ResponseId.StorageWait.Pop3Response(), fslog)
                                            .ConfigureAwait(false);
                            return;
                        }
                        break;
                    }
                default: {
                        if (!data.IsAuthorize) {
                            await stream.SendClient(Pop3ResponseId.NeededLogged.Pop3Response(), fslog)
                                        .ConfigureAwait(false);
                            return;
                        }
                        break;
                    }
            }
            await ParseCommand_(cmd, scmd).ConfigureAwait(false);
        }
        private async Task ParseCommand_(Pop3Command cmd, string [] scmd)
        {
#           if DEBUG_PRINT
            Global.Instance.Log.Add($"{nameof(Pop3Session)}:{nameof(ParseCommand)}(2) -> {data.IsAuthorize}/{cmd}/{scmd.Length} = {string.Join(",", scmd)}");
#           endif
            switch (cmd)
            {
                case Pop3Command.STLS:
                    {
                        if (stream.IsSecure) {
                            await stream.SendClient(Pop3ResponseId.TlsAlready.Pop3Response(), fslog)
                                        .ConfigureAwait(false);
                            break;
                        }
                        await stream.SendClient(Pop3ResponseId.StartTls.Pop3Response(), fslog)
                                    .ConfigureAwait(false);
                        await stream.StartTls().ConfigureAwait(false);
                        OnCallEvent(MailEventId.StartTls, (IpEndPoint == default) ? "no IP address!" : IpEndPoint.ToString(), IpEndPoint);
                        break;
                    }
                case Pop3Command.CAPA:
                    {
                        await stream.SendClient(Pop3ResponseId.Capa.Pop3Response(), fslog)
                              .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.PASS:
                case Pop3Command.USER:
                case Pop3Command.LOGIN:
                    {
                        if (scmd.Length < 2) {
                            await stream.SendClient(Pop3ResponseId.ErrorArgs.Pop3Response(scmd[0].Trim()), fslog)
                                  .ConfigureAwait(false);
                            break;
                        }
                        bool b = cmd switch {
                            Pop3Command.USER => data.LoginCredentials(CredentialsCheckId.Login, scmd[1]),
                            Pop3Command.PASS => data.LoginCredentials(CredentialsCheckId.Password, scmd[1]),
                            _ => false
                        };
                        if (b) {
                            if (cmd == Pop3Command.PASS)
                                _ = await BuildMessageList().ConfigureAwait(false);
                            await stream.SendClient(Pop3ResponseId.AcceptedArgs.Pop3Response(scmd[0].Trim()), fslog)
                                        .ConfigureAwait(false);
                            AuthToEvent(cmd);
                        } else {
                            await stream.SendClient(Pop3ResponseId.WrongAccount.Pop3Response(scmd[0].Trim()), fslog)
                                        .ConfigureAwait(false);
                            WrongAuthToEvent(scmd);
                            Dispose();
                        }
                        break;
                    }
                case Pop3Command.PLAIN:
                    {
                        if (scmd.Length == 2) { 
                            await stream.SendClient(Pop3ResponseId.Ok.Pop3Response(), fslog)
                                        .ConfigureAwait(false);
                            break;
                        } else if ((!data.IsAuthorize) && data.PlainCredentials(scmd[0])) {
                            _ = await BuildMessageList().ConfigureAwait(false);
                            await stream.SendClient(Pop3ResponseId.AcceptedArgs.Pop3Response(), fslog)
                                        .ConfigureAwait(false);
                            AuthToEvent(cmd);
                        } else {
                            await stream.SendClient(Pop3ResponseId.WrongAccount.Pop3Response(scmd[0].Trim()), fslog)
                                        .ConfigureAwait(false);
                            WrongAuthToEvent(scmd);
                            Dispose();
                        }
                        LastCommand = Pop3Command.NOOP;
                        break;
                    }
                case Pop3Command.APOP:
                    {
                        if ((scmd.Length < 3) ||
                            string.IsNullOrWhiteSpace(scmd[1]) || string.IsNullOrWhiteSpace(scmd[2])) {
                                await stream.SendClient(Pop3ResponseId.ErrorArgs.Pop3Response(scmd[0].Trim()), fslog)
                                            .ConfigureAwait(false);
                            break;
                        }
                        if (!data.LoginCredentials(CredentialsCheckId.Login, scmd[1]) ||
                            !data.ApopCredentials(SessionId, scmd[2])) {
                            await stream.SendClient(Pop3ResponseId.WrongAccount.Pop3Response(scmd[0].Trim()), fslog)
                                  .ConfigureAwait(false);
                            WrongAuthToEvent(scmd);
                            Dispose();
                            break;
                        }
                        _ = await BuildMessageList().ConfigureAwait(false);
                        await ParseCommand_(Pop3Command.STAT_EXTENDED, scmd)
                              .ConfigureAwait(false);
                        AuthToEvent(cmd);
                        break;
                    }
                case Pop3Command.AUTH:
                    {
                        await stream.SendClient(Pop3ResponseId.AuthCap.Pop3Response(), fslog)
                                    .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.STAT:
                    {
                        if (storage.IsBusy) {
                            CancellationTokenSource cts = default;
                            try {
                                cts = new CancellationTokenSource(TimeSpan.FromSeconds(9));
                                CancellationToken token = cts.Token;
                                while (storage.IsBusy) {
                                    await Task.Delay(150);
                                    if (token.IsCancellationRequested)
                                        break;
                                }
                            } catch { }
                            finally { if (cts != default) cts.Dispose(); }
                        }
                        string s = await CalculateMessages().ConfigureAwait(false);
                        if (string.IsNullOrEmpty(s))
                            await stream.SendClient(Pop3ResponseId.Error.Pop3Response(), fslog)
                                        .ConfigureAwait(false);
                        else
                            await stream.SendClient(s, fslog)
                                        .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.STAT_EXTENDED:
                    {
                        string s = await CalculateMessages(Pop3Command.STAT_EXTENDED).ConfigureAwait(false);
                        if (string.IsNullOrEmpty(s))
                            await stream.SendClient(Pop3ResponseId.Error.Pop3Response(), fslog)
                                        .ConfigureAwait(false);
                        else
                            await stream.SendClient(s, fslog)
                                        .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.LIST:
                    {
                        if (scmd.Length == 2)
                            await ParseCommand_(Pop3Command.LIST_ONE_INTERNAL, scmd)
                                  .ConfigureAwait(false);
                        else
                            await ParseCommand_(Pop3Command.LIST_ALL_INTERNAL, scmd)
                                  .ConfigureAwait(false);
                        return;
                    }
                case Pop3Command.LIST_ALL_INTERNAL:
                    {
                        await ParseCommand_(Pop3Command.STAT_EXTENDED, scmd)
                              .ConfigureAwait(false);
                        _ = await stream.RunCommand(
                            CmdLIST_All.Function, storage, scmd, () => Pop3ResponseId.NoMessageArgs.Pop3Response(storage.Counts), fslog)
                                       .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.LIST_ONE_INTERNAL:
                    {
                        _ = await stream.RunCommand(
                            CmdLIST_One.Function, storage, scmd, () => Pop3ResponseId.NoMessageArgs.Pop3Response(storage.Counts), fslog)
                                       .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.DELE:
                    {
                        _ = await stream.RunCommand(
                            CmdDELE.Function, storage, scmd, () => Pop3ResponseId.NoMessageArgs.Pop3Response(storage.Counts), fslog)
                            .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.RETR:
                    {
                        _ = await stream.RunCommand(
                            CmdRETR.Function, storage, scmd, () => Pop3ResponseId.NoMessageArgs.Pop3Response(storage.Counts), fslog)
                            .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.TOP:
                    {
                        _ = await stream.RunCommand(
                            CmdTOP.Function, storage, scmd, () => Pop3ResponseId.NoMessageArgs.Pop3Response(storage.Counts), fslog)
                            .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.UIDL:
                    {
                        if (scmd.Length == 2)
                            await ParseCommand_(Pop3Command.UIDL_ONE_INTERNAL, scmd)
                                  .ConfigureAwait(false);
                        else
                            await ParseCommand_(Pop3Command.UIDL_ALL_INTERNAL, scmd)
                                  .ConfigureAwait(false);
                        return;
                    }
                case Pop3Command.UIDL_ALL_INTERNAL:
                    {
                        _ = await stream.RunCommand(
                            CmdUIDL_All.Function, storage, scmd, () => Pop3ResponseId.NoMessageArgs.Pop3Response(storage.Counts), fslog)
                                       .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.UIDL_ONE_INTERNAL:
                    {
                        _ = await stream.RunCommand(
                            CmdUIDL_One.Function, storage, scmd, () => Pop3ResponseId.NoMessageArgs.Pop3Response(storage.Counts), fslog)
                                       .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.HELP:
                    {
                        string [] ss = Enum.GetNames(typeof(Pop3Command));
                        await stream.SendClient(Pop3ResponseId.HelpArgs.Pop3Response(string.Join(",", ss)), fslog)
                                    .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.NOOP:
                    {
                        await stream.SendClient(Pop3ResponseId.Ok.Pop3Response(), fslog)
                                    .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.RSET:
                    {
                        if (storage != default)
                            _ = await storage.UnDelete().ConfigureAwait(false);
                        data.ClearCredentials();
                        await stream.SendClient(Pop3ResponseId.Reset.Pop3Response(), fslog)
                                    .ConfigureAwait(false);
                        break;
                    }
                case Pop3Command.QUIT:
                    {
                        await stream.SendClient(Pop3ResponseId.LogOut.Pop3Response(), fslog)
                                    .ConfigureAwait(false);

                        if (IsDeleteAllMessages && (storage != default))
                            await storage.ClearDeleted().ConfigureAwait(false);
                        Dispose();
                        break;
                    }
                default:
                    {
                        await stream.SendClient(Pop3ResponseId.NotSupport.Pop3Response(), fslog)
                                    .ConfigureAwait(false);
                        break;
                    }
            }
        }
        private void AuthToEvent(Pop3Command cmd) {
            string ip = (IpEndPoint == default) ? "" : IpEndPoint.ToString();
            OnCallEvent(MailEventId.UserAuth, $"Auth {cmd}: {data.UserAccount.Email}/{ip}", data.UserAccount);
        }
        private void WrongAuthToEvent(string [] ss) {
            string ip = string.Empty;
            if (IpEndPoint != default) {
                AddAuthFilter.Invoke(IpEndPoint);
                ip = IpEndPoint.ToString();
            }
            OnCallEvent(MailEventId.UserAuth, $"Auth BAD: {ip}", string.Join(",", ss));
        }
        #endregion

        #region utils
        private async Task<string> CalculateMessages(Pop3Command type = Pop3Command.STAT) =>
            await Task<string>.Run(async () => {
                try {
                    if (storage.Count == 0)
                        _ = await storage.Scan().ConfigureAwait(false);
                    if (storage.Count > 0)
                        return (type == Pop3Command.STAT_EXTENDED) ?
                            Pop3ResponseId.StatMsgExtArgs.Pop3Response(storage.Count.ToString(), storage.TotalSizes) :
                            Pop3ResponseId.StatMsgArgs.Pop3Response(storage.Count.ToString(), storage.TotalSizes);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(CalculateMessages), ex); }
                return (type == Pop3Command.STAT_EXTENDED) ?
                    Pop3ResponseId.StatMsgExtArgs.Pop3Response("0", "0") :
                    Pop3ResponseId.StatMsgArgs.Pop3Response("0", "0");
            });

        private async Task<bool> BuildMessageList() =>
            await Task<bool>.Run(async () => {
                try {
                    do {
                        if (!data.IsAuthorize || !data.UserAccount.IsRootPath)
                            break;

                        storage = await cacheOpener.Open(data.UserAccount.Email)
                                                   .ConfigureAwait(false);
                        if (storage == default)
                            break;
                        return true;

                    } while (false);
                    await stream.SendClient(Pop3ResponseId.StorageError.Pop3Response(), fslog)
                                .ConfigureAwait(false);
                }
                catch (Exception ex) {
                    await stream.SendClient(Pop3ResponseId.ErrorArgs.Pop3Response(ex.Message), fslog)
                                .ConfigureAwait(false);
                    Global.Instance.Log.Add(nameof(BuildMessageList), ex);
                }
                return false;
            });
        #endregion
    }
}

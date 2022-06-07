/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MimeKit;
using MimeKit.Cryptography;
using SecyrityMail.Servers;
using SecyrityMail.Utils;

namespace SecyrityMail.Messages
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class MailMessage
    {
        private const string ErrorStatus = "5.3.2";
        private const string ErrorAction = "failed";
        private const string ErrorSubject = "Undelivered Mail Returned to Sender";
        private const string ErrorDiagCode = "X-LOCAL; no credentials to send this message from this account";
        private const string ErrorActionId = "Action";
        private const string ErrorDiagCodeId = "Diagnostic-Code";
        private const string ErrorReportingMtaId = "Reporting-MTA";
        private const string ErrorFinalRecipientId = "Final-Recipient";
        private const string ErrorArrivalDateId = "Arrival-Date";
        private const string DeliveryStatusId = "delivery-status";
        public const string XConfirmReadingToId = "X-Confirm-Reading-To";
        public const string XSPAM = "(SPAM)";

        private RunOnce runOnce = new();

        [XmlElement("id")]
        public int Id { get; set; } = 0;
        [XmlElement("msgid")]
        public string MsgId { get; set; } = string.Empty;
        [XmlElement("size")]
        public long Size { get; set; } = 0L;
        [XmlElement("from")]
        public string From { get; set; } = string.Empty;
        [XmlElement("subj")]
        public string Subj { get; set; } = string.Empty;
        [XmlElement("path")]
        public string FilePath { get; set; } = string.Empty;
        [XmlElement("folder")]
        public Global.DirectoryPlace Folder { get; set; } = Global.DirectoryPlace.None;
        [XmlElement("isread")]
        public bool IsRead { get; set; } = false;

        [XmlElement("date")]
        public DateTime DateSerialize { get; set; } = DateTime.MinValue;
        [XmlIgnore]
        public DateTimeOffset Date {
            get => new DateTimeOffset(DateSerialize);
            set { if (value.CheckDateTimeOffset()) DateSerialize = value.UtcDateTime; }
        }
        public MailMessage() { }

        public void Set(int id, string mid, string from, string subj, string file) =>
            Set(id, mid, from, subj, new FileInfo(file));

        public void Set(int id, string mid, string from, string subj, FileInfo file) {
            Id = id;
            From = from;
            Subj = NormalizeSubject(subj);
            MsgId = mid;
            Size = file.Length;
            FilePath = file.FullName;
            (Global.DirectoryPlace place, string _, DateTimeOffset dt) = Global.GetFolderInfo(file.FullName);
            Folder = place;
            if (dt != default) Date = dt;
            else DateSerialize = file.CreationTimeUtc;
        }

        public MailMessage Copy(MailMessage msg, int count) {
            if (msg == null) return null;

            Id = msg.Id = count;
            Size = msg.Size;
            From = msg.From;
            Subj = msg.Subj;
            Date = msg.Date;
            MsgId = msg.MsgId;
            Folder = msg.Folder;
            FilePath = msg.FilePath;
            return this;
        }

        #region LoadAndCreate
        public async Task<MailMessage> LoadAndCreate(string file, int count) => await LoadAndCreate(new FileInfo(file), count);
        public async Task<MailMessage> LoadAndCreate(FileInfo file, int count) =>
            await Task.Run(async () => {
                if (!runOnce.Begin())
                    return default;
                MimeMessage mmsg = default(MimeMessage);
                try {
                    if ((file == null) || (file.Length == 0) || !file.Exists)
                        return null;

                    mmsg = await MimeMessage.LoadAsync(file.FullName)
                                            .ConfigureAwait(false);
                    if (mmsg == null)
                        return null;

                    if (string.IsNullOrWhiteSpace(mmsg.MessageId)) {
                        mmsg.MessageId = GetOrCreateMessageId(mmsg.MessageId);
                        await mmsg.WriteToAsync(file.FullName);
                        file.Refresh();
                    }
                    string from = ((mmsg.From == null) || (mmsg.From.Count == 0)) ? string.Empty : mmsg.From.ToString();
                    Set(count, mmsg.MessageId, from, mmsg.Subject, file);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(LoadAndCreate), ex); return null; }
                finally {
                    if (mmsg != default)
                        try { mmsg.Dispose(); } catch { }
                    runOnce.End();
                }
                return this;
            });
        #endregion

        #region CreateAndDelivery
        public async Task<MailMessage> CreateAndDelivery(
            Global.DirectoryPlace place, string msgpath, int count, string rootpath = default,
            bool isdelete = true, bool pgpauto = false, bool localdelivery = true) =>
            await CreateAndDelivery(place, new FileInfo(msgpath), count, rootpath, isdelete, pgpauto, localdelivery);

        public async Task<MailMessage> CreateAndDelivery(
            Global.DirectoryPlace place, FileInfo msgpath, int count, string rootpath = default,
            bool isdelete = true, bool pgpauto = false, bool localdelivery = true) {

            if (runOnce.IsRunning)
                return default;

            MimeMessage mmsg = default(MimeMessage);
            try {
                if ((msgpath == null) || (msgpath.Length == 0) || !msgpath.Exists || msgpath.IsReadOnly)
                    return default;

                mmsg = await MimeMessage.LoadAsync(msgpath.FullName)
                                        .ConfigureAwait(false);
                if (mmsg == null)
                    return default;

                if (string.IsNullOrWhiteSpace(rootpath))
                    rootpath = Path.GetDirectoryName(Path.GetDirectoryName(msgpath.FullName));

                MailMessage msg = await CreateAndDelivery(place, mmsg, rootpath, count, pgpauto, localdelivery);

                FileInfo f = new(msg.FilePath);
                if ((f == null) || (f.Length == 0) || !f.Exists) {
                    msg.FilePath = Path.Combine(
                        Global.AppendPartDirectory(rootpath, Global.DirectoryPlace.Error, default),
                        Path.GetFileName(msg.FilePath));
                    msgpath.MoveTo(msg.FilePath);
                }
                if (isdelete)
                    msgpath.Delete();

                return msg;
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(CreateAndDelivery), ex); return null; }
            finally {
                if (mmsg != default)
                    try { mmsg.Dispose(); } catch { }
            }
        }
        public async Task<MailMessage> CreateAndDelivery(
            Global.DirectoryPlace place, MimeMessage mmsg, string rootpath, int count, bool pgpauto = false, bool localdelivery = true) =>
            await Task.Run(async () => {
                if (!runOnce.Begin())
                    return default;
                MimeMessage mmsgTmp = default;
                try {
                    if ((mmsg == null) || string.IsNullOrWhiteSpace(rootpath))
                        return null;

                    Id     = count;
                    From   = ((mmsg.From == null) || (mmsg.From.Count == 0)) ? string.Empty : mmsg.From.ToString();
                    Subj   = NormalizeSubject(mmsg.Subject);
                    Date   = mmsg.Date = GetOrCreateDateTimeOffset(mmsg.Date);
                    MsgId  = mmsg.MessageId = GetOrCreateMessageId(Global.Instance.Config.IsAlwaysNewMessageId ? string.Empty : mmsg.MessageId);
                    Folder = place;

                    switch (place) {
                        case Global.DirectoryPlace.Msg: {
                                bool iscrypted = false,
                                     iscrypt = false;
                                try {
                                    if (mmsg.Headers != default) {
                                        mmsg.Headers.RemoveAll(HeaderId.DispositionNotificationTo);
                                        mmsg.Headers.RemoveAll(HeaderId.DispositionNotificationOptions);
                                        mmsg.Headers.Remove(XConfirmReadingToId);
                                        mmsg.Headers.Remove(HeaderId.ReturnReceiptTo);
                                    }
                                    if (mmsg.Body is MultipartEncrypted) {
                                        iscrypted = true;
                                        if (pgpauto)
                                            try {
                                                MailMessageCrypt crypt = new();
                                                iscrypt = await crypt.Decrypt(mmsg);
                                                if (iscrypt)
                                                    Subj += " (PGP decoded)";
                                            }
                                            catch (Exception ex) {
                                                Global.Instance.Log.Add(nameof(MailMessageCrypt.Decrypt), ex);
                                            }
                                    }
                                    if ((iscrypted && iscrypt) || !iscrypted) {

                                        if (localdelivery && !string.IsNullOrWhiteSpace(mmsg.HtmlBody)) {

                                            BodyBuilder builder = new();
                                            CopyAttachments(builder, mmsg.Attachments);
                                            builder.HtmlBody = new ConverterHtmlToHtml().Convert(mmsg);
                                            builder.TextBody = mmsg.TextBody;

                                            mmsg.Body = builder.ToMessageBody();
                                        }
                                        if (Global.Instance.Config.IsSaveAttachments)
                                            await SaveAttachments(mmsg.Attachments, rootpath, mmsg.MessageId, mmsg.Date)
                                                  .ConfigureAwait(false);
                                    }
                                }
                                catch (Exception ex) { Global.Instance.Log.Add(place.ToString(), ex); }
                                finally {
                                    if (iscrypted)
                                        Global.Instance.Log.Add(nameof(MailMessageCrypt),
                                            $"PGP message {place}/'{mmsg.MessageId}' status: {iscrypted}/{iscrypt}");
                                }
                                break;
                            }
                        case Global.DirectoryPlace.Error: {

                                try {
                                    mmsgTmp = mmsg;
                                    MailboxAddress from = FromAddress(mmsgTmp, rootpath);
                                    mmsg = new MimeMessage {
                                        Body = new MultipartReport(DeliveryStatusId) {
                                            new MessageDeliveryStatus {
                                                StatusGroups = {
                                                    new HeaderList {
                                                        new Header(ErrorReportingMtaId, $"dns;<{Environment.MachineName}.local>"),
                                                        new Header(ErrorArrivalDateId, DateTime.UtcNow.ToString())
                                                    },
                                                    new HeaderList {
                                                        new Header(ErrorFinalRecipientId, $"rfc822;<{from.Address}>"),
                                                        new Header(ErrorActionId, ErrorAction),
                                                        new Header(HeaderId.Status, ErrorStatus),
                                                        new Header(ErrorDiagCodeId, ErrorDiagCode)
                                                    }
                                                }
                                            },
                                            new MessagePart { Message = mmsgTmp }
                                        },
                                        From = { MailerDaemonAddress() },
                                        To = { from },
                                        Subject = MailerDaemonSubject(mmsgTmp.Subject),
                                        InReplyTo = mmsgTmp.MessageId.ToString(),
                                        MessageId = GetOrCreateMessageId(string.Empty)
                                    };
                                }
                                catch (Exception ex) { Global.Instance.Log.Add(place.ToString(), ex); }
                                break;
                            }
                        case Global.DirectoryPlace.Out: {
                                MailMessageCrypt.Actions actions = MailMessageCrypt.Actions.None;
                                bool iscrypt = false;
                                try {
                                    if (mmsg.Headers != default) {
                                        mmsg.Headers.RemoveAll(HeaderId.Cc);
                                        mmsg.Headers.RemoveAll(HeaderId.Bcc);
                                        mmsg.Headers.RemoveAll(HeaderId.ContentReturn);
                                        mmsg.Headers.RemoveAll(HeaderId.X400ContentReturn);
                                        mmsg.Headers.RemoveAll(HeaderId.DispositionNotificationTo);
                                        mmsg.Headers.RemoveAll(HeaderId.DispositionNotificationOptions);
                                        mmsg.Headers.Remove(XConfirmReadingToId);
                                        mmsg.Headers.Remove(HeaderId.ReturnReceiptTo);
                                        mmsg.Headers.Remove(HeaderId.UserAgent);
                                    }
                                } catch { }
                                if (Global.Instance.Config.IsSmtpAllOutPgpCrypt ||
                                    Global.Instance.Config.IsSmtpAllOutPgpSign) {
                                    try {
                                        MailMessageCrypt crypt = new();
                                        bool[] b = new bool[] {
                                            Global.Instance.Config.IsSmtpAllOutPgpCrypt && !crypt.CheckCrypted(mmsg),
                                            Global.Instance.Config.IsSmtpAllOutPgpSign && !crypt.CheckSigned(mmsg)
                                        };
                                        actions = (b[0] && b[1]) ? MailMessageCrypt.Actions.SignEncrypt :
                                            (b[0] ? MailMessageCrypt.Actions.Encrypt :
                                                (b[1] ? MailMessageCrypt.Actions.Sign : MailMessageCrypt.Actions.None));

                                        switch (actions) {
                                            case MailMessageCrypt.Actions.Sign: {
                                                    iscrypt = await crypt.Sign(mmsg).ConfigureAwait(false);
                                                    break;
                                                }
                                            case MailMessageCrypt.Actions.Encrypt: {
                                                    iscrypt = await crypt.Encrypt(mmsg).ConfigureAwait(false);
                                                    break;
                                                }
                                            case MailMessageCrypt.Actions.SignEncrypt: {
                                                    iscrypt = await crypt.SignEncrypt(mmsg).ConfigureAwait(false);
                                                    break;
                                                }
                                            default: break;
                                        }
                                    }
                                    catch (Exception ex) { Global.Instance.Log.Add(place.ToString(), ex); }
                                    finally {
                                        if (actions != MailMessageCrypt.Actions.None)
                                            Global.Instance.Log.Add(nameof(MailMessageCrypt),
                                                $"PGP message {place}/'{mmsg.MessageId}' status: {iscrypt}");
                                    }
                                }
                                break;
                            }
                    }

                    DateTimeOffset dt = Date;
                    string filename = $"{dt.Hour}-{dt.Minute}-{dt.Second}-{Id}-{MsgId.Replace('@', '_')}.eml";
                    FilePath = Path.Combine(
                        Global.AppendPartDirectory(rootpath, place,
                            ((place == Global.DirectoryPlace.Out) || (place == Global.DirectoryPlace.Error)) ? default : dt),
                        filename);

                    await mmsg.WriteToAsync(FilePath).ContinueWith((t) => {
                        if (mmsgTmp != null) try { mmsgTmp.Dispose(); } catch { } 
                    }).ConfigureAwait(false);

                    FileInfo f = new(FilePath);
                    if ((f != null) && (f.Length > 0L) && f.Exists)
                        Size = f.Length;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(CreateAndDelivery), ex); return null; }
                finally { runOnce.End(); }
                return this;
            });
        #endregion

        #region Check
        public bool Check()
        {
            try {
                do {
                    if (string.IsNullOrWhiteSpace(FilePath))
                        break;

                    FileInfo f = new(FilePath);
                    if ((f == null) || (f.Length == 0) || !f.Exists)
                        break;

                    Size = f.Length;
                    DateSerialize = f.CreationTimeUtc;
                    return true;

                } while (false);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(Check), ex); }
            return false;
        }
        #endregion

        #region Move to folder
        public bool MoveToFolder(Global.DirectoryPlace place, string path = default)
        {
            try {
                if (string.IsNullOrWhiteSpace(FilePath) || !Global.IsAccountFolderValid(place))
                    return false;

                FileInfo f = new FileInfo(FilePath);
                if ((f == default) || !f.Exists)
                    return false;

                (Global.DirectoryPlace placed, string rootdir, DateTimeOffset dt) = Global.GetFolderInfo(FilePath);
                if ((placed == place) || (place == Global.DirectoryPlace.None))
                    return false;

                if (string.IsNullOrWhiteSpace(path)) {
                    if (string.IsNullOrWhiteSpace(rootdir))
                        return false;
                    path = Global.GetUserDirectory(rootdir);
                }

                DirectoryInfo dir = Directory.CreateDirectory(Global.AppendPartDirectory(path, place, dt));
                f.MoveTo(Path.Combine(
                            dir.FullName,
                            Path.GetFileName(FilePath)));
                f.Refresh();
                if (f.Exists) {
                    Folder = place;
                    FilePath = f.FullName;
                    if ((place == Global.DirectoryPlace.Spam) && !Subj.Contains(MailMessage.XSPAM))
                        Subj += $" {XSPAM}";
                    else if ((placed == Global.DirectoryPlace.Spam) && Subj.Contains(MailMessage.XSPAM)) {
                        int idx = Subj.IndexOf(XSPAM);
                        if (idx > 0)
                            Subj = Subj.Substring(0, idx);
                        else if (idx == 0) {
                            if (Subj.Length == XSPAM.Length)
                                Subj = string.Empty;
                            else if (Subj.Length > XSPAM.Length)
                                Subj = Subj.Substring(XSPAM.Length);
                        }
                    }
                    return true;
                }
            } catch (Exception ex) { Global.Instance.Log.Add(nameof(MoveToFolder), ex); }
            return false;
        }
        #endregion

        #region MimeMessage to text
        public async Task<Tuple<MimeMessage, string>> MimeMessageToText() =>
            await Task.Run(async () => {
                try {
                    MimeMessage mmsg = await MimeMessage.LoadAsync(FilePath)
                                                        .ConfigureAwait(false);
                    if (mmsg == null)
                        return new Tuple<MimeMessage, string>(default, string.Empty);

                    StringBuilder sb = new();
                    sb.AppendLine($"{Environment.NewLine}{Date} - {From}{Environment.NewLine}{Subj}");

                    if (!string.IsNullOrWhiteSpace(mmsg.TextBody))
                        sb.AppendLine(mmsg.TextBody);
                    else if (!string.IsNullOrWhiteSpace(mmsg.HtmlBody))
                        sb.AppendLine(new ConverterHtmlToHtml().ConvertT(mmsg.HtmlBody));
                    return new Tuple<MimeMessage, string>(mmsg, sb.ToString());
                } catch { }
                return new Tuple<MimeMessage, string>(default, string.Empty);
            });
        #endregion

        #region Message utils
        private string GetOrCreateMessageId(string id) =>
            string.IsNullOrWhiteSpace(id) ? MimeKit.Utils.MimeUtils.GenerateMessageId(Path.GetRandomFileName()) : id;

        private DateTimeOffset GetOrCreateDateTimeOffset(DateTimeOffset dt) =>
            (dt == default) ? DateTimeOffset.UtcNow : dt;

        private MailboxAddress MailerDaemonAddress() =>
            new MailboxAddress("Mail Delivery System", $"MAILER-DAEMON@{Environment.MachineName}.local");

        private MailboxAddress FromAddress(MimeMessage mmsg, string s) =>
            (mmsg.From.Count > 0) ? (MailboxAddress)mmsg.From[0] : new MailboxAddress("Account", Path.GetFileName(s));

        private string MailerDaemonSubject(string s) =>
            string.IsNullOrWhiteSpace(s) ? ErrorSubject : $"{ErrorSubject}: {s}";

        private string NormalizeSubject(string s) =>
            string.IsNullOrWhiteSpace(s) ? string.Empty : Regex.Replace(s, @"\p{C}+", string.Empty);

        private async Task SaveAttachments(IEnumerable<MimeEntity> attachments, string rootpath, string id, DateTimeOffset dt) =>
            await Task.Run(() => {
                try {
                    if ((attachments == null) || (attachments.Count() == 0))
                        return;

                    string path = Global.AppendPartDirectory(rootpath, Global.DirectoryPlace.Attach, dt);
                    foreach (MimeEntity a in attachments) {
                        if (a is MessagePart mep) {
                            using FileStream stream = OpenAttachFile(path, id, mep.ContentDisposition?.FileName);
                            mep.Message.WriteTo(stream);
                        }
                        else if (a is MimePart mip) {
                            using FileStream stream = OpenAttachFile(path, id, mip.FileName);
                            mip.Content.DecodeTo(stream);
                        }
                    }
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(SaveAttachments), ex); }
            });

        private FileStream OpenAttachFile(string path, string id, string name) =>
            File.OpenWrite(GetAttachFilePath(path, id, name));

        public static string GetAttachFilePath(string path, string id, string name) {
            id = id.Replace("@", "_").Replace(".", "_");
            return Path.Combine(path,
                string.IsNullOrEmpty(name) ? $"{id}-unknown.bin" : $"{id}-{name}");
        }

        public static void CopyAttachments(BodyBuilder builder, IEnumerable<MimeEntity> attachs) {
            if ((attachs == null) || (attachs.Count() == 0))
                return;
            foreach (var a in attachs)
                builder.Attachments.Add(a);
        }

        public static string GetMimeEntryName(MimeEntity entry) {
            string name = string.Empty;
            if (entry is MessagePart mep) {
                if (!string.IsNullOrWhiteSpace(mep.ContentDisposition?.FileName))
                    name = Path.GetFileName(mep.ContentDisposition?.FileName);
            }
            else if (entry is MimePart mip) {
                if (!string.IsNullOrWhiteSpace(mip?.FileName))
                    name = Path.GetFileName(mip?.FileName);
            }
            return name;
        }
        #endregion
    }
}

/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MimeKit;
using MimeKit.Cryptography;
using SecyrityMail.Utils;

namespace SecyrityMail.Messages
{
    public class MessageBuilder : IDisposable
    {
        public MimeMessage Message { get; private set; } = default(MimeMessage);

        public MessageBuilder(MimeMessage mmsg) => Message = mmsg;
        ~MessageBuilder() => Dispose();

        public void Dispose() {

            MimeMessage mmsg = Message;
            Message = null;
            if (mmsg != null)
                mmsg.Dispose();
        }

        public void HeaderFilter(Global.DirectoryPlace place) {
            switch (place) {
                case Global.DirectoryPlace.Msg: HeaderFilterMsg(); break;
                case Global.DirectoryPlace.Out: HeaderFilterOut(); break;
            }
        }

        public async Task<bool> BodyFilter(Global.DirectoryPlace place, bool pgpauto = false, bool localdelivery = false) {

            MimeMessage mmsg = Message;
            if ((mmsg == default) || (mmsg.Headers == default)) return false;

            switch (place) {
                case Global.DirectoryPlace.Msg: {
                        (bool iscrypted, bool iscrypt) = await BodyFilterMsg(pgpauto).ConfigureAwait(false);
                        if (((iscrypted && iscrypt) || !iscrypted) && localdelivery && !string.IsNullOrWhiteSpace(mmsg.HtmlBody))
                            _ = await BodyFilterParse().ConfigureAwait(false);
                        return (iscrypted && iscrypt) || !iscrypted;
                    }
                case Global.DirectoryPlace.Out:
                        return await BodyFilterOut(place).ConfigureAwait(false);
            }
            return false;
        }

        #region private
        private void HeaderFilterOut() {

            MimeMessage mmsg = Message;
            if ((mmsg == default) || (mmsg.Headers == default)) return;
            try {
                mmsg.Headers.RemoveAll(HeaderId.Cc);
                mmsg.Headers.RemoveAll(HeaderId.Bcc);
                mmsg.Headers.RemoveAll(HeaderId.ContentReturn);
                mmsg.Headers.RemoveAll(HeaderId.X400ContentReturn);
                mmsg.Headers.RemoveAll(HeaderId.DispositionNotificationTo);
                mmsg.Headers.RemoveAll(HeaderId.DispositionNotificationOptions);
                mmsg.Headers.Remove(MailMessage.XConfirmReadingToId);
                mmsg.Headers.Remove(HeaderId.ReturnReceiptTo);
                mmsg.Headers.Remove(HeaderId.UserAgent);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(HeaderFilterOut), ex); }
        }
        private void HeaderFilterMsg() {

            MimeMessage mmsg = Message;
            if ((mmsg == default) || (mmsg.Headers == default)) return;
            try {
                mmsg.Headers.RemoveAll(HeaderId.DispositionNotificationTo);
                mmsg.Headers.RemoveAll(HeaderId.DispositionNotificationOptions);
                mmsg.Headers.Remove(MailMessage.XConfirmReadingToId);
                mmsg.Headers.Remove(HeaderId.ReturnReceiptTo);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(HeaderFilterOut), ex); }
        }

        private async Task<(bool, bool)> BodyFilterMsg(bool pgpauto) =>
            await Task.Run(async () => {

                bool iscrypt = false,
                     iscrypted = false;

                MimeMessage mmsg = Message;
                if (mmsg == default) return (iscrypted, iscrypt);

                if (mmsg.Body is MultipartEncrypted) {
                    iscrypted = true;
                    if (pgpauto)
                        try {
                            MailMessageCrypt crypt = new();
                            iscrypt = await crypt.Decrypt(mmsg);
                            if (iscrypt)
                                mmsg.Subject += " (PGP decoded)";
                        } catch (Exception ex) { Global.Instance.Log.Add(nameof(HeaderFilterOut), ex); }
                }
                return (iscrypted, iscrypt);
            });

        private async Task<bool> BodyFilterOut(Global.DirectoryPlace place) =>
            await Task<bool>.Run(async () => {

                MimeMessage mmsg = Message;
                if (mmsg == default) return false;

                if (Global.Instance.Config.IsSmtpAllOutPgpCrypt ||
                    Global.Instance.Config.IsSmtpAllOutPgpSign) {

                    MailMessageCrypt.Actions actions = MailMessageCrypt.Actions.None;
                    bool iscrypt = false;

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
                        return iscrypt;
                    }
                    catch (Exception ex) { Global.Instance.Log.Add(place.ToString(), ex); }
                    finally {
                        if (actions != MailMessageCrypt.Actions.None)
                            Global.Instance.Log.Add(nameof(MailMessageCrypt),
                                $"PGP message {place}/'{mmsg.MessageId}' status: {iscrypt}");
                    }
                }
                return false;
            });

        private async Task<bool> BodyFilterParse() =>
            await Task<bool>.Run(() => {
                try {
                    MimeMessage mmsg = Message;
                    if ((mmsg == default) || (mmsg.Headers == default)) return false;
                    BodyBuilder builder = new();
                    IEnumerable<MimeEntity> attachs = mmsg.Attachments;
                    if (attachs != null)
                        foreach (var a in attachs)
                            builder.Attachments.Add(a);

                    builder.HtmlBody = new ConverterHtmlToHtml().Convert(mmsg);
                    builder.TextBody = mmsg.TextBody;

                    mmsg.Body = builder.ToMessageBody();
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(BodyFilterParse), ex); }
                return true;
            });
        #endregion
    }
}

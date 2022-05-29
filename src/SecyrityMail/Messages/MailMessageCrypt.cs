/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MimeKit;
using MimeKit.Cryptography;
using Org.BouncyCastle.Bcpg.OpenPgp;
using SecyrityMail.GnuPG;

namespace SecyrityMail.Messages
{
    public class MailMessageCrypt
    {
        public enum Actions : int
        {
            None = 0,
            Sign,
            Encrypt,
            Decrypt,
            SignEncrypt
        }

        public bool CheckSigned(MimeMessage mmsg) => (mmsg != null) ? mmsg.Body is MultipartSigned : true;
        public bool CheckCrypted(MimeMessage mmsg) => (mmsg != null) ? mmsg.Body is MultipartEncrypted : true;

        #region SignEncrypt
        public async Task<bool> SignEncrypt(MimeMessage mmsg) =>
            await SignEncrypt(mmsg, (a) => LocalLogger(nameof(SignEncrypt), a)).ConfigureAwait(false);

        public async Task<bool> SignEncrypt(MimeMessage mmsg, Action<Exception> act) {
            try {
                using CryptGpgContext ctx = new CryptGpgContext();
                await mmsg.SignAndEncryptAsync(ctx)
                          .ConfigureAwait(false);
                return true;
            } catch (Exception ex) { act.Invoke(ex); }
            return false;
        }
        #endregion

        #region Encrypt
        public async Task<bool> Encrypt(MimeMessage mmsg) =>
            await Encrypt(mmsg, (a) => LocalLogger(nameof(Encrypt), a)).ConfigureAwait(false);

        public async Task<bool> Encrypt(MimeMessage mmsg, Action<Exception> act) {
            try {
                using CryptGpgContext ctx = new CryptGpgContext();
                mmsg.Body = await MultipartEncrypted.EncryptAsync(ctx, mmsg.To.Mailboxes, mmsg.Body)
                                                    .ConfigureAwait(false);
                return true;
            } catch (Exception ex) { act.Invoke(ex); }
            return false;
        }
        #endregion

        #region Decrypt
        public async Task<bool> Decrypt(MimeMessage mmsg) =>
            await Decrypt(mmsg, (a) => LocalLogger(nameof(Decrypt), a)).ConfigureAwait(false);

        public async Task<bool> Decrypt(MimeMessage mmsg, Action<Exception> act) =>
            await Task.Run(() => {
                try {
                    if (mmsg.Body is MultipartEncrypted body) {
                        mmsg.Body = body.Decrypt();
                        return true;
                    }
                } catch (Exception ex) { act.Invoke(ex); }
                return false;
            });
        #endregion

        #region Sign
        public async Task<bool> Sign(MimeMessage mmsg, PgpSecretKey key = default) =>
            await Sign(mmsg, key, (a) => LocalLogger(nameof(Sign), a)).ConfigureAwait(false);

        public async Task<bool> Sign(MimeMessage mmsg, Action<Exception> act) =>
            await Sign(mmsg, default, act).ConfigureAwait(false);

        public async Task<bool> Sign(MimeMessage mmsg, PgpSecretKey key, Action<Exception> act) {
            try {
                using CryptGpgContext ctx = new CryptGpgContext();
                if (key != default) {
                    mmsg.Body = await MultipartSigned.CreateAsync(ctx, key, DigestAlgorithm.Sha1, mmsg.Body)
                                                     .ConfigureAwait(false);
                    return true;
                } else {
                    MailboxAddress sender = mmsg.From.Mailboxes.FirstOrDefault();
                    if (sender == null) return false;
                    mmsg.Body = await MultipartSigned.CreateAsync(ctx, sender, DigestAlgorithm.Sha1, mmsg.Body)
                                                     .ConfigureAwait(false);
                    return true;
                }
            } catch (Exception ex) { act.Invoke(ex); }
            return false;
        }
        #endregion

        #region Decrypt Stream
        public async Task<MemoryStream> DecryptStream(MimeMessage mmsg) =>
            await Task.Run(async () => {
                using MemoryStream ms = new(Encoding.UTF8.GetBytes(mmsg.TextBody), false);
                return await DecryptStream(ms).ConfigureAwait(false);
            });

        public async Task<MemoryStream> DecryptStream(MemoryStream ms) =>
            await Task.Run(async () => {
                using CryptGpgContext ctx = new CryptGpgContext();
                MemoryStream msd = new MemoryStream();
                await ctx.DecryptToAsync(ms, msd).ConfigureAwait(false);
                msd.Position = 0;
                return msd;
            });
        #endregion

        private void LocalLogger(string tag, Exception ex)
        {
            if (ex is CertificateNotFoundException)
                Global.Instance.Log.Add(tag, "A certificate could not be found for the signer or one or more of the recipients.");
            else if (ex is PrivateKeyNotFoundException)
                Global.Instance.Log.Add(tag, "The private key could not be found for the sender.");
            else if (ex is PublicKeyNotFoundException)
                Global.Instance.Log.Add(tag, "The public key could not be found for one or more of the recipients.");
            else if (ex != null)
                Global.Instance.Log.Add(tag, ex);
        }
    }
}

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
using SecyrityMail.Data;

namespace SecyrityMail.Messages
{
    public class MailMessageCrypt
    {
        public bool CheckSigned(MimeMessage mmsg) => (mmsg != null) ? mmsg.Body is MultipartSigned : true;
        public bool CheckCrypted(MimeMessage mmsg) => (mmsg != null) ? mmsg.Body is MultipartEncrypted : true;
        public async Task SignEncrypt(MimeMessage mmsg) {
            try {
                using CryptPGContext ctx = new CryptPGContext();
                await mmsg.SignAndEncryptAsync(ctx)
                          .ConfigureAwait(false);
            }
            catch (Exception ex) { LocalLogger(nameof(SignEncrypt), ex); }
        }
        public async Task Encrypt(MimeMessage mmsg) {
            try {
                using CryptPGContext ctx = new CryptPGContext();
                mmsg.Body = await MultipartEncrypted.EncryptAsync(ctx, mmsg.To.Mailboxes, mmsg.Body)
                                                    .ConfigureAwait(false);
            }
            catch (Exception ex) { LocalLogger(nameof(Encrypt), ex); }
        }
        public async Task<bool> Decrypt(MimeMessage mmsg) =>
            await Task.Run(() => {
                try {
                    if (mmsg.Body is MultipartEncrypted body) {
                        mmsg.Body = body.Decrypt();
                        return true;
                    }
                }
                catch (Exception ex) { LocalLogger(nameof(Decrypt), ex); }
                return false;
            });

        public async Task<MemoryStream> DecryptStream(MimeMessage mmsg) =>
            await Task.Run(async () => {
                using MemoryStream ms = new(Encoding.UTF8.GetBytes(mmsg.TextBody), false);
                return await DecryptStream(ms).ConfigureAwait(false);
            });

        public async Task<MemoryStream> DecryptStream(MemoryStream ms) =>
            await Task.Run(async () => {
                using CryptPGContext ctx = new CryptPGContext();
                MemoryStream msd = new MemoryStream();
                await ctx.DecryptToAsync(ms, msd).ConfigureAwait(false);
                msd.Position = 0;
                return msd;
            });

        public async Task Sign(MimeMessage mmsg, PgpSecretKey key = default) {
            try {
                using CryptPGContext ctx = new CryptPGContext();
                if (key != default)
                    mmsg.Body = await MultipartSigned.CreateAsync(ctx, key, DigestAlgorithm.Sha1, mmsg.Body)
                                                     .ConfigureAwait(false);
                else {
                    MailboxAddress sender = mmsg.From.Mailboxes.FirstOrDefault();
                    if (sender == null) return;
                    mmsg.Body = await MultipartSigned.CreateAsync(ctx, sender, DigestAlgorithm.Sha1, mmsg.Body)
                                                     .ConfigureAwait(false);
                }
            } catch (Exception ex) { LocalLogger(nameof(Sign), ex); }
        }

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

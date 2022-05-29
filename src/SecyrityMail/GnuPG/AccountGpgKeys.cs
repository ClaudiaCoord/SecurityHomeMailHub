/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using MimeKit;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace SecyrityMail.GnuPG
{
    public class AccountGpgKeys : IDisposable
    {
        private CryptGpgContext context { get; set; } = default(CryptGpgContext);

        public long KeyId { get; set; } = -1;
        public int PublicKeyCount { get; set; } = 0;
        public bool IsSecretKey { get; set; } = false;
        public bool IsPublicKey { get; set; } = false;
        public bool IsSigningKey { get; set; } = false;
        public MailboxAddress EmailAddress { get; set; } = default;

        public bool IsEmpty => (PublicKeyCount == 0) || (KeyId == -1) || (EmailAddress == default);
        public bool CanImport => IsEmpty && !IsSecretKey && !IsSigningKey;
        public bool CanExport => !IsEmpty && IsPublicKey;
        public bool CanCreate => CanImport && !IsPublicKey && (EmailAddress != default);

        public AccountGpgKeys(MailboxAddress mb) => EmailAddress = mb;
        public AccountGpgKeys(string s) => EmailAddress = new MailboxAddress("", s);
        ~AccountGpgKeys() => Dispose();

        public void Clean() {
            KeyId = -1;
            PublicKeyCount = 0;
            IsSecretKey = IsPublicKey = IsSigningKey = false;
        }
        public void Build() {
            Clean();
            try {
                context = Instance();
                List<PgpSecretKey> slist = context.EnumerateSecretKeys(EmailAddress).ToList();
                if ((slist == null) || (slist.Count == 0)) return;
                foreach (PgpSecretKey psk in slist) {
                    IsSecretKey = (!IsSecretKey) ? (psk.IsMasterKey && !psk.IsPrivateKeyEmpty) : IsSecretKey;
                    IsSigningKey = (!IsSigningKey) ? (psk.IsSigningKey && !psk.IsPrivateKeyEmpty) : IsSigningKey;
                    IsPublicKey = (!IsPublicKey) ? (psk.PublicKey != null) : IsPublicKey;
                    KeyId = (KeyId <= 0L) ? psk.KeyId : KeyId;
                }
                if (IsSecretKey || IsSigningKey) {
                    List<PgpPublicKey> plist = context.EnumeratePublicKeys(EmailAddress).ToList();
                    if ((plist == null) || (plist.Count == 0)) return;
                    PublicKeyCount = plist.Count;
                }
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(AccountGpgKeys), ex); }
            finally {
                if (IsSecretKey || IsSigningKey)
                    Dispose();
            }
        }

        public async Task<bool> GenerateKeyPairAsync() {
            context = Instance();
            return await context.GenerateKeyPairAsync(EmailAddress).ConfigureAwait(false);
        }
        public async Task<bool> ExportAsync(string path) {
            context = Instance();
            return await context.ExportAsync(EmailAddress, path).ConfigureAwait(false);
        }
        public async Task<bool> ImportAsync(string path) {
            context = Instance();
            return await context.ImportAsync(path).ConfigureAwait(false);
        }

        public override string ToString() =>
            $"Keys -> Secret:{IsSecretKey}, Signing:{IsSigningKey}, Public:{IsPublicKey}/{PublicKeyCount}, Id:{KeyId}, = {!IsEmpty}";

        public void Dispose() {
            CryptGpgContext ctx = context;
            context = null;
            if (ctx != null)
                ctx.Dispose();
        }

        private CryptGpgContext Instance() {
            if (context == null)
                context = new CryptGpgContext();
            return context;
        }
    }
}

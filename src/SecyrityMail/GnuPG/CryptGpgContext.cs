/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.IO;
using System.Threading.Tasks;
using MimeKit;
using MimeKit.Cryptography;
using Org.BouncyCastle.Bcpg.OpenPgp;
using SecyrityMail.Utils;
using static SecyrityMail.Global;

namespace SecyrityMail.GnuPG
{
    public class CryptGpgContext : GnuPGContext, IDisposable
    {
        private CancellationTokenSafe tokenSafe = default;
        public readonly static string PgpConfigBody = "personal-cipher-preferences AES256 AES192 AES\n" +
            "personal-digest-preferences SHA512 SHA384 SHA256 SHA224\n" +
            "cert-digest-algo SHA512\n" +
            "default-preference-list SHA512 SHA384 SHA256 SHA224 AES256 AES192 AES CAST5 ZLIB BZIP2 ZIP Uncompressed\n" +
            // "default-new-key-algo rsa4096\n" +
            "keyid-format 0xlong\n";

		public CryptGpgContext() {
			if (!string.IsNullOrWhiteSpace(Global.Instance.Config.PgpKeyHost)) {
                KeyServer = new Uri(Global.Instance.Config.PgpKeyHost, UriKind.Absolute);
                AutoKeyRetrieve = true;
            }
        }
        ~CryptGpgContext() => Dispose();

        public new void Dispose() {
            if ((tokenSafe != default) && !tokenSafe.IsDisposed)
                tokenSafe.Dispose();
            base.Dispose();
        }

        public static string PgpUserPath { get; set; } = string.Empty;

        public static bool CheckInstalled()
		{
			string gnupg = string.Empty;
			try {
				do {
					gnupg = Environment.GetEnvironmentVariable("GNUPGHOME");
					if (gnupg == null) {
						if (Path.DirectorySeparatorChar == '\\')
							gnupg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gnupg");
						else
							gnupg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gnupg");
					}
					if (gnupg == null)
						break;

					if (!gnupg.EndsWith("gnupg"))
						gnupg = Path.Combine(gnupg, "gnupg");

					if (!File.Exists(Path.Combine(gnupg, "pubring.gpg")))
						break;
					if (!File.Exists(Path.Combine(gnupg, "secring.gpg")))
						break;

					var opgpcnf = Path.Combine(gnupg, "gpg.conf");
					if (!File.Exists(opgpcnf))
						File.WriteAllText(opgpcnf, CryptGpgContext.PgpConfigBody);

					if (!File.Exists(opgpcnf))
						break;

					CryptographyContext.Register(typeof(CryptGpgContext));
					PgpUserPath = gnupg;
					return true;

				} while (false);
			}
			catch (Exception ex) { Global.Instance.Log.Add(nameof(CheckInstalled), ex); }
			Global.Instance.Log.Add(nameof(CheckInstalled), $"You configuration OpenPG empty or not found: '{gnupg}'");
			return false;
		}

		protected override string GetPasswordForKey(PgpSecretKey key) =>
			Global.Instance.Config.PgpPassword;

		public static async Task<bool> ExportAccountToGpg(string pgpbin) =>
			await Task.Run(async () => {
                try {
					string path = Global.GetRootDirectory(DirectoryPlace.Export);
                    CryptGpgAccountsExport export = new CryptGpgAccountsExport();
                    return await export.WriteAll(path, pgpbin).ConfigureAwait(false);
                } catch (Exception ex) { Global.Instance.Log.Add("Gpg Account Export", ex); }
                return false;
            });

        public async Task<bool> GenerateKeyPairAsync(string s) =>
            await GenerateKeyPairAsync(new MailboxAddress("", s)).ConfigureAwait(false);

        public async Task<bool> GenerateKeyPairAsync(MailboxAddress mb) =>
            await Task.Run(() => {
				if ((mb == default) || string.IsNullOrEmpty(Global.Instance.Config.PgpPassword))
					return false;
                try {
                    GenerateKeyPair(mb, Global.Instance.Config.PgpPassword);
                    return true;
                } catch (Exception ex) { Global.Instance.Log.Add("Gpg Account Create", ex); }
                return false;
            });

        public async Task<bool> ExportAsync(string s, string path = default) =>
            await ExportAsync(new MailboxAddress("", s), path).ConfigureAwait(false);

        public async Task<bool> ExportAsync(MailboxAddress mb, string path = default) =>
            await Task.Run(async () => {
                if ((mb == default) || string.IsNullOrEmpty(Global.Instance.Config.PgpPassword))
                    return false;
                try {
                    if (string.IsNullOrWhiteSpace(path))
                        path = Path.Combine(
                            Global.GetRootDirectory(DirectoryPlace.Export),
                            $"{mb.Address.Replace('@', '-')}-public.key");

                    CheckToken();
                    using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                    await ExportAsync(new MailboxAddress[] { mb }, fs, true, tokenSafe.Token)
                         .ConfigureAwait(false);
                    return true;
                } catch (Exception ex) { Global.Instance.Log.Add("Gpg Account Export", ex); }
                return false;
            });

        public async Task<bool> ImportAsync(string path) =>
            await Task.Run(async () => {
                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(Global.Instance.Config.PgpPassword))
                    return false;
                try {
                    CheckToken();
                    using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    await ImportAsync(fs, tokenSafe.Token);
                    return true;
                } catch (Exception ex) { Global.Instance.Log.Add("Gpg Account Import", ex); }
                return false;
            });

        private void CheckToken() {
            if (tokenSafe == default)
                tokenSafe = new();
            if (tokenSafe.IsCancellationRequested)
                tokenSafe.Reload();
        }
    }
}

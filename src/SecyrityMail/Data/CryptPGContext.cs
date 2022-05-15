
using System.IO;
using System;
using MimeKit.Cryptography;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace SecyrityMail.Data
{
    public class CryptPGContext : GnuPGContext
    {
        public readonly static string PgpConfigBody = "personal-cipher-preferences AES256 AES192 AES\n" +
            "personal-digest-preferences SHA512 SHA384 SHA256 SHA224\n" +
            "cert-digest-algo SHA512\n" +
            "default-preference-list SHA512 SHA384 SHA256 SHA224 AES256 AES192 AES CAST5 ZLIB BZIP2 ZIP Uncompressed\n";

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
						File.WriteAllText(opgpcnf, CryptPGContext.PgpConfigBody);

					if (!File.Exists(opgpcnf))
						break;

					CryptographyContext.Register(typeof(CryptPGContext));
					return true;

				} while (false);
			}
			catch (Exception ex) { Global.Instance.Log.Add(nameof(CheckInstalled), ex); }
			Global.Instance.Log.Add(nameof(CheckInstalled), $"You configuration OpenPG empty or not found: '{gnupg}'");
			return false;
		}

		protected override string GetPasswordForKey(PgpSecretKey key) =>
            Global.Instance.Config.PgpPassword;
    }
}

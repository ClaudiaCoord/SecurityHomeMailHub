/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SecyrityMail.MailAccounts;

namespace SecyrityMail.GnuPG
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class CryptGpgAccountExport
    {
        private string _path = string.Empty;

        [XmlElement("name")]
        public string Name { get; set; } = string.Empty;
        [XmlElement("email")]
        public string Email { get; set; } = string.Empty;
        [XmlElement("replay")]
        public string ReplayTo { get; set; } = string.Empty;
        [XmlElement("path")]
        public string FilePathSerialize { get => _path; set => _path = value; }

        [XmlIgnore]
        public string FilePath { get => _path; set { _path = Path.Combine(value, $"{Email.Replace('@', '-')}.key-source"); } }

        [XmlIgnore]
        public string FileName => string.IsNullOrWhiteSpace(FilePath) ? string.Empty : Path.GetFileName(FilePath);

        [XmlIgnore]
        public bool IsEmpty => Name == string.Empty || Email == string.Empty || FilePath == string.Empty;

        public CryptGpgAccountExport() { }
        public CryptGpgAccountExport(UserAccount account, string path) {
            if ((account == null) || !account.Enable)
                return;
            Copy(account, path);
        }

        public void Copy(UserAccount account, string path) {
            if ((account == null) || !account.Enable)
                return;
            Name = account.Name;
            Email = account.Email;
            ReplayTo = account.ReplayTo;
            FilePath = path;
        }
    }

    public class CryptGpgAccountsExport
    {
        private static readonly string tag = "Export to GPG";
        private static readonly string execFormat = "{0}\\gpg2.exe --yes --batch --gen-key {1}";
        private static readonly string bodyFormat =
            "Key-Type: RSA\n" +
            "Key-Length: 4096\n" +
            "Subkey-Type: RSA\n" +
            "Subkey-Length: 2048\n" +
            "Name-Real: {0}\n" +
            "Name-Email: {1}\n" +
            "Expire-Date: 0\n" +
            "Passphrase: {2}\n";

        public CryptGpgAccountsExport() { }

        public async Task<bool> Write(CryptGpgAccountExport acc) =>
            await Task.Run(() => {
                try {
                    if ((acc == null) || acc.IsEmpty) return false;
                    File.WriteAllText(acc.FilePath,
                        string.Format(bodyFormat, acc.Name, acc.Email, Global.Instance.Config.PgpPassword));
                    return true;
                } catch (Exception ex) { Global.Instance.Log.Add(tag, ex); }
                return false;
            });

        public async Task<bool> WriteAll(string path, string pgpbin) =>
            await Task.Run(async () => {
                try {
                    if (Global.Instance.Accounts.Count == 0) return false;
                    int n = 0;
                    StringBuilder sb = new();
                    for (int i = 0; i < Global.Instance.Accounts.Count; i++) {
                        UserAccount acc = Global.Instance.Accounts[i];
                        if (acc.IsEmpty) {
                            Global.Instance.Log.Add(tag, $"skip export: {acc.Email}/{acc.Enable}");
                            continue;
                        }
                        CryptGpgAccountExport export = new (acc, path);
                        if (export.IsEmpty) {
                            Global.Instance.Log.Add(tag, $"skip export: {acc.Email}");
                            continue;
                        }
                        bool b = await Write(export).ConfigureAwait(false);
                        if (b) {
                            sb.AppendFormat(execFormat, pgpbin, export.FileName);
                            n++;
                        }
                    }
                    if (n > 0) {
                        Global.Instance.Log.Add(tag, $"Exported {n}, path: {path}");
                        File.WriteAllText(Path.Combine(path, "gpgImport.cmd"), sb.ToString());
                        return true;
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(tag, ex); }
                return false;
            });
    }
}

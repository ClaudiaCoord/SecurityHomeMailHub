/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;

namespace SecyrityMail.Proxy.SshProxy
{
    public class SshAccountConverter
    {
        private static readonly string[] _CredentialsUser = new string[] {
            "username"
        };
        private static readonly string[] _CredentialsPass = new string[] {
            "password"
        };
        private static readonly string[] _Expired = new string[] {
            "date expired", "expire on", "active for", "expired", "expire"
        };
        private static readonly string[] _Location = new string[] {
            "location"
        };
        private static readonly string[] _Host = new string[] {
            "server host", "ip addr", " ip ", "host", "server"
        };
        private static readonly string[] _Port = new string[] {
            "openssh", "port ssh", "openssh port"
        };
        private static readonly string[] _NotPort = new string[] {
            "dropbear", "squid", "ssl", "tls"
        };

        private string[] Lines { get; set; } = default(string[]);
        public SshAccount Account { get; } = new();

        public SshAccountConverter(string s) => ConvertBegin(s);

        public bool Convert(string s = default) {

            if (s != default) {
                Account.Clear();
                ConvertBegin(s);
            }

            foreach (string line in Lines) {
                try {
                    string[] tokens = line.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if ((tokens == null) || (tokens.Length < 2))
                        continue;

                    {
                        if (string.IsNullOrEmpty(Account.Login) &&
                            ParseString(tokens, _CredentialsUser, null, out string sout)) {
                            Account.Login = sout;
                            continue;
                        }
                    } {
                        if (string.IsNullOrEmpty(Account.Pass) &&
                            ParseString(tokens, _CredentialsPass, null, out string sout)) {
                            Account.Pass = sout;
                            continue;
                        }
                    } {
                        if (string.IsNullOrEmpty(Account.Host) &&
                            ParseString(tokens, _Host, _Location, out string sout)) {
                            Account.Host = sout;
                            continue;
                        }
                    } {
                        if (Account.Port <= 0) {
                            string sout;
                            if (ParseString(tokens, _Port, null, out sout) ||
                                ParseString(tokens, new string[] { "port" }, _NotPort, out sout)) {
                                if (ParsePort(sout, out int port)) {
                                    Account.Port = port;
                                    continue;
                                }
                            }
                        }
                    } {
                        if (string.IsNullOrEmpty(Account.Name) &&
                            ParseString(tokens, _Location, null, out string sout)) {
                            Account.Name = sout;
                            continue;
                        }
                    } {
                        if ((Account.Expired == DateTime.MinValue) &&
                            ParseExpire(tokens, _Expired, out DateTime dout)) {
                            Account.Expired = dout;
                            continue;
                        }
                    }
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(SshAccountConverter), ex); }
            }
            if (Account.IsEmptyName) {
                if (!string.IsNullOrWhiteSpace(Account.Host))
                    Account.Name = Account.Host;
                else
                    Account.Name = $"{DateTime.Now:MM-dd-HH-mm-ss}";
            }
            if (!Account.IsEmptyNoCheckTypeAndPort)
                Account.Type = ProxyType.SshSock5;
            if (Account.Port <= 0)
                Account.Port = 22;
            return !Account.IsEmpty;
        }

        private void ConvertBegin(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s));
            Lines = s.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (Lines == null)
                throw new ArgumentNullException(nameof(Lines));
        }

        private bool ParseExpire(string[] ss, string[] strings, out DateTime dout) {

            if ((ss != null) && (ss.Length >= 2) && !string.IsNullOrWhiteSpace(ss[0]) && !string.IsNullOrWhiteSpace(ss[1])) {

                string s1 = ss[0].Trim().ToLower();
                foreach (string token in strings) {
                    if (s1.Contains(token)) {
                        string s2 = ss[1].Trim(new char[] {'\r', '\n', '\t', ' ', '.', ','});
                        if ((s2.IndexOf('-') >= 0) || (s2.IndexOf('/') >= 0)) {
                            if (DateTime.TryParse(s2, out DateTime dt)) {
                                dout = dt;
                                return true;
                            }
                        }
                    }
                }
            }
            dout = DateTime.MinValue;
            return false;
        }

        private bool ParsePort(string s, out int iout) {

            if (!string.IsNullOrWhiteSpace(s)) {
                s = s.Trim();
                if ((s.IndexOf(',') != -1) || (s.IndexOf(' ') != -1)) {
                    string[] ss = s.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string token in ss) {
                        string t = token.Trim();
                        if (!string.IsNullOrWhiteSpace(t) && int.TryParse(t, out int port)) {
                            iout = port;
                            return true;
                        }
                    }
                } else {
                    if (int.TryParse(s, out int port)) {
                        iout = port;
                        return true;
                    }
                }
            }
            iout = -1;
            return false;
        }

        private bool ParseString(string[] ss, string[] strings, string[] notstrings, out string sout) {

            if ((ss != null) && (ss.Length >= 2) && !string.IsNullOrWhiteSpace(ss[0]) && !string.IsNullOrWhiteSpace(ss[1])) {

                string s = ss[0].Trim().ToLower();
                foreach (string token in strings) {
                    if (s.Contains(token)) {
                        if (!ParseFound(s, notstrings)) {
                            sout = ss[1].Trim();
                            return true;
                        }
                    }
                }
            }
            sout = string.Empty;
            return false;
        }

        private bool ParseFound(string s, string[] strings) {
            if ((strings == null) || (strings.Length == 0))
                return false;
            foreach (string token in strings)
                if (s.Contains(token)) return true;
            return false;
        }
    }
}

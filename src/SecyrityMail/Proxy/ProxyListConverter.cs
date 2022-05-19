/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SecyrityMail.Proxy
{
    public class ProxyListConverter
    {
        private static readonly string SpysmeProxyUrl = "https://spys.me/proxy.txt";
        private static readonly string SpysmeSocksUrl = "https://spys.me/socks.txt";
        private readonly object __lock = new();
        private bool IsRunning = false;

        #region All Build
        public async Task<bool> AllBuild()
        {

            if (IsRunning)
                return false;
            IsRunning = true;

            return await Task.Run(async () => {
                try
                {
                    _ = await SpysMeConvert();
                    foreach (ProxyType t in MailProxy.SelectableProxyTypeList)
                        _ = await HidemyConvert(
                            Global.GetRootFile(Global.DirectoryPlace.Proxy, MailProxy.Files[(int)t]))
                                  .ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(AllBuild), ex); }
                finally { IsRunning = false; }
                return false;
            });
        }
        #endregion

        #region Check Build
        public async Task<bool> CheckBuild(ProxyType type) {

            if (IsRunning)
                return false;
            IsRunning = true;

            return await Task.Run(() => {
                try {
                    DateTime dt = DateTime.Now;
                    switch (type)
                    {
                        case ProxyType.Http:
                        case ProxyType.Https:
                        case ProxyType.Sock4:
                        case ProxyType.Sock5: return IsFileUpdate(type, dt);
                        case ProxyType.All: {
                                foreach (ProxyType t in MailProxy.SelectableProxyTypeList)
                                    if (!IsFileUpdate(t, dt)) return false;
                                break;
                            }
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(AllBuild), ex); return false; }
                finally { IsRunning = false; }
                return true;
            });
        }
        #endregion

        #region Convert from Url: https://hidemy.name/ru/proxy-list/
        public async Task<bool> HidemyConvert(string fileOut) =>
            await Task.Run(() => {
                try {
                    if (string.IsNullOrEmpty(fileOut))
                        return false;

                    string fileIn = fileOut.Replace(".list", ".in");
                    FileInfo fin = new(fileIn);
                    if ((fin == null) || !fin.Exists || (fin.Length == 0L))
                        return false;

                    List<string> inlist = ReadData(fin.FullName);
                    if ((inlist == null) || (inlist.Count == 0))
                        return false;

                    List<string> list = new List<string>();
                    Regex r1 = new Regex(@"^((\S+)\s(\d+)).*$",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline |
                        RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant);

                    foreach (string s in inlist) {
                        if (s.StartsWith("HTTP") || s.StartsWith("SOCKS"))
                            continue;

                        MatchCollection m = r1.Matches(s);
                        if ((m.Count > 0) && (m[0].Groups.Count == 4))
                            list.Add($"{m[0].Groups[2].Value}:{m[0].Groups[3].Value}");
                    }
                    if (list.Count > 0) {
                        FileTruncate(fin.FullName);
                        return SpliceData(fileOut, list);
                    }
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(HidemyConvert), ex); }
                return false;
            });

        public async Task<List<string>> HidemyConvert(string src, List<string> list) =>
            await Task<List<string>>.Run(() => {
                try
                {
                    if (string.IsNullOrEmpty(src))
                        return new();

                    Regex r1 = new Regex(@"^((\S+)\s(\d+)).*$",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline |
                        RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant);

                    string [] ss = src.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if ((ss == null) || (ss.Length == 0))
                        return new();

                    foreach (string s in ss) {
                        if (s.StartsWith("HTTP") || s.StartsWith("SOCKS"))
                            continue;

                        MatchCollection m = r1.Matches(s);
                        if ((m.Count > 0) && (m[0].Groups.Count == 4))
                            list.Add($"{m[0].Groups[2].Value}:{m[0].Groups[3].Value}");
                    }
                    if (list.Count > 0)
                        return list.Distinct().ToList();
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(HidemyConvert), ex); }
                return new();
            });
        #endregion

        #region Convert from Url: https://spys.me/proxy.txt & https://spys.me/socks.txt
        public async Task<bool> SpysMeConvert(ProxyType type = ProxyType.All) =>
            await Task.Run(() => {
                try {
                    using HttpClient client = new HttpClient();

                    if ((type == ProxyType.Http) || (type == ProxyType.Https) || (type == ProxyType.All)) {
                        string s1 = client.GetStringAsync(SpysmeProxyUrl).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (!string.IsNullOrWhiteSpace(s1))
                            _ = SpysMeConvert_(ProxyType.Http, s1)
                               .ConfigureAwait(false)
                               .GetAwaiter()
                               .GetResult();
                    }
                    if ((type == ProxyType.Sock4) || (type == ProxyType.Sock5) || (type == ProxyType.All)) {
                        string s2 = client.GetStringAsync(SpysmeSocksUrl).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (!string.IsNullOrWhiteSpace(s2))
                            _ = SpysMeConvert_(ProxyType.Sock5, s2)
                               .ConfigureAwait(false)
                               .GetAwaiter()
                               .GetResult();
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(SpysMeConvert), ex); }
                return false;
            });

        private async Task<bool> SpysMeConvert_(ProxyType type, string text) =>
            await Task.Run(() => {
                try {
                    string[] ain = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    if ((ain == null) || (ain.Length == 0))
                        return false;

                    Regex r = new(@"^(?<ip>.+):(?<port>\d+)\s?(?<country>\w{2})-(?<anon>\w)-?(?<type>:?\w?)",
                        RegexOptions.Multiline | RegexOptions.IgnoreCase |
                        RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant);

                    bool b = false;
                    List<string> httpList = new List<string>();
                    List<string> httpsList = new List<string>();
                    foreach (string line in ain) {

                        MatchCollection matches = r.Matches(line);
                        if (matches.Count > 0) {

                            string[] names = r.GetGroupNames();
                            foreach (Match m in matches) {
                                string ip = string.Empty,
                                       port = string.Empty,
                                       country = string.Empty,
                                       anon = string.Empty,
                                       type = string.Empty;

                                foreach (string name in names) {
                                    if (name.Equals("ip"))
                                        ip = m.Groups[name].Value;
                                    else if (name.Equals("port"))
                                        port = m.Groups[name].Value;
                                    else if (name.Equals("country"))
                                        country = m.Groups[name].Value;
                                    else if (name.Equals("anon"))
                                        anon = m.Groups[name].Value;
                                    else if (name.Equals("type"))
                                        type = m.Groups[name].Value;
                                }
                                if (!string.IsNullOrWhiteSpace(ip) && "H".Equals(anon) && !"RU".Equals(country)) {
                                    if ("S".Equals(type))
                                        httpsList.Add($"{ip}:{port}");
                                    else
                                        httpList.Add($"{ip}:{port}");
                                }
                            }
                        }
                    }
                    if ((type == ProxyType.Http) || (type == ProxyType.Https)) {
                        if (httpList.Count > 0)
                            b = SpliceData(
                                Global.GetRootFile(
                                    Global.DirectoryPlace.Proxy, MailProxy.Files[(int)ProxyType.Http]), httpList);
                        if (httpsList.Count > 0)
                            b = SpliceData(
                                Global.GetRootFile(
                                    Global.DirectoryPlace.Proxy, MailProxy.Files[(int)ProxyType.Https]), httpsList);
                    }
                    else if ((type == ProxyType.Sock4) || (type == ProxyType.Sock5)) {
                        if (httpsList.Count > 0)
                            httpList.AddRange(httpsList);
                        b = SpliceData(
                            Global.GetRootFile(
                                Global.DirectoryPlace.Proxy, MailProxy.Files[(int)type]), httpList);
                    }
                    return b;
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(SpysMeConvert), ex); }
                return false;
            });
        #endregion

        #region private
        private bool SpliceData(string sout, List<string> list) {
            try {
                lock (__lock) {
                    using FileStream fs = File.Open(sout, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    byte[] bytes;
                    if (fs.Length > 0) {
                        bytes = new byte[fs.Length];
                        int i = fs.Read(bytes, 0, bytes.Length);
                        if (i > 0) {
                            string[] aout = Encoding.UTF8.GetString(bytes, 0, i)
                                                         .Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if ((aout != null) && (aout.Length > 0))
                                list.AddRange(aout);
                        }
                        fs.Seek(0, SeekOrigin.Begin);
                    }
                    bytes = Encoding.UTF8.GetBytes(string.Join(
                        Environment.NewLine, list.Distinct().OrderBy(q => q).ToArray()));
                    if ((bytes != null) && (bytes.Length > 0))
                        fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                    Global.Instance.Log.Add(nameof(ProxyListConverter), $"update proxy list: {Path.GetFileNameWithoutExtension(sout)} / {fs.Length}");
                }
                return true;
#if DEBUG
            } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex);  }
#else
            } catch (Exception ex) { Global.Instance.Log.Add(nameof(SpliceData), ex); }
#endif
            return false;
        }
        private List<string> ReadData(string sout)
        {
            try {
                lock (__lock) {
                    using FileStream fs = File.Open(sout, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (fs.Length == 0)
                        return new();

                    byte[] bytes = new byte[fs.Length];
                    int i = fs.Read(bytes, 0, bytes.Length);
                    if (i > 0) {
                        return Encoding.UTF8.GetString(bytes, 0, i)
                                            .Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                            .ToList();
                    }
                }
#if DEBUG
            } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
#else
            } catch (Exception ex) { Global.Instance.Log.Add(nameof(SpliceData), ex); }
#endif
            return new();
        }

        private void FileTruncate(string s) {
            lock (__lock)
                try { using FileStream fs = new FileStream(s, FileMode.Truncate, FileAccess.Write); } catch { }
        }

        private bool IsFileUpdate(ProxyType type, DateTime dt) {
            FileInfo f = new(Global.GetRootFile(Global.DirectoryPlace.Proxy, MailProxy.Files[(int)type]));
            return !(!f.Exists || (f.Length == 0) || (f.LastWriteTime.AddDays(1) < dt));
        }
        #endregion
    }
}

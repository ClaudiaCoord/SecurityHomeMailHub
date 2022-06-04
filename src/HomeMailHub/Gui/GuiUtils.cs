/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MimeKit;
using NStack;
using SecyrityMail;
using Terminal.Gui;
using RES = HomeMailHub.Properties.Resources;

namespace HomeMailHub.Gui
{
    internal class ReadMessageData
    {
        public MimeMessage Message { get; set; } = default;
        public FileInfo Info { get; set; } = default;
        public bool IsEmpty => Message == null || Info == null;
    }
    internal static class GuiUtils
    {
        private static readonly string[] urlTools = new string[] { "https://webqr.com/" };

        public static void IDisposableObject<T>(this Type type, T val, BindingFlags bf = BindingFlags.Default) where T : class {
            try {
                bf = (bf == BindingFlags.Default) ? BindingFlags.Instance | BindingFlags.NonPublic : bf;
                foreach (PropertyInfo pi in val.GetType().GetProperties(bf)) {
                    var a = pi.GetValue(val);
                    if (a != null)
                        a.GetType().IDisposableObject_(a, pi.Name);
                }
            } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"{nameof(IDisposableObject)}: {ex}"); }
        }

        private static void IDisposableObject_<T>(this Type type, T val, string name) where T : class {
            try {
                if (val == default) return;
                InterfaceMapping m = type.GetInterfaceMap(typeof(IDisposable));
                MethodInfo mi = type.GetMethod(nameof(IDisposable.Dispose), new Type[0]);
                if ((mi != default) && (m.TargetMethods.Length > 0) && (mi == m.TargetMethods[0])) {
#                   if DEBUG
                    System.Diagnostics.Debug.WriteLine($"{type.Name} - {name}");
#                   endif
                    try { ((IDisposable)val).Dispose(); } catch { }
                }
            } catch { }
        }

        public static async Task<MenuItem[]> LoadMenuUrls(this string s) =>
            await Task.Run(() => {
                try
                {
                    string[] ss = File.ReadAllLines(
                        Global.GetRootFile(Global.DirectoryPlace.Root,
                        $"{s}.urls"));

                    if ((ss == default) || (ss.Length == 0))
                        return default;

                    int n = 0;
                    MenuItem[] mitems = new MenuItem[ss.Length + urlTools.Length + 1];
                    for (int i = 0; i < ss.Length; i++) {
                        Uri uri = new Uri(ss[i]);
                        mitems[n++] = new MenuItem(
                            uri.DnsSafeHost, "", () => GuiUtils.BrowseUri(uri));
                    }
                    mitems[n++] = null;
                    for (int i = 0; i < urlTools.Length; i++) {
                        Uri uri = new Uri(urlTools[i]);
                        mitems[n++] = new MenuItem(
                            uri.DnsSafeHost, "", () => GuiUtils.BrowseUri(uri));
                    }
                    return mitems;
                } catch { }
                return default;
            });

        public static async Task<ReadMessageData> ReadMessage(this string s) =>
            await Task<ReadMessageData>.Run(async () => {
                try {
                    if (string.IsNullOrWhiteSpace(s)) {
                        Application.MainLoop.Invoke(() =>
                            _ = MessageBox.ErrorQuery(50, 7,
                            RES.UTILS_TXT1,
                            $"{RES.UTILS_TXT1}, {RES.UTILS_ABORT}", RES.TAG_OK));
                        return new ReadMessageData();
                    }
                    FileInfo f = new(s);
                    if ((f == null) || !f.Exists) {
                        Application.MainLoop.Invoke(() =>
                            _ = MessageBox.ErrorQuery(50, 7,
                            RES.UTILS_TXT2,
                            $"{RES.UTILS_TXT2}, {RES.UTILS_ABORT}", RES.TAG_OK));
                        return new ReadMessageData();
                    }
                    if (f.Length == 0) {
                        Application.MainLoop.Invoke(() =>
                            _ = MessageBox.ErrorQuery(50, 7,
                            RES.UTILS_TXT3,
                            $"{RES.UTILS_TXT3}, {RES.UTILS_ABORT}", RES.TAG_OK));
                        return new ReadMessageData();
                    }
                    MimeMessage mmsg = await MimeMessage.LoadAsync(f.FullName)
                                                        .ConfigureAwait(false);
                    if (mmsg == null) {
                        Application.MainLoop.Invoke(() =>
                            _ = MessageBox.ErrorQuery(50, 7,
                            RES.UTILS_TXT4,
                            $" {RES.UTILS_TXT4}, {RES.UTILS_ABORT}", RES.TAG_OK));
                        return default;
                    }
                    return new ReadMessageData() { Message = mmsg, Info = f };
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"{nameof(IDisposableObject)}: {ex}"); }
                return new ReadMessageData();
            });

        public static InternetAddressList EmailParse(this string s) {

            Regex r = new(@"\""(?<tag>[\w\s-_.] +)\""\s\<(?<addr>[\w@.-_]+)\>,?|(?<addr>[\w@.-_]+),?",
                RegexOptions.Singleline | RegexOptions.IgnoreCase |
                RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant);

            InternetAddressList list = new InternetAddressList();
            MatchCollection matches = r.Matches(s);
            if (matches.Count > 0) {
                string[] names = r.GetGroupNames();
                foreach (Match m in matches) {

                    string addr = string.Empty,
                           tag = string.Empty;

                    foreach (string name in names) {
                        if (name.Equals("addr"))
                            addr = m.Groups[name].Value;
                        else if (name.Equals("tag"))
                            tag = m.Groups[name].Value;
                    }
                    if (!string.IsNullOrWhiteSpace(addr)) {
                        if (!string.IsNullOrWhiteSpace(tag))
                            list.Add(new MailboxAddress(tag, addr));
                        else
                            list.Add(new MailboxAddress("", addr));
                    }
                }
            }
            return list;
        }

        public static string FirstLetter(this string s) {
            string ss = s.ClearText();
            return char.ToUpper(ss[0]) + ss.Substring(1);
        }

        public static string ClearText(this string s) =>
            s.Replace("_", "");

        public static ustring ClearText(this ustring s) =>
            s.Replace("_", "");

        public static ustring GetListTitle(this string a, int i) =>
            GetListTitle(a, RES.TAG_ACCOUNTS, i);

        public static ustring GetListTitle(this string a, string b, int i) =>
            string.Format("{0}{1} : {2}", string.IsNullOrWhiteSpace(a) ? "" : $"{a} - ", b, i);

        public static string HumanizeClassName(this string s) {

            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            if (s.IndexOf(' ') != -1) return s;
            int end = s.Length - 1;
            StringBuilder sb = new();
            for (int i = 0; i < s.Length; i++) {
                int n = i + 1;
                bool b = (i > 0) && char.IsUpper(s[i]) && (n < s.Length) && !char.IsUpper(s[n]) && (i != end);
                sb.Append(b ? $" {s[i]}" : s[i]);
            }
            return sb.ToString();
        }

        public static void BrowseUri(this Uri uri) {
            try {
                System.Diagnostics.Process.Start(uri.AbsoluteUri);
            } catch (Exception ex) { Global.Instance.Log.Add(nameof(BrowseUri), ex); }
        }

        public static void BrowseFile(this string s) {
            try {
                System.Diagnostics.Process.Start(s);
            } catch (Exception ex) { Global.Instance.Log.Add(nameof(BrowseFile), ex); }
        }

        public static void StatusBarError(this Exception ex) => StatusBarText(ex.Message);
        public static void StatusBarText(this string s) {
            if (GuiRootStatusBar.Get != null)
                Application.MainLoop.Invoke(() => {
                    try { GuiRootStatusBar.Get.UpdateStatus<string>(GuiStatusItemId.Error, s); } catch {}});
        }
    }
}

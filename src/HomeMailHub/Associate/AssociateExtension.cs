/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using SecyrityMail.Messages;

namespace HomeMailHub.Associate
{
    internal class AssociateExtension
    {
        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr h, string m, string c, int type);

        public void ShowError(string s) =>
            MessageBox((IntPtr)0, s, "Run Associate file extension", 0);

        public (bool, string) Parse(string [] ss) {
            bool[] b = new bool[2];
            do {
                string ext = Path.GetExtension(ss[ss.Length - 1]);
                if (string.IsNullOrWhiteSpace(ext))
                    break;
                b[0] = ext.Equals(".eml", StringComparison.InvariantCultureIgnoreCase);
                b[1] = !b[0] ? ext.Equals(".msg", StringComparison.InvariantCultureIgnoreCase) : false;
                if (!b[0] && !b[1])
                    break;
                try {
                    string path = (ss.Length == 1) ? ss[0] : string.Join(" ", ss);
                    if (b[0])
                        return (true, path);
                    else if (b[1]) {
                        MailMessage msg = new();
                        msg.Load(path).Wait();
                        return (true, msg.FilePath);
                    }
                } catch (Exception ex) { ShowError(ex.Message); }
            } while (false);
            return (b[0] || b[1], string.Empty);
        }
    }
}

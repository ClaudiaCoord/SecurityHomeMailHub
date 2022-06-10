/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SecyrityMail;

namespace HomeMailHub.Associate
{
    internal class AssociateFileExtension
    {
        public string AppExe { get; private set; }
        public string AppPath { get; private set; }
        public string AppIcon { get; private set; }
        public string Ext { get; private set; }
        private string [] KeysString;

        public AssociateFileExtension(string ext) {
            Ext = ext;
            AppPath = Assembly.GetExecutingAssembly().Location;
            AppExe = Path.GetFileName(AppPath);
            AppIcon = Path.Combine(Path.GetDirectoryName(AppPath), $"{Path.GetFileNameWithoutExtension(AppExe)}.png");
            KeysString = new string[] {
                /* 0  */ $@"Software\Classes\.{Ext}",
                /* 1  */ $@"Software\Classes\Applications",
                /* 2  */ $@"Software\Classes\Applications\{AppExe}",
                /* 3  */ $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.{Ext}",
                /* 4  */ $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.{Ext}\UserChoice",
                /* 5  */ $@"Applications\{AppExe}",
                /* 6  */ @"shell\open\command",
                /* 7  */ @"shell\edit\command",
                /* 8  */ "DefaultIcon",
                /* 9  */ "PerceivedType",
                /* 10  */ "UserChoice",
                /* 11 */ "Progid"
            };
        }

        public bool IsAssociated() {
            RegistryKey AppReg = null, AppAssoc = null;
            try {
                do {
                    if ((AppReg = Registry.CurrentUser.OpenSubKey(KeysString[2], false)) == null)
                        break;
                    if ((AppAssoc = Registry.CurrentUser.OpenSubKey(KeysString[4], false)) == null)
                        break;
                    var obj = AppAssoc.GetValue(KeysString[11]);
                    if ((obj is string s) && KeysString[5].Equals(s))
                        return true;
                } while (false);
            } catch { }
            finally {
                if (AppReg != null)
                    AppReg.Dispose();
                if (AppAssoc != null)
                    AppAssoc.Dispose();
            }
            return false;
        }

        public bool Associate() {
            RegistryKey FileReg = null, AppReg = null, AppAssoc = null;
            try {
                (FileReg, AppReg, AppAssoc) = CreateRegistry();

                if ((FileReg == null) || (FileReg == null) || (FileReg == null))
                    return false;

                FileReg.CreateSubKey(KeysString[8]).SetValue("", AppIcon);
                FileReg.CreateSubKey(KeysString[9]).SetValue("", "Text");

                AppReg.CreateSubKey(KeysString[6]).SetValue("", $@"""{AppPath}"" ""%1""");
                AppReg.CreateSubKey(KeysString[7]).SetValue("", $@"""{AppPath}"" ""%1""");
                AppReg.CreateSubKey(KeysString[8]).SetValue("", AppIcon);

                AppAssoc.CreateSubKey(KeysString[10]).SetValue(KeysString[11], KeysString[5]);
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(Associate), ex); }
            finally {
                if (FileReg != null)
                    FileReg.Dispose();
                if (AppReg != null)
                    AppReg.Dispose();
                if (AppAssoc != null)
                    AppAssoc.Dispose();
            }
            return false;
        }

        public bool DeAssociate() {
            RegistryKey FileReg = null, AppReg = null, AppAssoc = null;
            try {
                (FileReg, AppReg, AppAssoc) = GetRegistry();

                if (FileReg != null) {
                    FileReg.DeleteSubKeyTree(KeysString[8], false);
                    FileReg.DeleteSubKeyTree(KeysString[9], false);
                }
                if (AppReg != null)
                    AppReg.DeleteSubKeyTree(AppExe, false);
                if (AppAssoc != null)
                    AppAssoc.DeleteSubKeyTree(KeysString[10], false);
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                return true;
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(DeAssociate), ex); }
            finally {
                if (FileReg != null)
                    FileReg.Dispose();
                if (AppReg != null)
                    AppReg.Dispose();
                if (AppAssoc != null)
                    AppAssoc.Dispose();
            }
            return false;
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        #region private
        private (RegistryKey, RegistryKey, RegistryKey) CreateRegistry() =>
            (Registry.CurrentUser.CreateSubKey(KeysString[0]),
             Registry.CurrentUser.CreateSubKey(KeysString[2]),
             Registry.CurrentUser.CreateSubKey(KeysString[3]));

        private (RegistryKey, RegistryKey, RegistryKey) GetRegistry() =>
            (Registry.CurrentUser.OpenSubKey(KeysString[0], true),
             Registry.CurrentUser.OpenSubKey(KeysString[1], true),
             Registry.CurrentUser.OpenSubKey(KeysString[3], true));
        #endregion
    }
}

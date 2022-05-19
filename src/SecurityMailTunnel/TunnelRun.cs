/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecurityMailTunnel
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using VPN.WireGuard;

namespace SecurityMailTunnel
{
    internal class TunnelRun
    {
        [STAThread]
        static void Main(string[] args) {

            if ((args == null) || (args.Length != 3) || !"/service".Equals(args[0]))
                return;

            if (string.IsNullOrEmpty(args[1]))
                return;

            FileInfo f = new(args[1].Trim());
            if ((f == default) || !f.Exists)
                return;

            Thread th = new(() => {
                try {
                    if (!int.TryParse(args[2], out int pid))
                        return;
                    Process.GetProcessById(pid).WaitForExit();
                    VpnService.Remove(f.FullName, false);
                } catch { }
            });
            try {
                th.Start();
                VpnService.Run(f.FullName);
                VpnService.Remove(f.FullName, false);
                th.Interrupt();
            }
            catch { if (th.IsAlive) try { th.Interrupt(); } catch {}}
            finally {
                if (f != default) {
                    f.Refresh();
                    if (f.Exists)
                        f.Delete();
                }
            }
        }
    }
}

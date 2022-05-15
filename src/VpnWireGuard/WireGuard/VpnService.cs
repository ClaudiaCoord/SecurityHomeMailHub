
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace VPN.WireGuard
{
    [Guid("35714A43-2DE6-464E-8F37-E32C44567D95")]
    public static class VpnService
    {
        private const string InfoName = "Mail security VPN";
        private const string InfoDescription = "Mail security VPN WireGuard tunnel";

        public static string GetVpnId(string configFile) => Path.GetFileNameWithoutExtension(configFile);
        public static string GetShortName(string configFile) => $"WireGuardTunnel${GetVpnId(configFile)}";
        public static VpnDriver.VpnAdapter GetAdapter(string configFile) =>
            new VpnDriver.VpnAdapter(GetVpnId(configFile));

        public static void Run(string configFile) => VpnDriver.Run(configFile);

        public static void Add(string configFile, bool ephemeral)
        {
            string NameId = GetShortName(configFile),
                   NameExe = Path.Combine(
                       Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
                       "SecurityMailTunnel.exe"),
                   ExeArgs = string.Format(
                       "\"{0}\" /service \"{1}\" {2}", NameExe, configFile,
                       Process.GetCurrentProcess().Id);

            /*
            Debug.WriteLine(ExeArgs);
            Debug.WriteLine(Environment.CurrentDirectory);
            Debug.WriteLine(Assembly.GetExecutingAssembly().Location);
            */

            IntPtr scm = VpnWin32.OpenSCManager(null, null, VpnWin32.ScmAccessRights.AllAccess);
            if (scm == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                IntPtr service = VpnWin32.OpenService(scm, NameId, VpnWin32.ServiceAccessRights.AllAccess);
                if (service != IntPtr.Zero)
                {
                    VpnWin32.CloseServiceHandle(service);
                    Remove(configFile, true);
                }
                service = VpnWin32.CreateService(
                    scm, NameId, InfoName,
                    VpnWin32.ServiceAccessRights.AllAccess,
                    VpnWin32.ServiceType.Win32OwnProcess,
                    VpnWin32.ServiceStartType.Demand,
                    VpnWin32.ServiceError.Normal,
                    ExeArgs, null, IntPtr.Zero, "Nsi\0TcpIp", null, null);
                if (service == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                try
                {
                    VpnWin32.ServiceSidType sidType = VpnWin32.ServiceSidType.Unrestricted;
                    if (!VpnWin32.ChangeServiceSidConfig2(service, VpnWin32.ServiceConfigType.SidInfo, ref sidType))
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    VpnWin32.ServiceDescription description = new VpnWin32.ServiceDescription { lpDescription = InfoDescription };
                    if (!VpnWin32.ChangeServiceDescriptionConfig2(service, VpnWin32.ServiceConfigType.Description, ref description))
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    if (!VpnWin32.StartService(service, 0, null))
                        throw new Win32Exception(Marshal.GetLastWin32Error());

                    if (ephemeral && !VpnWin32.DeleteService(service))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                finally
                {
                    VpnWin32.CloseServiceHandle(service);
                }
            }
            finally
            {
                VpnWin32.CloseServiceHandle(scm);
            }
        }

        public static void Remove(string configFile, bool waitForStop)
        {
            IntPtr scm = VpnWin32.OpenSCManager(null, null, VpnWin32.ScmAccessRights.AllAccess);
            if (scm == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                IntPtr service = VpnWin32.OpenService(scm, GetShortName(configFile), VpnWin32.ServiceAccessRights.AllAccess);
                if (service == IntPtr.Zero)
                {
                    VpnWin32.CloseServiceHandle(service);
                    return;
                }
                try
                {
                    VpnWin32.VpnServiceStatus serviceStatus = new VpnWin32.VpnServiceStatus();
                    VpnWin32.ControlService(service, VpnWin32.ServiceControl.Stop, serviceStatus);

                    for (int i = 0;
                        (waitForStop && (i < 180) &&
                         VpnWin32.QueryServiceStatus(service, serviceStatus) &&
                         serviceStatus.dwCurrentState != VpnWin32.ServiceState.Stopped); ++i)
                        Thread.Sleep(1000);

                    if (!VpnWin32.DeleteService(service) && Marshal.GetLastWin32Error() != 0x00000430)
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                finally
                {
                    VpnWin32.CloseServiceHandle(service);
                }
            }
            finally
            {
                VpnWin32.CloseServiceHandle(scm);
            }
        }
    }
}

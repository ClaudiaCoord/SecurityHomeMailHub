/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SecyrityMail.Utils;

namespace SecyrityMail.Vpn.RouteTable
{
    public class NetRouteTable
    {
        public NetRouteEntry RouteEntry { get; private set; } = default(NetRouteEntry);
        private string GetCmdFile(bool b) => b ? "RouteAdd.cmd" : "RouteDelete.cmd";
        private string GetMessage(NetRouteEntry entry, int x) =>
            (entry == null) ? $"status = {x}" :
                $"status = {x}: {entry.DestinationNet}/{entry.DestinationMask} to: {entry.GatewayIP} metric: {entry.Metric}";

        public void CreateRoute() {
            try {
                Init();
                int x = RouteActions(RouteEntry, true);
                Global.Instance.Log.Add(nameof(CreateRoute), GetMessage(RouteEntry, x));
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(CreateRoute), ex); }
        }
        public void DeleteRoute() {
            try {
                if ((RouteEntry != null) && !RouteEntry.IsEmpty) {
                    int x = RouteActions(RouteEntry.Clone().SetMetric(), false);
                    Global.Instance.Log.Add(nameof(DeleteRoute), GetMessage(RouteEntry, x));
                }
                Clear();
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(DeleteRoute), ex); }
        }

        public async void CreateRoute(VpnAccount acc) =>
            await FileInit(true, acc).ConfigureAwait(false);

        public async void DeleteRoute(VpnAccount acc) =>
            await FileInit(false, acc).ConfigureAwait(false);

        #region static
        private void Init() {
            if ((RouteEntry != null) && !RouteEntry.IsEmpty)
                RouteActions(RouteEntry, false);
            NetRouteEntry nre = GetDefaultGwBase();
            if (nre != null) RouteEntry = nre.SwapGw();
            else Clear();
        }
        private void Clear() {
            if (RouteEntry != null)
                RouteEntry = default;
        }
        private async Task<bool> FileInit(bool action, VpnAccount acc) =>
            await Task.Run(async () => {
                try {
                    string file = GetCmdFile(action);
                    FileInfo f = new FileInfo(
                        Path.Combine(Global.GetRootDirectory(Global.DirectoryPlace.Vpn), file));

                    if ((f == null) || !f.Exists || (f.Length == 0)) {
                        Global.Instance.Log.Add(nameof(NetRouteTable), $"user route file '{file}' is empty or not exist..");
                        if (action) CreateRoute();
                        else DeleteRoute();
                        return false;
                    }
                    CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                    try {
                        using ProcessResults results = await ProcessExec.RunAsync(
                            f.FullName, $"{acc.CurrentServiceName} {acc.Interface.Address}", cancellation.Token);

                        if (results.ExitCode != 0)
                            Global.Instance.Log.Add(nameof(NetRouteTable), $"exec '{file}', return code: {results.ExitCode}");
                        if (results.StandardError.Length > 0)
                            foreach (var s in results.StandardError)
                                if (!string.IsNullOrWhiteSpace(s))
                                    Global.Instance.Log.Add(nameof(NetRouteTable), s);
                    }
                    finally {
                        cancellation.Dispose();
                        if (!action && (acc != null))
                            acc.CurrentServiceName = string.Empty;
                    }
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(NetRouteTable), ex); }
                return false;
            });

        private static int RouteActions(NetRouteEntry entry, bool isadd) {
            if ((entry == null) || entry.IsEmpty) return -1;
            NetRouteTableNative.MIB_IPFORWARDROW route = new NetRouteTableNative.MIB_IPFORWARDROW {
                dwForwardDest = BitConverter.ToUInt32(IPAddress.Parse(entry.DestinationNet.ToString()).GetAddressBytes(), 0),
                dwForwardMask = BitConverter.ToUInt32(IPAddress.Parse(entry.DestinationMask.ToString()).GetAddressBytes(), 0),
                dwForwardNextHop = BitConverter.ToUInt32(IPAddress.Parse(entry.GatewayIP.ToString()).GetAddressBytes(), 0),
                dwForwardMetric1 = Convert.ToUInt32(entry.Metric),
                dwForwardType = Convert.ToUInt32(3),
                dwForwardProto = Convert.ToUInt32(3),
                dwForwardAge = Convert.ToUInt32(entry.ForwardAge),
                dwForwardIfIndex = Convert.ToUInt32(entry.InterfaceIndex)
            };
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NetRouteTableNative.MIB_IPFORWARDROW)));
            try {
                Marshal.StructureToPtr(route, ptr, false);
                return isadd ?
                    NetRouteTableNative.CreateIpForwardEntry(ptr) :
                    NetRouteTableNative.DeleteIpForwardEntry(ptr);
            }
            catch { }
            finally {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
            return -1;
        }

        private static NetRouteEntry GetDefaultGwBase() {
            NetworkInterface card = NetworkInterface.GetAllNetworkInterfaces()
                .Where(x => x.OperationalStatus == OperationalStatus.Up && x.Supports(NetworkInterfaceComponent.IPv4))
                .FirstOrDefault();
            if (card == default) return default;
            IPInterfaceProperties prop = card.GetIPProperties();
            if (prop == default) return default;
            IPv4InterfaceProperties pip4 = prop.GetIPv4Properties();
            if (pip4 == default) return default;
            int idx = pip4.Index;
            if (idx < 0) return default;
            UnicastIPAddressInformation addr = prop.UnicastAddresses
                .Where(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .FirstOrDefault();
            if (addr == default) return default;
            GatewayIPAddressInformation agw = prop.GatewayAddresses
                .Where(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .FirstOrDefault();
            if (agw == default) return default;

            int i = 0;
            byte[] mask = addr.IPv4Mask.GetAddressBytes(),
                    net = addr.Address.GetAddressBytes();
            net[i] = (mask[i] == 0) ? (byte)0 : net[i++];
            net[i] = (mask[i] == 0) ? (byte)0 : net[i++];
            net[i] = (mask[i] == 0) ? (byte)0 : net[i++];
            net[i] = (mask[i] == 0) ? (byte)0 : net[i];

            return new NetRouteEntry() {
                DestinationNet = new IPAddress(net),
                DestinationMask = addr.IPv4Mask,
                GatewayIP = agw.Address,
                InterfaceIP = addr.Address,
                InterfaceIndex = idx
            };
        }
        #endregion
    }
}

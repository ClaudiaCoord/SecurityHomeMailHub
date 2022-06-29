/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.ComponentModel;
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
    public enum RouteError : int {
        Success = 0,
        PermissionsNotEnough = 5,
        OperationsNotSupported = 50,
        InvalidParameter = 87,
        NetworkInterfaceNotExists = 1168,
        ObjectAlreadyExists = 5010,
        OtherError = -1,
    }

    public class NetRouteTable
    {
        public NetRouteEntry [] RouteEntrys { get; } = new NetRouteEntry[3];
        private string GetCmdFile(bool b) => b ? "RouteAdd.cmd" : "RouteDelete.cmd";
        private string GetAction(bool b) => b ? "Route Add" : "Route Delete";
        private string GetMessage(NetRouteEntry entry, int x) {
            string desc = (x != 0) ? $", ({new Win32Exception(x).Message})" : string.Empty;
            return (entry == null) ? $"{GetError(x)} = {x}{desc}" :
                    ((entry.GatewayIP == null) ?
                        $"{GetError(x)}/{x} = {entry.DestinationNet}/{entry.DestinationMask} index: {entry.InterfaceIndex} metric: {entry.Metric}{desc}" :
                        $"{GetError(x)}/{x} = {entry.DestinationNet}/{entry.DestinationMask} to: {entry.GatewayIP}/{entry.InterfaceIndex} metric: {entry.Metric}{desc}");
        }

        public async Task<bool> CreateRoute(VpnAccount acc) =>
            await RouteBuild(true, acc).ConfigureAwait(false);

        public async Task<bool> DeleteRoute(VpnAccount acc) =>
            await RouteBuild(false, acc).ConfigureAwait(false);

        #region static
        private async Task<bool> FileInit(bool action, VpnAccount acc) =>
            await Task.Run(async () => {
                try {
                    string file = GetCmdFile(action);
                    FileInfo f = new FileInfo(
                        Path.Combine(Global.GetRootDirectory(Global.DirectoryPlace.Vpn), file));

                    if ((f == null) || !f.Exists || (f.Length == 0)) {
                        Global.Instance.Log.Add(nameof(NetRouteTable), $"user route file '{file}' is empty or not exist..");
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
                    }
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(NetRouteTable), ex); }
                return false;
            });

        private async Task<bool> RouteBuild(bool action, VpnAccount acc) =>
            await Task.Run(async () => {
                if (acc == null) return false;
                try {

                    bool b = await FileInit(action, acc);
                    if (b) return b;

                    Task.Delay(1000).Wait();

                    if (action) {
                        DeleteRouting();

                        NetworkInterface[] ifaces = NetworkInterface.GetAllNetworkInterfaces();
                        if (ifaces == null) return false;

                        NetRouteEntry nreLocalNet = GetDefaultInterface(ifaces);
                        if (nreLocalNet == null) return false;
                        RouteEntrys[0] = nreLocalNet.SwapGw();

                        NetRouteEntry nreVpn = GetVpnInterface(ifaces, acc);
                        if (nreVpn == null) return false;

                        NetRouteEntry nreVpnGw = GetVpnGw(nreVpn);
                        if (nreVpnGw == null) return false;
                        RouteEntrys[1] = nreVpnGw;

                        NetRouteEntry nreVpnNet = GetVpnNet(nreVpn);
                        if (nreVpnNet == null) return false;
                        RouteEntrys[2] = nreVpnNet;

                        foreach (var a in RouteEntrys) {
                            try {
                                if (a != null) {
                                    int x = RouteActions(a, true);
                                    a.IsDuplicate = x == (int)RouteError.ObjectAlreadyExists;
                                    if (!a.IsDuplicate)
                                        Global.Instance.Log.Add(GetAction(true), GetMessage(a, x));
                                }
                            } catch (Exception ex) { Global.Instance.Log.Add(nameof(CreateRoute), ex); }
                        }
                        return true;
                    } else {
                        DeleteRouting();
                        ClearRouteEntrys();
                    }
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(NetRouteTable), ex); ClearRouteEntrys(); }
                return false;
            });

        private static NetRouteEntry GetDefaultInterface(NetworkInterface[] ifaces) {
            NetworkInterface card = ifaces
                .Where(x => x.OperationalStatus == OperationalStatus.Up &&
                            x.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                            x.Supports(NetworkInterfaceComponent.IPv4)).FirstOrDefault();
            return ParseInterface(card);
        }

        private static NetRouteEntry GetVpnInterface(NetworkInterface[] ifaces, VpnAccount acc) {
            NetworkInterface card = ifaces
                .Where(x => x.Name.Equals(acc.CurrentServiceName) &&
                            x.OperationalStatus == OperationalStatus.Up &&
                            x.Supports(NetworkInterfaceComponent.IPv4)).FirstOrDefault();
            return ParseInterface(card);
        }

        private static int RouteActions(NetRouteEntry entry, bool isadd) {
            if ((entry == null) || entry.IsEmpty) return -1;
            NetRouteTableNative.MIB_IPFORWARDROW route = new NetRouteTableNative.MIB_IPFORWARDROW {
                dwForwardDest = (entry.DestinationNet == null) ? 0U : BitConverter.ToUInt32(IPAddress.Parse(entry.DestinationNet.ToString()).GetAddressBytes(), 0),
                dwForwardMask = (entry.DestinationMask == null) ? 0U : BitConverter.ToUInt32(IPAddress.Parse(entry.DestinationMask.ToString()).GetAddressBytes(), 0),
                dwForwardNextHop = (entry.GatewayIP == null) ? 0U : BitConverter.ToUInt32(IPAddress.Parse(entry.GatewayIP.ToString()).GetAddressBytes(), 0),
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

        private static NetRouteEntry ParseInterface(NetworkInterface card) {

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

            int i = 0;
            byte[] mask = addr.IPv4Mask.GetAddressBytes(),
                    net = addr.Address.GetAddressBytes();
            net[i]   = (mask[i] == 0) ? (byte)0 : net[i];
            net[++i] = (mask[i] == 0) ? (byte)0 : net[i];
            net[++i] = (mask[i] == 0) ? (byte)0 : net[i];
            net[++i] = (mask[i] == 0) ? (byte)0 : net[i];

            return new NetRouteEntry() {
                DestinationNet = new IPAddress(net),
                DestinationMask = addr.IPv4Mask,
                GatewayIP = (agw == default) ? default : agw.Address,
                InterfaceIP = addr.Address,
                InterfaceIndex = idx
            };
        }

        private static NetRouteEntry GetVpnGw(NetRouteEntry entry) {
            NetRouteEntry nre = new();
            nre.DestinationNet = nre.DestinationMask = nre.GatewayIP = IPAddress.Parse("0.0.0.0");
            nre.InterfaceIP = default;
            nre.InterfaceIndex = entry.InterfaceIndex;
            return nre;
        }

        private static NetRouteEntry GetVpnNet(NetRouteEntry entry) {
            NetRouteEntry nre = new();
            nre.DestinationNet = entry.DestinationNet;
            nre.DestinationMask = entry.DestinationMask;
            nre.GatewayIP = entry.InterfaceIP; //IPAddress.Parse("0.0.0.0");
            nre.InterfaceIP = default;
            nre.InterfaceIndex = entry.InterfaceIndex;
            return nre;
        }

        private static RouteError GetError(int x) =>
            x switch {
                0 => RouteError.Success,
                5 => RouteError.PermissionsNotEnough,
                50 => RouteError.OperationsNotSupported,
                87 => RouteError.InvalidParameter,
                1168 => RouteError.NetworkInterfaceNotExists,
                5010 => RouteError.ObjectAlreadyExists,
                _ => RouteError.OtherError,
            };

        private void DeleteRouting() {
            foreach (NetRouteEntry a in RouteEntrys) {
                try {
                    if ((a != null) && !a.IsDuplicate) {
                        int x = RouteActions(a.Clone().SetMetric(), false);
                        Global.Instance.Log.Add(GetAction(false), GetMessage(a, x));
                    }
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(DeleteRouting), ex); }
            }
        }
        private void ClearRouteEntrys() {
            for (int i = 0; i < RouteEntrys.Length; i++) {
                try {
                    if (RouteEntrys[i] != null) RouteEntrys[i] = null;
                } catch { }
            }
        }
        #endregion
    }
}


using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace VPN.WireGuard
{
    public class VpnDriver
    {
#       if BUILDWRONG
#       error Wrong build mode, select 32 or 64 platform
#       endif

        /*
#       if BUILD32
#       elif BUILD64
#       elif BUILDWRONG
#       error Wrong build mode, select 32 or 64 platform
#       endif
        */

        [DllImport("wireguard.dll", EntryPoint = "WireGuardOpenAdapter", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern IntPtr openAdapter([MarshalAs(UnmanagedType.LPWStr)] string name);
        [DllImport("wireguard.dll", EntryPoint = "WireGuardCloseAdapter", CallingConvention = CallingConvention.StdCall)]
        private static extern void freeAdapter(IntPtr adapter);
        [DllImport("wireguard.dll", EntryPoint = "WireGuardGetConfiguration", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool getConfiguration(IntPtr adapter, byte[] iface, ref UInt32 bytes);

        [DllImport("tunnel.dll", EntryPoint = "WireGuardGenerateKeypair", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool WireGuardGenerateKeypair(byte[] publicKey, byte[] privateKey);
        [DllImport("tunnel.dll", EntryPoint = "WireGuardTunnelService", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Run([MarshalAs(UnmanagedType.LPWStr)] string configFile);

        public class VpnAdapter
        {
            private IntPtr _handle;
            private uint _lastGetGuess;
            public VpnAdapter(string name)
            {
                _lastGetGuess = 1024;
                _handle = openAdapter(name);
                if (_handle == IntPtr.Zero)
                    throw new Win32Exception();
            }
            ~VpnAdapter() => freeAdapter(_handle);

            public unsafe VpnStat GetStatistic()
            {
                VpnStat stat = new VpnStat();
                byte[] bytes;
                while (true)
                {
                    bytes = new byte[_lastGetGuess];
                    if (getConfiguration(_handle, bytes, ref _lastGetGuess))
                        break;
                    if (Marshal.GetLastWin32Error() != 234)
                        throw new Win32Exception();
                }
                fixed (void* start = bytes)
                {
                    var ioctlIface = (IoctlInterface*)start;
                    var ioctlPeer = (IoctlPeer*)((byte*)ioctlIface + sizeof(IoctlInterface));
                    if (ioctlIface->PeersCount > 0)
                    {
                        stat.RxBytes = ioctlPeer->RxBytes;
                        stat.TxBytes = ioctlPeer->TxBytes;
                    }
                }
                return stat;
            }

            public class VpnStat
            {
                public ulong TxBytes { get; set; } = 0U;
                public ulong RxBytes { get; set; } = 0U;
            }

            private enum IoctlInterfaceFlags : uint
            {
                HasPublicKey = 1 << 0,
                HasPrivateKey = 1 << 1,
                HasListenPort = 1 << 2,
                ReplacePeers = 1 << 3
            };

            [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 80)]
            private unsafe struct IoctlInterface
            {
                public IoctlInterfaceFlags Flags;
                public UInt16 ListenPort;
                public fixed byte PrivateKey[32];
                public fixed byte PublicKey[32];
                public UInt32 PeersCount;
            };

            private enum IoctlPeerFlags : uint
            {
                HasPublicKey = 1 << 0,
                HasPresharedKey = 1 << 1,
                HasPersistentKeepalive = 1 << 2,
                HasEndpoint = 1 << 3,
                ReplaceAllowedIPs = 1 << 5,
                Remove = 1 << 6,
                UpdateOnly = 1 << 7
            };

            [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 136)]
            private unsafe struct IoctlPeer
            {
                public IoctlPeerFlags Flags;
                public UInt32 Reserved;
                public fixed byte PublicKey[32];
                public fixed byte PresharedKey[32];
                public UInt16 PersistentKeepalive;
                public VpnWin32.SOCKADDR_INET Endpoint;
                public UInt64 TxBytes, RxBytes;
                public UInt64 LastHandshake;
                public UInt32 AllowedIPsCount;
            };

            [StructLayout(LayoutKind.Explicit, Pack = 8, Size = 24)]
            private unsafe struct IoctlAllowedIP
            {
                [FieldOffset(0)]
                [MarshalAs(UnmanagedType.Struct)]
                public VpnWin32.IN_ADDR V4;
                [FieldOffset(0)]
                [MarshalAs(UnmanagedType.Struct)]
                public VpnWin32.IN6_ADDR V6;
                [FieldOffset(16)]
                public VpnWin32.ADDRESS_FAMILY AddressFamily;
                [FieldOffset(20)]
                public byte Cidr;
            };
        }
    }
}

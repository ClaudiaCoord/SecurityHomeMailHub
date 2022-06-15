/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Runtime.InteropServices;

namespace SecyrityMail.Vpn.RouteTable
{
    internal class NetRouteTableNative
    {
        [ComVisible(false), StructLayout(LayoutKind.Sequential)]
        public struct IPForwardTable
        {
            public uint Size;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public MIB_IPFORWARDROW[] Table;
        };

        [ComVisible(false), StructLayout(LayoutKind.Sequential)]
        public struct MIB_IPFORWARDROW
        {
            internal uint dwForwardDest;
            internal uint dwForwardMask;
            internal uint dwForwardPolicy;
            internal uint dwForwardNextHop;
            internal uint dwForwardIfIndex;
            internal uint dwForwardType;
            internal uint dwForwardProto;
            internal uint dwForwardAge;
            internal uint dwForwardNextHopAS;
            internal uint dwForwardMetric1;
            internal uint dwForwardMetric2;
            internal uint dwForwardMetric3;
            internal uint dwForwardMetric4;
            internal uint dwForwardMetric5;
        };
        public enum MIB_IPFORWARD_TYPE : uint
        {
            MIB_IPROUTE_TYPE_OTHER = 1,
            MIB_IPROUTE_TYPE_INVALID = 2,
            MIB_IPROUTE_TYPE_DIRECT = 3,
            MIB_IPROUTE_TYPE_INDIRECT = 4
        }
        public enum MIB_IPFORWARD_PROTO : uint
        {
            MIB_IPPROTO_OTHER = 1,
            MIB_IPPROTO_LOCAL = 2,
            MIB_IPPROTO_NETMGMT = 3,
            MIB_IPPROTO_ICMP = 4,
            MIB_IPPROTO_EGP = 5,
            MIB_IPPROTO_GGP = 6,
            MIB_IPPROTO_HELLO = 7,
            MIB_IPPROTO_RIP = 8,
            MIB_IPPROTO_IS_IS = 9,
            MIB_IPPROTO_ES_IS = 10,
            MIB_IPPROTO_CISCO = 11,
            MIB_IPPROTO_BBN = 12,
            MIB_IPPROTO_OSPF = 13,
            MIB_IPPROTO_BGP = 14,
            MIB_IPPROTO_NT_AUTOSTATIC = 10002,
            MIB_IPPROTO_NT_STATIC = 10006,
            MIB_IPPROTO_NT_STATIC_NON_DOD = 10007
        }

        public static IPForwardTable ReadIPForwardTable(IntPtr tablePtr)
        {
            var result = (IPForwardTable)Marshal.PtrToStructure(tablePtr, typeof(IPForwardTable));

            MIB_IPFORWARDROW[] table = new MIB_IPFORWARDROW[result.Size];
            IntPtr p = new IntPtr(tablePtr.ToInt64() + Marshal.SizeOf(result.Size));
            for (int i = 0; i < result.Size; ++i)
            {
                table[i] = (MIB_IPFORWARDROW)Marshal.PtrToStructure(p, typeof(MIB_IPFORWARDROW));
                p = new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(MIB_IPFORWARDROW)));
            }
            result.Table = table;

            return result;
        }

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        public extern static int GetIpForwardTable(IntPtr pIpForwardTable, ref int pdwSize, bool bOrder);

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        public extern static int CreateIpForwardEntry(IntPtr pRoute);

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        public extern static int DeleteIpForwardEntry(IntPtr pRoute);

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        public extern static int SetIpForwardEntry(IntPtr pRoute);
    }
}

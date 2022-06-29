/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System.Net;

namespace SecyrityMail.Vpn.RouteTable
{
    public class NetRouteEntry
    {
        public IPAddress DestinationNet { get; set; } = default(IPAddress);
        public IPAddress DestinationMask { get; set; } = default(IPAddress);
        public IPAddress GatewayIP { get; set; } = default(IPAddress);
        public IPAddress InterfaceIP { get; set; } = default(IPAddress);
        public int InterfaceIndex { get; set; } = -1;
        public int ForwardType { get; set; } = 3;
        public int ForwardProtocol { get; set; } = 3;
        public int ForwardAge { get; set; } = 0;
        public int Metric { get; set; } = 35;
        public bool IsDuplicate { get; set; } = false;

        public bool IsEmpty =>
            (DestinationNet == null) || (DestinationMask == null) || (InterfaceIndex < 0);

        public bool IsGwEmpty =>
            IsEmpty || (GatewayIP == null) || (InterfaceIP == null);

        public NetRouteEntry SwapGw() {
            IPAddress a = InterfaceIP;
            InterfaceIP = GatewayIP;
            GatewayIP = a; return this;
        }
        public NetRouteEntry SetMetric(int x = 0) {
            Metric = x; return this;
        }
        public NetRouteEntry Clone() {
            return new () {
                DestinationNet = DestinationNet,
                DestinationMask = DestinationMask,
                GatewayIP = GatewayIP,
                InterfaceIP = InterfaceIP,
                InterfaceIndex = InterfaceIndex,
                ForwardType = ForwardType,
                ForwardProtocol = ForwardProtocol,
                ForwardAge = ForwardAge,
                Metric = Metric
            };
        }
        public void Copy(NetRouteEntry entry) {
            if (entry == null) return;
            DestinationNet = entry.DestinationNet;
            DestinationMask = entry.DestinationMask;
            GatewayIP = entry.GatewayIP;
            InterfaceIP = entry.InterfaceIP;
            InterfaceIndex = entry.InterfaceIndex;
            ForwardType = entry.ForwardType;
            ForwardProtocol = entry.ForwardProtocol;
            ForwardAge = entry.ForwardAge;
            Metric = entry.Metric;
        }

        public override string ToString() =>
            (GatewayIP == null) ?
                ((InterfaceIP == null) ?
                    $"{DestinationNet}/{DestinationMask} - {InterfaceIndex}/{Metric}" :
                    $"{DestinationNet}/{DestinationMask} - {InterfaceIP}/{InterfaceIndex}/{Metric}") :
                $"{DestinationNet}/{DestinationMask} - {GatewayIP}/{InterfaceIndex}/{Metric}";
    }
}

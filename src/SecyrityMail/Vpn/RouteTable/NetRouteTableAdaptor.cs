/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System.Net;

namespace SecyrityMail.Vpn.RouteTable
{
    public class NetRouteTableAdaptor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string MACAddress { get; set; }
        public int InterfaceIndex { get; set; }
        public IPAddress PrimaryIpAddress { get; set; }
        public IPAddress SubnetMask { get; set; }
        public IPAddress PrimaryGateway { get; set; }
    }
}

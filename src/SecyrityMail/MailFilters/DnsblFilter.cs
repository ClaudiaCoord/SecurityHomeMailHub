/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SecyrityMail.MailFilters
{
    public class DnsblFilter
    {
        static readonly Tuple<IPAddress, bool, string>[] ResponseList = new Tuple<IPAddress, bool, string>[] {
            new Tuple<IPAddress, bool, string>(IPAddress.Parse("127.0.0.1"), false, "DNSBL not blocked"),
            new Tuple<IPAddress, bool, string>(IPAddress.Parse("127.0.0.2"),  true, "SBL Data"),
            new Tuple<IPAddress, bool, string>(IPAddress.Parse("127.0.0.3"),  true, "SBL CSS Data"),
            new Tuple<IPAddress, bool, string>(IPAddress.Parse("127.0.0.4"),  true, "XBL CBL Data"),
            new Tuple<IPAddress, bool, string>(IPAddress.Parse("127.0.0.9"),  true, "SBL DROP/EDROP Data"),
            new Tuple<IPAddress, bool, string>(IPAddress.Parse("127.0.0.10"), true, "PBL ISP Maintained"),
            new Tuple<IPAddress, bool, string>(IPAddress.Parse("127.0.0.11"), true, "PBL Maintained"),
        };

        public string DnsblHost => Global.Instance.Config.DnsblHost;

        public bool Check(IPAddress ip)
        {
            if (!CheckInput(ip)) return false;
            try {
                string r = IPAddressReverse(ip);
                IPHostEntry h = Dns.GetHostEntry($"{r}.{DnsblHost}");
                return CheckIPHostEntry(h);
            }
            catch (System.Net.Sockets.SocketException ex) {
                if (ex.ErrorCode != 11001)
                    Global.Instance.Log.Add(nameof(DnsblFilter), ex);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(DnsblFilter), ex); }
            return false;
        }

        public async Task<bool> CheckAsync(IPAddress ip) {
            if (!CheckInput(ip)) return false;
            try {
                string r = IPAddressReverse(ip);
                IPHostEntry h = await Dns.GetHostEntryAsync($"{r}.{DnsblHost}").ConfigureAwait(false);
                return CheckIPHostEntry(h);
            }
            catch (System.Net.Sockets.SocketException ex) {
                if (ex.ErrorCode != 11001)
                    Global.Instance.Log.Add(nameof(DnsblFilter), ex);
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(DnsblFilter), ex); }
            return false;
        }

        private bool CheckIPHostEntry(IPHostEntry h) {
            if ((h.AddressList != null) && (h.AddressList.Length > 0)) {
                foreach (IPAddress i in h.AddressList) {
                    var a = (from k in ResponseList where k.Item1.Equals(i) select k).FirstOrDefault();
                    if ((a != null) && a.Item2) {
                        Global.Instance.Log.Add(nameof(DnsblFilter), $"{h.HostName} blocked from {a.Item3}");
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckInput(IPAddress ip) =>
            (ip != default) && !string.IsNullOrWhiteSpace(DnsblHost);

        private static string IPAddressReverse(IPAddress ip) {
            byte[] bytes = ip.GetAddressBytes();
            uint uip = BitConverter.ToUInt32(bytes, 0);
            return string.Format("{3}.{2}.{1}.{0}", uip & 0xff, (uip >> 8) & 0xff, (uip >> 16) & 0xff, (uip >> 24) & 0xff);
        }
    }
}

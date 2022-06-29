/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SecyrityMail.Data;
using SecyrityMail.Utils;

namespace SecyrityMail.MailFilters
{
    public class AccessIpFilter : IAutoInit
    {
        private DnsblFilter dnsbl = new();
        private RunOnce runOnce = new();
        List<Tuple<UInt32, UInt32>> ipList = new();
        List<Tuple<string, string>> countryList = new();

        public bool IsAccessIpWhiteList => Global.Instance.Config.IsAccessIpWhiteList;
        public bool IsAccessIpCheckDns => Global.Instance.Config.IsAccessIpCheckDns;
        public bool IsDnsblIpCheck => Global.Instance.Config.IsDnsblIpCheck;

        public async Task AutoInit() => await Reload().ConfigureAwait(false);

        public async Task Reload() =>
            await Task.Run(() => SetSourceList(Global.Instance.Config.ForbidenEntryList))
                                .ConfigureAwait(false);

        public bool Check(IPAddress ipa) {

            if ((ipList.Count == 0) && (countryList.Count == 0) && !IsDnsblIpCheck)
                return true;

            do {
                if (runOnce.IsRunning)
                    break;

                if (ipList.Count > 0) {
                    try {
                        UInt32 u = ToUInt32(ipa);
                        if (u == 0U)
                            break;

                        if ((from i in ipList
                             where i.Item1 <= u && i.Item2 >= u
                             select i).FirstOrDefault() != null)
                            return IsAccessIpWhiteList;
                    } catch (Exception ex) { Global.Instance.Log.Add(nameof(Check), ex); }

                    if (IsAccessIpCheckDns)
                        try {
                            IPHostEntry h = Dns.GetHostEntry(ipa);
                            if ((h != null) && (h.AddressList != null) && (h.AddressList.Length > 0)) {
                                foreach (IPAddress ip in h.AddressList) {
                                    if ((ip == null) || (ip == ipa) ||
                                        (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork))
                                        continue;

                                    UInt32 u = ToUInt32(ip);
                                    if (u == 0U)
                                        continue;

                                    if ((from i in ipList
                                         where i.Item1 <= u && i.Item2 >= u
                                         select i).FirstOrDefault() != null)
                                        return IsAccessIpWhiteList;
                                }
                            }
                        } catch (Exception ex) { Global.Instance.Log.Add(nameof(Check), ex); }
                }

                if (countryList.Count > 0)
                    try {
                        Tuple<string, string>  t = ipa.GetIpInfo();
                        if (t == null)
                            return true;

                        if ((from i in countryList
                             where (!string.IsNullOrWhiteSpace(i.Item1) && !string.IsNullOrWhiteSpace(t.Item1) && i.Item1.Equals(t.Item1)) ||
                                   (!string.IsNullOrWhiteSpace(i.Item2) && !string.IsNullOrWhiteSpace(t.Item2) && i.Item2.Equals(t.Item2))
                             select i).FirstOrDefault() != null)
                            return IsAccessIpWhiteList;
                    } catch (Exception ex) { Global.Instance.Log.Add(nameof(Check), ex); }

                if (IsDnsblIpCheck)
                    try {
                        if (dnsbl.Check(ipa)) return IsAccessIpWhiteList;
                    } catch (Exception ex) { Global.Instance.Log.Add(nameof(Check), ex); }

                return !IsAccessIpWhiteList;
            } while (false);
            return false;
        }

        public void SetSourceList(List<string> list) {

            if (!runOnce.Begin())
                return;

            try {

                ipList.Clear();
                countryList.Clear();

                if (list.Count == 0)
                    return;

                Regex r = new(@"(?<ip>[0-9.]+)/(?<mask>\d{1,2})\s?$|(?<ip>[0-9.]+)\s?$|(?<code>\w{2})\s?$|(?<country>\w+)\s?$",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

                foreach (string s in list) {
                    try {
                        MatchCollection matches = r.Matches(s);
                        if (matches.Count > 0) {

                            string[] names = r.GetGroupNames();
                            foreach (Match m in matches) {

                                string ip = string.Empty,
                                       mask = string.Empty,
                                       isocode = string.Empty,
                                       country = string.Empty;

                                foreach (string name in names) {

                                    if (name.Equals("ip"))
                                        ip = m.Groups[name].Value;
                                    else if (name.Equals("mask"))
                                        mask = m.Groups[name].Value;
                                    else if (name.Equals("code"))
                                        isocode = m.Groups[name].Value;
                                    else if (name.Equals("country"))
                                        country = m.Groups[name].Value;
                                }
                                if (!string.IsNullOrWhiteSpace(ip)) {
                                    UInt32 c = 0, u = ToUInt32(ip);
                                    if (u == 0U)
                                        continue;
                                    if (!string.IsNullOrWhiteSpace(mask) && int.TryParse(mask, out int msk))
                                        c = ToUInt32(u, msk);
                                    ipList.Add(new(u, (c == 0U) ? u : c));
                                }
                                else if (!string.IsNullOrWhiteSpace(isocode))
                                    countryList.Add(new (isocode.Trim(), string.Empty));
                                else if (!string.IsNullOrWhiteSpace(country))
                                    countryList.Add(new(string.Empty, country.Trim()));
                            }
                        }
                    } catch (Exception ex) { Global.Instance.Log.Add(nameof(SetSourceList), ex); }
                }
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(AccessIpFilter), ex); }
            finally { runOnce.End(); }
        }

        public static UInt32 ToUInt32(string s) {
            if (IPAddress.TryParse(s, out IPAddress ipa))
                return ToUInt32(ipa);
            return 0U;
        }
        public static UInt32 ToUInt32(IPAddress ip) {
            byte [] bytes = ip.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
        public static UInt32 ToUInt32(UInt32 ip, int mask) =>
            mask switch {
                0  => ip,
                32 => ip,
                31 => 2U + ip,
                30 => 4U + ip,
                29 => 8U + ip,
                28 => 16U + ip,
                27 => 32U + ip,
                26 => 64U + ip,
                25 => 128U + ip,
                24 => 256U + ip,
                23 => 512U + ip,
                22 => 1024U + ip,
                21 => 2048U + ip,
                20 => 4096U + ip,
                19 => 8192U + ip,
                18 => 16384U + ip,
                17 => 32768U + ip,
                16 => 65536U + ip,
                15 => 131072U + ip,
                14 => 262144U + ip,
                13 => 524288U + ip,
                12 => 1048576U + ip,
                11 => 2097152U + ip,
                10 => 4194304U + ip,
                9  => 8388608U + ip,
                8  => 16777216U + ip,
                7  => 33554432U + ip,
                6  => 67108864U + ip,
                5  => 134217728U + ip,
                4  => 268435456U + ip,
                3  => 536870912U + ip,
                2  => 1073741824U + ip,
                1  => 2147483648U + ip,
                _  => 0U
            };

        public static IPAddress ToNetMask(int mask) {
            string s = mask switch {
                0 => "0.0.0.0",
                32 => "255.255.255.255",
                31 => "255.255.255.254",
                30 => "255.255.255.252",
                29 => "255.255.255.248",
                28 => "255.255.255.240",
                27 => "255.255.255.224",
                26 => "255.255.255.192",
                25 => "255.255.255.128",
                24 => "255.255.255.0",
                23 => "255.255.254.0",
                22 => "255.255.252.0",
                21 => "255.255.248.0",
                20 => "255.255.240.0",
                19 => "255.255.224.0",
                18 => "255.255.192.0",
                17 => "255.255.128.0",
                16 => "255.255.0.0",
                15 => "255.254.0.0",
                14 => "255.252.0.0",
                13 => "255.248.0.0",
                12 => "255.240.0.0",
                11 => "255.224.0.0",
                10 => "255.192.0.0",
                9 => "255.128.0.0",
                8 => "255.0.0.0",
                7 => "254.0.0.0",
                6 => "252.0.0.0",
                5 => "248.0.0.0",
                4 => "240.0.0.0",
                3 => "224.0.0.0",
                2 => "192.0.0.0",
                1 => "128.0.0.0",
                _ => "0.0.0.0"
            };
            return IPAddress.Parse(s);
        }
    }
}

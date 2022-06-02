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

namespace SecyrityMail.IPFilters
{
    public class ForbidenIp : IAutoInit
    {
        private bool __loading = false;
        List<Tuple<UInt32, UInt32>> ipList = new();
        List<Tuple<string, string>> countryList = new();

        public bool IsAccessIpWhiteList => Global.Instance.Config.IsAccessIpWhiteList;
        public bool IsAccessIpCheckDns => Global.Instance.Config.IsAccessIpCheckDns;

        public async Task AutoInit() => await Reload();

        public async Task Reload() =>
            await Task.Run(() => SetSourceList(Global.Instance.Config.ForbidenEntryList))
                                .ConfigureAwait(false);

        public bool Check(IPAddress ipa) {

            if ((ipList.Count == 0) && (countryList.Count == 0))
                return true;

            do {
                if (__loading)
                    break;

                if (ipList.Count > 0)
                    try {
                        UInt32 u = ToUInt32(ipa);
                        if (u == 0U)
                            break;

                        if ((from i in ipList
                             where i.Item1 <= u && i.Item2 >= u
                             select i).FirstOrDefault() != null)
                            return IsAccessIpWhiteList;
                    } catch (Exception ex) { Global.Instance.Log.Add(nameof(Check), ex); }

                /*
                if (IsAccessIpCheckDns)
                    try {
                        string host = Utility.Net.IPAddress.getReversedIpString(ipa) + "." + suffix;
                        IPAddress[] result = Dns.GetHostAddresses(host);
                        if (result.Length > 0)
                            if (result.Contains<IPAddress>(OPresult))
                                return IsAccessIpWhiteList;
                    } catch (Exception ex) { Global.Instance.Log.Add(nameof(Check), ex); }
                */

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

                return !IsAccessIpWhiteList;
            } while (false);
            return false;
        }

        public void SetSourceList(List<string> list) {

            if (__loading)
                return;
            __loading = true;

            try {

                ipList.Clear();
                countryList.Clear();

                if (list.Count == 0)
                    return;

                Regex r = new(@"^(?<ip>[0-9.]+)/(?<mask>\d{1,2})\s$|^(?<ip>[0-9.]+)\s$|^(?<code>\w{2})\s$|^(?<country>\w+)\s?$",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
            catch (Exception ex) { Global.Instance.Log.Add(nameof(ForbidenIp), ex); }
            finally { __loading = false; }
        }

        private static UInt32 ToUInt32(string s) {
            if (IPAddress.TryParse(s, out IPAddress ipa))
                return ToUInt32(ipa);
            return 0U;
        }
        private static UInt32 ToUInt32(IPAddress ip) {
            byte [] bytes = ip.GetAddressBytes();
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
        private static UInt32 ToUInt32(UInt32 ip, int mask) =>
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
    }
}

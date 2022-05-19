/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MaxMind.Db;

namespace SecyrityMail.IPFilters
{
    public static class IpAddressInfo
    {
        private static Reader GeoIpReader = default(Reader);
        private static readonly string GeoIpBase = "GeoLite2-Country.mmdb";
        private static readonly object __lock = new();
        private static string __language = null;
        private static readonly string[] __languages = new string[] {
            "ru", "en", "de", "es", "fr", "ja", "pt", "zh"
        };
        /// <summary>
        /// Get base from: https://github.com/P3TERX/GeoLite.mmdb
        /// </summary>

        public static string GetIpDescription(this string s)
        {
            Tuple<string, string> t = GetIpInfo(s);
            if (t == null)
                return string.Empty;

            if ((t.Item1 != null) && (t.Item2 != null))
                return $"({t.Item2}, {t.Item1})";
            else if (t.Item1 != null)
                return $"({t.Item1})";
            else if (t.Item2 != null)
                return $"({t.Item2})";
            return string.Empty;
        }

        public static Tuple<string, int, string> GetIpInfo(this Tuple<string, int> t) =>
            new(t.Item1, t.Item2, GetIpDescription(t.Item1));

        public static Tuple<string, string> GetIpInfo(this IPAddress ipa) {

            if (ipa == null)
                return default;

            try {
                lock (__lock) {

                    LanguageInit();
                    if (GeoIpReader == default)
                        OpenGeoDb();

                    Dictionary<string, object> dict = GeoIpReader.Find<Dictionary<string, object>>(ipa);
                    if (dict == default)
                        return default;
                    string isocode = default, country = default;
                    try {
                        isocode = dict
                            .Where(p => p.Key == "country")
                            .Select(p => p.Value).Cast<Dictionary<string, object>>()
                            .SelectMany(d => d)
                            .Where(p => p.Key == "iso_code")
                            .Select(p => p.Value.ToString()).Single();
                    } catch { }
                    try {
                        country = dict
                            .Where(p => p.Key == "country")
                            .Select(p => p.Value).Cast<Dictionary<string, object>>()
                            .SelectMany(d => d)
                            .Where(p => p.Key == "names")
                            .Select(p => p.Value).Cast<Dictionary<string, object>>()
                            .SelectMany(d => d)
                            .Where(p => p.Key == __language)
                            .Select(p => p.Value.ToString()).Single();
                    } catch { }
                    return new (isocode, country);
                }
            } catch { }
            return default;
        }

        public static Tuple<string, string> GetIpInfo(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return default;

            try {
                lock (__lock) {

                    LanguageInit();
                    if (GeoIpReader == default)
                        OpenGeoDb();

                    if (IPAddress.TryParse(s, out IPAddress ipa)) {

                        Dictionary<string, object> dict = GeoIpReader.Find<Dictionary<string, object>>(ipa);
                        if (dict == default)
                            return default;
                        string region = default, country = default;
                        try {
                            region = dict
                                .Where(p => p.Key == "continent")
                                .Select(p => p.Value).Cast<Dictionary<string, object>>()
                                .SelectMany(d => d)
                                .Where(p => p.Key == "names")
                                .Select(p => p.Value).Cast<Dictionary<string, object>>()
                                .SelectMany(d => d)
                                .Where(p => p.Key == __language)
                                .Select(p => p.Value.ToString()).Single();
                        } catch { }
                        try {
                            country = dict
                                .Where(p => p.Key == "country")
                                .Select(p => p.Value).Cast<Dictionary<string, object>>()
                                .SelectMany(d => d)
                                .Where(p => p.Key == "names")
                                .Select(p => p.Value).Cast<Dictionary<string, object>>()
                                .SelectMany(d => d)
                                .Where(p => p.Key == __language)
                                .Select(p => p.Value.ToString()).Single();
                        } catch { }
                        if ((region != null) && (country != null))
                            return new(region, country);
                        else if (region != null)
                            return new(region, string.Empty);
                        else if (country != null)
                            return new(string.Empty, country);
                    }
                }
            } catch { }
            return default;
        }

        public static IPAddress ToIpAddress(this string host, bool isV6 = false) {
            IPAddress[] addrs = Dns.GetHostAddresses(host);
            return addrs.FirstOrDefault(a => a.AddressFamily == (isV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork))
                   ??
                   addrs.FirstOrDefault();
        }

        public static IPAddress GetInterfaceIp(this string s, AddressFamily family) {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            NetworkInterface iface = (from i in adapters
                                      where i.Name.Equals(s, StringComparison.InvariantCultureIgnoreCase)
                                      select i).FirstOrDefault();
            if (iface == null)
                return default(IPAddress);
            return (from i in iface.GetIPProperties().UnicastAddresses
                    where i.Address.AddressFamily == family
                    select i.Address).FirstOrDefault();
        }

        public static IPAddress CheckInterfaceIp(this string ip) {
            IPAddress ipa = ip.ToIpAddress();
            foreach (NetworkInterface iface in NetworkInterface.GetAllNetworkInterfaces()) {
                return (from i in iface.GetIPProperties().UnicastAddresses
                               where i.Address.Equals(ipa)
                               select i.Address).FirstOrDefault();
            }
            return default;
        }

        public static async Task<bool> CheckRoute(this string host, List<string> badIp = default, int maxhop = 0) =>
            await Task.Run(async () => {
                if (string.IsNullOrWhiteSpace(host))
                    return false;

                try {
                    maxhop = (maxhop >= 254) ? 253 : ((maxhop < 0) ? 0 : maxhop);
                    byte[] buffer = new byte[32];
                    new Random().NextBytes(buffer);
                    List<IPAddress> badIpAddresses = default;
                    IPAddress ip = host.ToIpAddress();
                    if (ip == null)
                        return false;
                    {
                        string ips = ip.ToString().Equals(host) ? ip.ToString() : $"{ip}|{host}";
                        Global.Instance.Log.Add(nameof(CheckRoute), $"Begin check route to: {ips} {GetIpDescription(ip.ToString())}");
                    }
                    if ((badIp != default) && (badIp.Count > 0)) {
                        badIpAddresses = (from i in badIp select IPAddress.Parse(i)).ToList();
                    }

                    using Ping pinger = new Ping();
                    int tcount = 0, ttl = maxhop;
                    while (true) {

                        if (ttl++ >= 254)
                            break;
                        PingReply reply = await pinger.SendPingAsync(ip, 10000, buffer, new PingOptions(ttl, true))
                                                      .ConfigureAwait(false);
                        if ((reply == null) || (tcount >= 3))
                            break;

                        Global.Instance.Log.Add(nameof(CheckRoute),
                            string.Format("{0}] route: {1} [{2}] {3}",
                                (ttl < 10) ? "0" + ttl.ToString() : ttl.ToString(),
                                reply.Address,
                                (reply.Status == IPStatus.TtlExpired) ? "Next" : reply.Status.ToString(),
                                GetIpDescription(reply.Address.ToString())));

                        if ((reply != null) && reply.Address.Equals(ip))
                            return true;

                        if ((badIpAddresses != default) && (badIpAddresses.Count > 0) &&
                            ((from i in badIpAddresses where i.Equals(reply.Address) select i).FirstOrDefault() != default))
                            break;

                        if (reply.Status == IPStatus.TimedOut)
                            tcount++;

                        if (reply.Status != IPStatus.Success && reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
                            break;
                    }
                } catch (Exception ex) { Global.Instance.Log.Add(nameof(CheckRoute), ex); }
                return false;
            });

        public static void Dispose()
        {
            Reader r = GeoIpReader;
            GeoIpReader = default;
            if (r != default)
                r.Dispose();
        }

        private static void OpenGeoDb() =>
            GeoIpReader = new Reader(
                            Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), GeoIpBase));

        private static void LanguageInit() {
            if (__language == null)
            {
                __language = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
                if (__language == null)
                    __language = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
                if (__language == null)
                    __language = "en";
                else {
                    var found = (from i in __languages
                                 where i.Equals(__language)
                                 select i).FirstOrDefault();
                    if (found == default)
                        __language = "en";
                }
            }
        }

    }
}

/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Proxy;
using SecyrityMail.IPFilters;

namespace SecyrityMail.Proxy
{
    public class HostData
    {
        public string HostName { get; set; }
        public IPAddress Address { get; set; }
        public int Port { get; set; }
        public HostData(string h, int p) {
            HostName = h;
            Port = p;
            Address = h.ToIpAddress();
        }
        public HostData(string uri) {
            Uri u = new Uri(uri);
            HostName = u.DnsSafeHost;
            Port = u.Port;
            if (u.HostNameType == UriHostNameType.Dns)
                Address = HostName.ToIpAddress();
            else
                Address = IPAddress.Parse(u.Host);
        }
    }
    internal class ProxyCheck
    {
        private byte[] GetConnectString(HostData data) => Encoding.UTF8.GetBytes(
            $"CONNECT {data.Address}:{data.Port} HTTP/1.1{Environment.NewLine}{Environment.NewLine}");

        private byte[] GetDestinationString(HostData data) => Encoding.UTF8.GetBytes(
            $"GET / HTTP/1.1\r\nHost: {data.HostName}{GetDestinationPort(data)}\r\n" +
            "User-Agent: curious/1.0.1\r\n" +
            "Accept: */*\r\n" +
            "Connection: close\r\n\r\n");

        private string GetDestinationPort(HostData data) =>
            data.Port != 80 ? $":{data.Port}" : "";

        public async Task<bool> CheckConnect(ProxyType type, IPEndPoint proxy, HostData dest, CancellationToken token) =>
            await CheckConnect(type, proxy.Address.ToString(), proxy.Port, dest, 8000, token, Global.Instance.Log.Add).ConfigureAwait(false);

        public async Task<bool> CheckConnect(ProxyType type, IPAddress addr, int port, HostData dest, CancellationToken token) =>
            await CheckConnect(type, addr.ToString(), port, dest, 8000, token, Global.Instance.Log.Add).ConfigureAwait(false);

        public async Task<bool> CheckConnect(
            ProxyType type, string host, int port, HostData dest, int timeout, CancellationToken token, Action<string, string> action) =>
            await Task.Run(async () => {
                Stream stream = default(Stream);
                CancellationTokenSource cancellation = default(CancellationTokenSource);

                try {
                    IProxyClient client;
                    switch (type) {
                        case ProxyType.Http:  client = new HttpProxyClient(host, port); break;
                        case ProxyType.Https: client = new HttpsProxyClient(host, port) {
                            ServerCertificateValidationCallback = (s, c, ch, e) => true,
                            CheckCertificateRevocation = false
                        }; break;
                        case ProxyType.Sock4: client = new Socks4Client(host, port); break;
                        case ProxyType.Sock5: client = new Socks5Client(host, port); break;
                        default: return false;
                    }
                    stream = await client.ConnectAsync(dest.Address.ToString(), dest.Port, timeout, token)
                                         .ConfigureAwait(false);
                    if (stream == null) return false;

                    stream.ReadTimeout = timeout;
                    stream.WriteTimeout = timeout / 2;

                    if (stream.CanWrite) {
                        byte[] bytes = GetDestinationString(dest);
                        await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                    } else
                        return false;

                    StringBuilder sb = new();
                    cancellation = new(TimeSpan.FromMilliseconds(timeout + 0.0));
                    while (true) {
                        if (cancellation.IsCancellationRequested) break;
                        if (token.IsCancellationRequested) return false;

                        byte[] bytes = new byte[8196];
                        int count = await stream.ReadAsync(bytes, 0, bytes.Length)
                                                .ConfigureAwait(false);
                        if (count > 0) {
                            sb.Append(Encoding.UTF8.GetString(bytes, 0, count));
                            if (sb.Length >= 15) break;
                        }
                        await Task.Delay(15).ConfigureAwait(false);
                    }

                    if (sb.Length == 0)
                        return false;

                    string s = sb.ToString();
                    if (string.IsNullOrWhiteSpace(s)) return false;

                    Match m = Regex.Match(s, @"^HTTP/1.(\d)\s(\d{3})\s?(:?.+)\r?\n",
                        RegexOptions.CultureInvariant |
                        RegexOptions.Multiline |
                        RegexOptions.IgnoreCase |
                        RegexOptions.Compiled);

                    if ((!m.Success) || (m.Groups.Count < 3) || !int.TryParse(m.Groups[2].Value, out int code))
                        return false;

                    if ((m.Groups.Count >= 4) && !string.IsNullOrWhiteSpace(m.Groups[3].Value)) {
                        string rs = (m.Groups[3].Value.Length > 100) ?
                            m.Groups[3].Value.Substring(0, 100).Trim() : m.Groups[3].Value.Trim();
                        action.Invoke(nameof(CheckConnect), $"info  {host}:{port} -> {code} = {rs}");
                    }
                    stream.Close();
                    return true;
                }
                catch (NotSupportedException ex) {
                    action.Invoke(nameof(CheckConnect), $"error {host}:{port} -> {ex.Message}");
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                catch (ProxyProtocolException ex) {
                    action.Invoke(nameof(CheckConnect), $"error {host}:{port} -> {ex.Message}");
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                catch (TimeoutException ex) {
                    action.Invoke(nameof(CheckConnect), $"error {host}:{port} -> {ex.Message}");
                }
                catch (SocketException ex) {
                    string s = (ex.Message.Length > 60) ? ex.Message.Substring(0, 60) : ex.Message;
                    action.Invoke(nameof(CheckConnect), $"error {host}:{port} -> {s}");
                }
                catch (Exception ex) {
                    action.Invoke(nameof(CheckConnect), ex.Message);
                    System.Diagnostics.Debug.WriteLine(ex);
                }
                finally {
                    if (cancellation != default)
                        try { cancellation.Dispose(); } catch { }
                    if (stream != default)
                        try { stream.Dispose(); } catch { }
                }
                return false;
            });
    }
}

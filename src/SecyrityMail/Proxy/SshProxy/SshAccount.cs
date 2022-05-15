
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SecyrityMail.Utils;

namespace SecyrityMail.Proxy.SshProxy
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class SshAccount
    {
        private string _Name = string.Empty;

        [XmlElement("port")]
        public int Port { get; set; } = -1;
        [XmlElement("host")]
        public string Host { get; set; } = string.Empty;
        [XmlElement("login")]
        public string Login { get; set; } = string.Empty;
        [XmlElement("pass")]
        public string Pass { get; set; } = string.Empty;
        [XmlElement("type")]
        public ProxyType Type { get; set; } = ProxyType.None;
        [XmlElement("expired")]
        public DateTime Expired { get; set; } = DateTime.MinValue;
        [XmlElement("enable")]
        public bool Enable { get; set; } = true;
        [XmlElement("name")]
        public string Name {
            get {
                if (string.IsNullOrWhiteSpace(_Name)) {
                    bool[] b = new bool [] {
                        !string.IsNullOrWhiteSpace(Host),
                        !string.IsNullOrWhiteSpace(Login)
                    };
                    if (b[0] && b[1])
                        _Name = $"{Host}-{Login}";
                    else if (b[0])
                        _Name = Host;
                    if (b[1])
                        _Name = Login;
                    else
                        return string.Empty;
                }
                return _Name;
            }
            set => _Name = value;
        }

        [XmlIgnore]
        public bool IsEmpty => IsEmptyNoCheckType || Type == ProxyType.None;

        [XmlIgnore]
        public bool IsEmptyNoCheckType => Port == -1 || IsEmptyNoCheckTypeAndPort;

        [XmlIgnore]
        public bool IsEmptyNoCheckTypeAndPort => 
            string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Pass);

        [XmlIgnore]
        public bool IsExpired =>
            Expired != DateTime.MinValue && Expired <= DateTime.Now;

        public bool Copy(SshAccount acc) {
            if (acc == default)
                return false;

            Port = acc.Port;
            Host = acc.Host;
            Login = acc.Login;
            Pass = acc.Pass;
            Name = acc.Name;
            Type = acc.Type;
            Expired = acc.Expired;
            Enable = acc.Enable;
            return true;
        }

        public void Clear() {
            Port = -1;
            Host =
            Login =
            Pass =
            Name = string.Empty;
            Type = ProxyType.None;
            Expired = DateTime.MinValue;
            Enable = false;
        }

        public async Task<bool> Load(string path) =>
            await Task.Run(() => {
                try
                {
                    SshAccount acc = path.DeserializeFromFile<SshAccount>();
                    return Copy(acc);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Load), ex); }
                return false;
            });

        public async Task<bool> Save(string path) =>
            await Task.Run(() => {
                try
                {
                    path.SerializeToFile(this);
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Save), ex); }
                return false;
            });
    }
}

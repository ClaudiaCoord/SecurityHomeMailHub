
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MimeKit;
using SecyrityMail.Data;
using SecyrityMail.Utils;

namespace SecyrityMail.MailAddress
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class AddressesBook : IAutoInit, IDisposable
    {
        [XmlIgnore]
        public static readonly string FileName = $"{nameof(AddressesBook)}.conf";
        private readonly object __lock = new();
        private volatile Timer timer;
        private bool isSaved = true;

        [XmlElement("items")]
        public List<AddressEntry> Items { get; set; } = new();

        public AddressesBook() => timer = new Timer(TimerCb, default, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        ~AddressesBook() => Dispose();

        private async void TimerCb(object _) {
            if (timer != null)
                timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            if (!isSaved)
                await Save().ConfigureAwait(false);
        }

        public void Add(AddressEntry ae) => Add_(ae);
        public void Add(MailboxAddress ma) => Add_(ma.Name, ma.Address, string.Empty);
        public void Add(InternetAddress ia) => Add(ia as MailboxAddress);
        public void Add(Tuple<string, string> t) => Add_(t.Item1, t.Item2, string.Empty);
        public void Add(string n, string e, string d = default) => Add_(n, e, d);
        public void AddRange(List<Tuple<string, string>> list) { foreach (var t in list) Add(t.Item1, t.Item2); }
        public void AddRange(InternetAddressList list) { foreach (var a in list) Add(a.Name, ((MailboxAddress)a).Address); }
        public async Task AddRangeAsync(MimeMessage mmsg) =>
            await Task.Run(() => {
                try {
                    if (mmsg == null)
                        return;
                    if ((mmsg.From != null) && (mmsg.From.Count > 0))
                        AddRange(mmsg.From);
                    if ((mmsg.To != null) && (mmsg.To.Count > 0))
                        AddRange(mmsg.To);
                    if ((mmsg.Cc != null) && (mmsg.Cc.Count > 0))
                        AddRange(mmsg.Cc);
                    if ((mmsg.Bcc != null) && (mmsg.Bcc.Count > 0))
                        AddRange(mmsg.Bcc);
                    if ((mmsg.ReplyTo != null) && (mmsg.ReplyTo.Count > 0))
                        AddRange(mmsg.ReplyTo);
                    if ((mmsg.ResentTo != null) && (mmsg.ResentTo.Count > 0))
                        AddRange(mmsg.ResentTo);
                    if ((mmsg.ResentReplyTo != null) && (mmsg.ResentReplyTo.Count > 0))
                        AddRange(mmsg.ResentReplyTo);
                    timer.Change(TimeSpan.FromSeconds(30.0), Timeout.InfiniteTimeSpan);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(AddressesBook), ex); }
                finally {
                    isSaved = false;
                }
            });

        public MailboxAddress Find(string s) {
            AddressEntry ae;
            lock (__lock)
                ae = (from i in Items where i.Email.Equals(s) select i).FirstOrDefault();
            if (ae != null)
                return ae.Get;
            return default;
        }

        public InternetAddressList GetAddressList() {
            var a = (from i in Items select new MailboxAddress(i.Name, i.Email)).ToList();
            return new InternetAddressList(a);
        }
        public List<string> GetSuggestionsList() =>
            (from i in Items select i.Email).ToList();

        public async void Dispose() {
            
            if (!isSaved)
                _ = await Save().ConfigureAwait(false);

            Timer t = timer;
            timer = default;
            if (t != null)
                t.Dispose();
        }

        public async Task AutoInit() => _ = await Load().ConfigureAwait(false);

        public bool Copy(AddressesBook ab) {

            if ((ab == null) || (ab.Items == null) || (ab.Items.Count == 0))
                return false;
            lock (__lock) {
                Items.Clear();
                Items.AddRange(ab.Items);
                return Items.Count > 0;
            }
        }

        public async Task<bool> Load() =>
            await Load(Global.GetRootFile(Global.DirectoryPlace.Root, FileName));

        public async Task<bool> Load(string path) =>
            await Task.Run(() => {
                try {
                    AddressesBook ab = path.DeserializeFromFile<AddressesBook>();
                    return Copy(ab);
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Load), ex); }
                return false;
            });

        public async Task<bool> Save() =>
            await Save(Global.GetRootFile(Global.DirectoryPlace.Root, FileName));

        public async Task<bool> Save(string path) =>
            await Task.Run(() => {
                try {
                    List<AddressEntry> items = Items.Distinct().ToList();
                    items.Sort(CompareAddressEntry);
                    lock (__lock) {
                        Items.Clear();
                        Items.AddRange(items);
                        path.SerializeToFile<AddressesBook>(this);
                    }
                    isSaved = true;
                    return true;
                }
                catch (Exception ex) { Global.Instance.Log.Add(nameof(Save), ex); }
                return false;
            });

        private static int CompareAddressEntry(AddressEntry a, AddressEntry b) =>
            a.Email.CompareTo(b.Email);

        private void Add_(AddressEntry ae) {
            if ((from i in Items where i.Email.Equals(ae.Email) select i).FirstOrDefault() == null)
                lock (__lock) Items.Add(ae);
        }
        private void Add_(string n, string e, string d) {
            if ((from i in Items where i.Email.Equals(e) select i).FirstOrDefault() == null)
                lock (__lock) Items.Add(new AddressEntry(n, e, d));
        }
    }
}

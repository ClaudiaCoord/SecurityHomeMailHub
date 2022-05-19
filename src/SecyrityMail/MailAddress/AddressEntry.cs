/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using MimeKit;

namespace SecyrityMail.MailAddress
{
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class AddressEntry : IComparer<AddressEntry>
    {
        [XmlElement("name")]
        public string Name { get; set; } = string.Empty;
        [XmlElement("addr")]
        public string Email { get; set; } = string.Empty;
        [XmlElement("desc")]
        public string Desc { get; set; } = string.Empty;

        public AddressEntry() { }
        public AddressEntry(MailboxAddress addr) => Init(addr.Name, addr.Address, string.Empty);
        public AddressEntry(InternetAddress addr) => Init(addr.Name, ((MailboxAddress)addr).Address, string.Empty);
        public AddressEntry(Tuple<string, string> t) => Init(t.Item1, t.Item2, string.Empty);
        public AddressEntry(string name, string email, string desc = default) => Init(name, email, desc);

        [XmlIgnore]
        public MailboxAddress Get => new MailboxAddress(Name, Email);
        [XmlIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(Email);

        private void Init(string name, string email, string desc) {
            Name = name; Email = email; Desc = desc;
        }

        public int Compare(AddressEntry x, AddressEntry y) =>
            x.Email.CompareTo(y.Email);
    }
}

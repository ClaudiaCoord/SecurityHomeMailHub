/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;

namespace SecyrityMail.MailFilters
{
    public class SpamFilterData
    {
        private string body = string.Empty;

        public string Ip { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body {
            get => string.IsNullOrWhiteSpace(body) ? Subject :
                (string.IsNullOrWhiteSpace(Subject) ? body : $"{Subject}{Environment.NewLine}{body}");
            set => body = value;
        }
        public bool IsEmpty => string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(Body);
    }
}

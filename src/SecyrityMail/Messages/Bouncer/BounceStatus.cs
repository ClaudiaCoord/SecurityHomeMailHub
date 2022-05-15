﻿namespace SecyrityMail.Messages.Bouncer
{
    public sealed class BounceStatus
    {
        public BounceStatus(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public int Code { get; }
        public string Message { get; }

        public override string ToString()
        {
            return $"{Code} {Message}";
        }
    }
}

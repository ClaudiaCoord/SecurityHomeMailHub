/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;

namespace HomeMailHub.CmdLine
{
    internal class CmdOptionException : Exception
    {
        public bool IsCallHelp { get; set; } = false;
        private static string GetMessage(Exception e) => $"{e.GetType()}: {e.Message}";

        public static CmdOptionException Create(Exception e) => new(GetMessage(e), e);
        public static CmdOptionException Create(string s, Exception e) => new(s, e);
        public static CmdOptionException Create(Exception e, bool b) => new(GetMessage(e), e, b);
        private CmdOptionException(string s, Exception e) : base(s, e) { }
        private CmdOptionException(string s, Exception e, bool b) : base(s, e) { IsCallHelp = b; }
    }
}

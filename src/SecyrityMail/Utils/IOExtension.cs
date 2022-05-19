/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


namespace SecyrityMail.Utils
{
    internal static class IOExtension
    {
        public static string BasePathFile(this string s, bool b) => b ? $"{s}.bak" : s;
    }
}

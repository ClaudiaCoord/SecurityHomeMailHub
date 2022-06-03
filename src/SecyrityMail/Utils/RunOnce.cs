/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

namespace SecyrityMail.Utils
{
    internal class RunOnce
    {
        public bool IsRunning { get; private set; } = false;

        public bool Begin() { if (IsRunning) return false; IsRunning = true; return true; }
        public void End() { if (IsRunning) IsRunning = false; }
    }
}

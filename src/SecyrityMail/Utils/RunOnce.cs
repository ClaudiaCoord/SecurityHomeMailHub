/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System.Threading;

namespace SecyrityMail.Utils
{
    internal class RunOnce {
        private long _running = 0L;
        public bool IsRunning {
            get => Interlocked.Read(ref _running) != 0L;
            set => Interlocked.Exchange(ref _running, value ? 1L : 0L);
        }

        public bool Begin() { if (IsRunning) return false; IsRunning = true; return true; }
        public void End() { if (IsRunning) IsRunning = false; }
    }
}

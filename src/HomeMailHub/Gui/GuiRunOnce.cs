/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Threading;

namespace HomeMailHub.Gui
{
    internal class GuiRunOnce
    {
        private long __lock = 0, __last = -1L;
        public bool IsLocked {
            get => Interlocked.Read(ref __lock) != 0L;
            private set => Interlocked.Exchange(ref __lock, value ? 1L : 0L);
        }
        public int LastId {
            get => (int)Interlocked.Read(ref __last);
            private set => Interlocked.Exchange(ref __last, (long)value);
        }

        public bool IsRun() => IsLocked;
        public bool IsRun(int id) => IsLocked || (LastId == id);
        public bool IsRange(int count) => (LastId >= 0) && (LastId < count);

        public bool GoRun() {
            if (IsLocked) return false;
            IsLocked = true;
            return true;
        }
        public bool GoRun(int id) {
            if (IsLocked || (LastId == id)) return false;
            LastId = id;
            IsLocked = true;
            return true;
        }
        public bool GoRun(Action<bool> action) {
            if (IsLocked) {
                action.Invoke(true);
                return false;
            }
            IsLocked = true;
            action.Invoke(false);
            return true;
        }
        public bool GoRun(int id, Action<bool> action) {
            if (!GoRun(id)) return false;
            action.Invoke(false);
            return true;
        }
        public void EndRun() {
            if (IsLocked) IsLocked = false;
        }
        public void EndRun(Action<bool> action) {
            if (!IsLocked) return;
            IsLocked = false;
            action.Invoke(false);
        }
        public void ResetId() => LastId = -1;
        public override string ToString() => $"lock:{IsLocked}, id:{LastId}";
    }
}

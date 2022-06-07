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
        public bool IsNewId(int id) => (LastId != id) && (id >= 0);
        public bool IsValidId(int id) => id >= 0;
        public bool IsValidId() => LastId >= 0;

        public bool Begin() {
            if (IsLocked) return false;
            IsLocked = true;
            return true;
        }
        public bool Begin(int id) {
            if (IsLocked || (LastId == id)) return false;
            LastId = id;
            IsLocked = true;
            return true;
        }
        public bool Begin(Action<bool> action) {
            if (IsLocked) {
                action.Invoke(true);
                return false;
            }
            IsLocked = true;
            action.Invoke(false);
            return true;
        }
        public bool Begin(int id, Action<bool> action) {
            if (!Begin(id)) return false;
            action.Invoke(false);
            return true;
        }
        public void End() {
            if (IsLocked) IsLocked = false;
        }
        public void End(Action<bool> action) {
            if (!IsLocked) return;
            IsLocked = false;
            action.Invoke(false);
        }
        public void ResetId() => LastId = -1;
        public override string ToString() => $"lock:{IsLocked}, id:{LastId}";
    }
}

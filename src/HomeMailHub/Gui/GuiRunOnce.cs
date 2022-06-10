/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Threading;

namespace HomeMailHub.Gui
{
    public class GuiRunOnce
    {
        private long __lock = 0, __last = -1L;
        public bool IsLocked {
            get => Interlocked.Read(ref __lock) != 0L;
            private set => Interlocked.Exchange(ref __lock, value ? 1L : 0L);
        }
        public int Id {
            get => (int)Interlocked.Read(ref __last);
            private set => Interlocked.Exchange(ref __last, (long)value);
        }
        public string Ids { get; private set; } = string.Empty;

        public bool IsRun() => IsLocked;
        public bool IsRun(int id) => IsLocked || (Id == id);
        public bool IsRange(int count) => (Id >= 0) && (Id < count);
        public bool IsNewId(int id) => (Id != id) && (id >= 0);
        public bool IsValidId(int id) => id >= 0;
        public bool IsValidId() => Id >= 0;
        public bool IsValidIds() => !string.IsNullOrWhiteSpace(Ids);
        public void ChangeId(int id) => Id = id;
        public void ChangeId(string s) => Ids = (!Ids.Equals(s)) ? s : Ids;
        public void ChangeId(int id, string s) { Id = id; ChangeId(s); }
        public void ResetId() { Id = -1; Ids = string.Empty; }

        public bool Begin() {
            if (IsLocked) return false;
            IsLocked = true;
            return true;
        }
        public bool Begin(int id) {
            if (IsLocked || (Id == id)) return false;
            Id = id;
            IsLocked = true;
            return true;
        }
        public bool Begin(int id, string ids)
        {
            if (IsLocked || (Id == id)) return false;
            Id = id;
            Ids = ids;
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
        public override string ToString() => $"lock:{IsLocked}, id:{Id}, Ids:{Ids}";
    }
}

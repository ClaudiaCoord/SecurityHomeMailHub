/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Threading;
using Terminal.Gui;

namespace HomeMailHub.Gui
{
    internal class GuiBusyBar : ProgressBar, IDisposable
    {
        private Timer systemTimer { get; set; } = default;

        public GuiBusyBar() : base() {
            base.Visible = false;
            base.ColorScheme = Colors.Base;
            systemTimer = new Timer((a) => {
                Application.MainLoop?.Invoke(() => Pulse());
            }, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        ~GuiBusyBar() => Dispose();
        public new void Dispose() {
            this.GetType().IDisposableObject(this);
            base.Dispose();
        }

        public void Start() {
            Application.MainLoop?.Invoke(() => Visible = true);
            systemTimer.Change(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(150));
        }
        public void Stop() {
            systemTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            Application.MainLoop?.Invoke(() => Visible = false);
        }
    }
}

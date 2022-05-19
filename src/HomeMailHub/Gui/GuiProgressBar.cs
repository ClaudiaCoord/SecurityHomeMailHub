/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using Terminal.Gui;

namespace HomeMailHub.Gui
{
    internal class GuiProgressBar : ProgressBar, IDisposable {

        private int _current = 0;
        private int _step = 0;
        private float _val = 0.01F;

        public GuiProgressBar() : base() => base.Visible = false;
        ~GuiProgressBar() => Dispose();
        public new void Dispose() => base.Dispose();

        public void Begin(int count) {

            Application.MainLoop.Invoke(() => {
                base.Fraction = 0.03F;
                base.Visible = true;
            });
            _step = (count >= 100) ? (count / 100) : 0;
            _val = (count >= 100) ? 0.01F : ((100 / count) * 0.01F);
            _current = _step;
        }
        public void End() {

            Application.MainLoop.Invoke(() => {
                base.Fraction = 0F;
                base.Visible = false;
            });
            _val = 0.01F;
            _current = _step = 0;
        }
        public new void Pulse() {

            if (_current++ >= _step) {
                Application.MainLoop.Invoke(() =>
                    base.Fraction = (base.Fraction + _val >= 1) ? 1.0F : (base.Fraction + _val));
                _current = 0;
            }
        }
    }
}

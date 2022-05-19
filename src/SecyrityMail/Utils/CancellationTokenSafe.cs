/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */


using System;
using System.Threading;

namespace SecyrityMail.Utils
{
    public class TokenSafe
    {
        CancellationToken token, extoken;

        public TokenSafe() { token = default; extoken = default; }
        public TokenSafe(CancellationToken t, CancellationToken ext) { token = t; extoken = ext; }

        public CancellationToken GetToken => token;
        public bool CanBeCanceled => (token == default) ? true : token.CanBeCanceled;
        public WaitHandle WaitHandle => (token == default) ? default : token.WaitHandle;
        public bool IsCancellationRequested =>
            ((token != default) && token.IsCancellationRequested) ||
            ((extoken != default) && extoken.IsCancellationRequested) ||
            ((extoken == default) && (token == default));

        public void ThrowIfCancellationRequested() {
            if (extoken != default) extoken.ThrowIfCancellationRequested();
            if (token != default) token.ThrowIfCancellationRequested();
            if ((token == default) && (extoken == default))
                throw new InvalidOperationException("all cancellation token is null?!");
        }
    }

    public class CancellationTokenSafe : IDisposable
    {
        CancellationToken extct = default(CancellationToken);
        CancellationTokenSource ctsrc = default(CancellationTokenSource);
        bool isDisposed = false;

        public CancellationTokenSafe(CancellationToken ct) { Reload(); extct = default; }
        public CancellationTokenSafe(TimeSpan t = default) { Reload(t); }
        ~CancellationTokenSafe() => Dispose();

        public TokenSafe TokenSafe { get; private set; } = new();
        public CancellationToken Token { get; private set; } = default;
        public bool IsCancellationRequested => IsDisposed ? true : ctsrc.IsCancellationRequested;
        public bool IsDisposed => (ctsrc == default) || isDisposed;
        public bool IsExtendedToken => extct != default;
        public void Clear() => Dispose();
        public void Cancel() { if (!IsDisposed && !ctsrc.IsCancellationRequested) ctsrc.Cancel(); }
        public void Reload(TimeSpan t = default) {
            if ((ctsrc != default) && !IsDisposed)
                Dispose();
            if (t == default)
                ctsrc = new();
            else
                ctsrc = new(t);

            if ((extct != default) && extct.IsCancellationRequested)
                extct = default;

            Token = ctsrc.Token; isDisposed = false;
            TokenSafe = new(Token, extct);
        }
        public CancellationToken GetExtendedCancellationToken() => extct;
        public void SetExtendedCancellationToken(CancellationToken t) { extct = t; TokenSafe = new(Token, extct); }
        public void CheckExtendedCancellationToken() {
            if (!IsExtendedToken)
                throw new InvalidOperationException("extended cancellation Token not set!");
        }

        public void Dispose() {
            isDisposed = true;
            CancellationTokenSource st = ctsrc;
            ctsrc = default;
            if (st != default) {
                if (!st.IsCancellationRequested)
                    try { st.Cancel(); } catch { }
                try { st.Dispose(); } catch { }
            }
        }
    }
}

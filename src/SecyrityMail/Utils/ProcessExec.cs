/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/SecyrityMail
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SecyrityMail.Utils
{
    public sealed class ProcessResults : IDisposable {
        public ProcessResults(Process process, DateTime startTime, string[] sOutput, string[] sError) {
            Process = process;
            ExitCode = process.ExitCode;
            RunTime = process.ExitTime - startTime;
            StandardOutput = sOutput;
            StandardError = sError;
        }

        public Process Process { get; }
        public int ExitCode { get; }
        public TimeSpan RunTime { get; }
        public string[] StandardOutput { get; }
        public string[] StandardError { get; }
        public void Dispose() { Process.Dispose(); }
    }

    public static class ProcessExec {

        public static Task<ProcessResults> RunAsync(string fileName)
            => RunAsync(new ProcessStartInfo(fileName));

        public static Task<ProcessResults> RunAsync(string fileName, CancellationToken cancellationToken)
            => RunAsync(new ProcessStartInfo(fileName), cancellationToken);

        public static Task<ProcessResults> RunAsync(string fileName, string arguments)
            => RunAsync(new ProcessStartInfo(fileName, arguments));

        public static Task<ProcessResults> RunAsync(string fileName, string arguments, CancellationToken cancellationToken)
            => RunAsync(new ProcessStartInfo(fileName, arguments), cancellationToken);

        public static Task<ProcessResults> RunAsync(ProcessStartInfo processStartInfo)
            => RunAsync(processStartInfo, CancellationToken.None);

        public static Task<ProcessResults> RunAsync(ProcessStartInfo processStartInfo, CancellationToken cancellationToken)
            => RunAsync(processStartInfo, new List<string>(), new List<string>(), cancellationToken);

        #region Run Async
        public static async Task<ProcessResults> RunAsync(
            ProcessStartInfo pi, List<string> so, List<string> se, CancellationToken token, bool iscmd = false) {
            pi.UseShellExecute = iscmd;
            /* pi.CreateNoWindow = iscmd; */
            pi.RedirectStandardOutput = true;
            pi.RedirectStandardError = true;

            TaskCompletionSource<ProcessResults> tcs = new ();
            Process proc = new Process {
                StartInfo = pi,
                EnableRaisingEvents = true,
            };

            TaskCompletionSource<string[]> outputResults = new ();
            proc.OutputDataReceived += (sender, args) => {
                if (args.Data != null)
                    so.Add(args.Data);
                else
                    outputResults.SetResult(so.ToArray());
            };

            TaskCompletionSource<string[]> errorResults = new ();
            proc.ErrorDataReceived += (sender, args) => {
                if (args.Data != null)
                    se.Add(args.Data);
                else
                    errorResults.SetResult(se.ToArray());
            };

            TaskCompletionSource<DateTime> startTime = new ();
            proc.Exited += async (sender, args) => {
                tcs.TrySetResult(
                    new ProcessResults(
                        proc,
                        await startTime.Task.ConfigureAwait(false),
                        await outputResults.Task.ConfigureAwait(false),
                        await errorResults.Task.ConfigureAwait(false)
                    )
                );
            };

            using (token.Register(
                () => {
                    tcs.TrySetCanceled();
                    try {
                        if (!proc.HasExited) proc.Kill();
                    } catch (InvalidOperationException) { }
                })) {

                token.ThrowIfCancellationRequested();
                DateTime sTime = DateTime.Now;

                if (proc.Start() == false)
                    tcs.TrySetException(new InvalidOperationException("Failed to start process"));
                else {
                    try {
                        sTime = proc.StartTime;
                    } catch { }
                    startTime.SetResult(sTime);
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }
                return await tcs.Task.ConfigureAwait(false);
            }
        }
        #endregion
    }
}

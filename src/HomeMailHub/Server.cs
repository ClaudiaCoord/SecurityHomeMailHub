/*
 * Git: https://github.com/ClaudiaCoord/SecurityHomeMailHub/tree/main/src/HomeMailHub
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.IO;
using System.Threading.Tasks;
using HomeMailHub.Associate;
using HomeMailHub.CmdLine;
using HomeMailHub.Gui;
using SecyrityMail;
using SecyrityMail.Utils;

namespace HomeMailHub
{
    internal class Server
    {
        static Options options = default;
        static GuiApp gui = default;
        static CancellationTokenSafe cancellation = new();
        static StreamWriter[] filelog = new StreamWriter[2];

        [STAThread]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            TaskScheduler.UnobservedTaskException +=
                new EventHandler<UnobservedTaskExceptionEventArgs>(TaskScheduler_UnobservedTaskException);

            #region Associate extension handle
            if (args.Length > 0) {
                try {
                    AssociateExtension ae = new();
                    (bool iscmd, string file) = ae.Parse(args);
                    if (iscmd) {
                        if (string.IsNullOrWhiteSpace(file))
                            return;
                        try {
                            Global.Instance.Config.Copy(new ConfigurationLoad());
                            Global.Instance.Init();
                            gui = new(typeof(GuiMessageReadWindow), file);
                            gui.Init();
                        }
                        catch (Exception ex) { ae.ShowError(ex.Message); }
                        finally {
                            if (!cancellation.IsCancellationRequested)
                                cancellation.Cancel();
                            cancellation.Dispose();
                            Global.Instance.DeInit();
                        }
                        return;
                    }
                } catch { }
            }
            #endregion

            options = CmdOption.Parse<Options>(args);
            if (!options.Check())
                return;

            filelog[0] = new StreamWriter(Global.GetRootFile(Global.DirectoryPlace.Log, "main.log"), false);
            filelog[0].AutoFlush = true;
            filelog[1] = new StreamWriter(Global.GetRootFile(Global.DirectoryPlace.Log, "event.log"), false);
            filelog[1].AutoFlush = true;
            GuiApp.IsWriteLog = options.IsAllLog;

            gui = new();
            gui.EventCb += Gui_EventCb;

            Global.Instance.Config.Copy(new ConfigurationLoad());
            Global.Instance.Log.EventCb += (s, a) => {
                bool b1 = IsWriteLog(0),
                     b2 = IsIsViewLog();
                if (!b2)
                    return;
                Global.Instance.Log.ForeachLog((t) => {
                    if (b1) filelog[0].WriteLine($"{t.Item1:HH:mm:ss} - {t.Item2} - {t.Item3}");
                    if (b2) _ = gui.AddLog(t);
                });
            };
            Global.Instance.EventCb += (s, a) => {
                if (IsWriteLog(1))
                    filelog[1].WriteLine($"{a.Id}/{a.Sender} - {a.Text} - {a.Obj}");
                if (IsIsViewEvent())
                    gui.AddEvent(a);
            };
            try {
                Global.Instance.Init(cancellation.Token);
                Global.Instance.Start();

                gui.Start();
                cancellation.Cancel();
                Global.Instance.Stop();
                Global.Instance.Wait();
            }
            catch (Exception ex) { Global.Instance.Log.Add(nameof(Main), ex); }
            finally {
                if (!cancellation.IsCancellationRequested)
                    cancellation.Cancel();
                cancellation.Dispose();
                gui.EventCb -= Gui_EventCb;
                gui.Dispose();
                Global.Instance.DeInit();

                StreamWriter f;
                f = filelog[0];
                filelog[0] = default;
                if (f != null)
                    f.Dispose();

                f = filelog[1];
                filelog[1] = default;
                if (f != null)
                    f.Dispose();
            }
        }

        private static void Gui_EventCb(object sender, SecyrityMail.Data.EventActionArgs a) =>
            Global.Instance.ToMainEvent(sender, a);

        private static void TaskScheduler_UnobservedTaskException(object s, UnobservedTaskExceptionEventArgs e) =>
            Global.Instance.Log.Add(s.GetType().Name, e.Exception);

        private static void CurrentDomain_UnhandledException(object s, UnhandledExceptionEventArgs e) =>
            Global.Instance.Log.Add(s.GetType().Name, e.ExceptionObject as Exception);

        private static bool IsWriteLog(int i) => (filelog[i] != default) && GuiApp.IsWriteLog;
        private static bool IsIsViewLog() => (gui != default) && GuiApp.IsViewLog;
        private static bool IsIsViewEvent() => (gui != default) && GuiApp.IsViewEvent;
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.IO;

namespace Terminal.Gui {

	public static class Application {
		static Stack<Toplevel> toplevels = new Stack<Toplevel> ();

		public static ConsoleDriver Driver;

		public static List<Toplevel> MdiChildes {
			get {
				if (MdiTop != null) {
					List<Toplevel> mdiChildes = new List<Toplevel> ();
					foreach (var top in toplevels) {
						if (top != MdiTop && !top.Modal) {
							mdiChildes.Add (top);
						}
					}
					return mdiChildes;
				}
				return null;
			}
		}

		public static Toplevel MdiTop {
			get {
				if (Top.IsMdiContainer) {
					return Top;
				}
				return null;
			}
		}

		public static Toplevel Top { get; private set; }

		public static Toplevel Current { get; private set; }

		public static bool HeightAsBuffer {
			get {
				if (Driver == null) {
					throw new ArgumentNullException ("The driver must be initialized first.");
				}
				return Driver.HeightAsBuffer;
			}
			set {
				if (Driver == null) {
					throw new ArgumentNullException ("The driver must be initialized first.");
				}
				Driver.HeightAsBuffer = value;
			}
		}

		public static bool AlwaysSetPosition {
			get {
				if (Driver is NetDriver) {
					return (Driver as NetDriver).AlwaysSetPosition;
				}
				return false;
			}
			set {
				if (Driver is NetDriver) {
					(Driver as NetDriver).AlwaysSetPosition = value;
					Driver.Refresh ();
				}
			}
		}

		static Key alternateForwardKey = Key.PageDown | Key.CtrlMask;

		public static Key AlternateForwardKey {
			get => alternateForwardKey;
			set {
				if (alternateForwardKey != value) {
					var oldKey = alternateForwardKey;
					alternateForwardKey = value;
					OnAlternateForwardKeyChanged (oldKey);
				}
			}
		}

		static void OnAlternateForwardKeyChanged (Key oldKey)
		{
			foreach (var top in toplevels) {
				top.OnAlternateForwardKeyChanged (oldKey);
			}
		}

		static Key alternateBackwardKey = Key.PageUp | Key.CtrlMask;

		public static Key AlternateBackwardKey {
			get => alternateBackwardKey;
			set {
				if (alternateBackwardKey != value) {
					var oldKey = alternateBackwardKey;
					alternateBackwardKey = value;
					OnAlternateBackwardKeyChanged (oldKey);
				}
			}
		}

		static void OnAlternateBackwardKeyChanged (Key oldKey)
		{
			foreach (var top in toplevels) {
				top.OnAlternateBackwardKeyChanged (oldKey);
			}
		}

		static Key quitKey = Key.Q | Key.CtrlMask;

		public static Key QuitKey {
			get => quitKey;
			set {
				if (quitKey != value) {
					var oldKey = quitKey;
					quitKey = value;
					OnQuitKeyChanged (oldKey);
				}
			}
		}

		private static List<CultureInfo> supportedCultures;

		public static List<CultureInfo> SupportedCultures => supportedCultures;

		static void OnQuitKeyChanged (Key oldKey)
		{
			foreach (var top in toplevels) {
				top.OnQuitKeyChanged (oldKey);
			}
		}

		public static MainLoop MainLoop { get; private set; }

		public static bool IsMouseDisabled { get; set; }

		public static Action Iteration;

		public static Rect MakeCenteredRect (Size size)
		{
			return new Rect (new Point ((Driver.Cols - size.Width) / 2, (Driver.Rows - size.Height) / 2), size);
		}

		class MainLoopSyncContext : SynchronizationContext {
			MainLoop mainLoop;

			public MainLoopSyncContext (MainLoop mainLoop)
			{
				this.mainLoop = mainLoop;
			}

			public override SynchronizationContext CreateCopy ()
			{
				return new MainLoopSyncContext (MainLoop);
			}

			public override void Post (SendOrPostCallback d, object state)
			{
				mainLoop.AddIdle (() => {
					d (state);
					return false;
				});
			}

			public override void Send (SendOrPostCallback d, object state)
			{
				mainLoop.Invoke (() => {
					d (state);
				});
			}
		}

		public static bool UseSystemConsole;

		public static void Init (ConsoleDriver driver = null, IMainLoopDriver mainLoopDriver = null) => Init (() => Toplevel.Create (), driver, mainLoopDriver);

		internal static bool _initialized = false;

		static void Init (Func<Toplevel> topLevelFactory, ConsoleDriver driver = null, IMainLoopDriver mainLoopDriver = null)
		{
			if (_initialized && driver == null) return;

			if (_initialized) {
				throw new InvalidOperationException ("Init must be bracketed by Shutdown");
			}

			ResetState ();

			if (driver != null) {
				if (mainLoopDriver == null) {
					throw new ArgumentNullException ("mainLoopDriver cannot be null if driver is provided.");
				}
				Driver = driver;
				Driver.Init (TerminalResized);
				MainLoop = new MainLoop (mainLoopDriver);
				SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext (MainLoop));
			}

			if (Driver == null) {
				var p = Environment.OSVersion.Platform;
				if (UseSystemConsole) {
					Driver = new NetDriver ();
					mainLoopDriver = new NetMainLoop (Driver);
				} else if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows) {
					Driver = new WindowsDriver ();
					mainLoopDriver = new WindowsMainLoop (Driver);
				} else {
					mainLoopDriver = new UnixMainLoop ();
					Driver = new CursesDriver ();
				}
				Driver.Init (TerminalResized);
				MainLoop = new MainLoop (mainLoopDriver);
				SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext (MainLoop));
			}
			Top = topLevelFactory ();
			Current = Top;
			supportedCultures = GetSupportedCultures ();
			_initialized = true;
		}

		public class RunState : IDisposable {
			public RunState (Toplevel view)
			{
				Toplevel = view;
			}
			internal Toplevel Toplevel;

			public void Dispose ()
			{
				Dispose (true);
				GC.SuppressFinalize (this);
			}

			protected virtual void Dispose (bool disposing)
			{
				if (Toplevel != null && disposing) {
					End (Toplevel);
					Toplevel.Dispose ();
					Toplevel = null;
				}
			}
		}

		static void ProcessKeyEvent (KeyEvent ke)
		{
			if(RootKeyEvent?.Invoke(ke) ?? false) {
				return;
			}

			var chain = toplevels.ToList ();
			foreach (var topLevel in chain) {
				if (topLevel.ProcessHotKey (ke))
					return;
				if (topLevel.Modal)
					break;
			}

			foreach (var topLevel in chain) {
				if (topLevel.ProcessKey (ke))
					return;
				if (topLevel.Modal)
					break;
			}

			foreach (var topLevel in chain) {
				if (topLevel.ProcessColdKey (ke))
					return;
				if (topLevel.Modal)
					break;
			}
		}

		static void ProcessKeyDownEvent (KeyEvent ke)
		{
			var chain = toplevels.ToList ();
			foreach (var topLevel in chain) {
				if (topLevel.OnKeyDown (ke))
					return;
				if (topLevel.Modal)
					break;
			}
		}


		static void ProcessKeyUpEvent (KeyEvent ke)
		{
			var chain = toplevels.ToList ();
			foreach (var topLevel in chain) {
				if (topLevel.OnKeyUp (ke))
					return;
				if (topLevel.Modal)
					break;
			}
		}

		static View FindDeepestTop (Toplevel start, int x, int y, out int resx, out int resy)
		{
			var startFrame = start.Frame;

			if (!startFrame.Contains (x, y)) {
				resx = 0;
				resy = 0;
				return null;
			}

			if (toplevels != null) {
				int count = toplevels.Count;
				if (count > 0) {
					var rx = x - startFrame.X;
					var ry = y - startFrame.Y;
					foreach (var t in toplevels) {
						if (t != Current) {
							if (t != start && t.Visible && t.Frame.Contains (rx, ry)) {
								start = t;
								break;
							}
						}
					}
				}
			}
			resx = x - startFrame.X;
			resy = y - startFrame.Y;
			return start;
		}

		static View FindDeepestMdiView (View start, int x, int y, out int resx, out int resy)
		{
			if (start.GetType ().BaseType != typeof (Toplevel)
				&& !((Toplevel)start).IsMdiContainer) {
				resx = 0;
				resy = 0;
				return null;
			}

			var startFrame = start.Frame;

			if (!startFrame.Contains (x, y)) {
				resx = 0;
				resy = 0;
				return null;
			}

			int count = toplevels.Count;
			for (int i = count - 1; i >= 0; i--) {
				foreach (var top in toplevels) {
					var rx = x - startFrame.X;
					var ry = y - startFrame.Y;
					if (top.Visible && top.Frame.Contains (rx, ry)) {
						var deep = FindDeepestView (top, rx, ry, out resx, out resy);
						if (deep == null)
							return FindDeepestMdiView (top, rx, ry, out resx, out resy);
						if (deep != MdiTop)
							return deep;
					}
				}
			}
			resx = x - startFrame.X;
			resy = y - startFrame.Y;
			return start;
		}

		static View FindDeepestView (View start, int x, int y, out int resx, out int resy)
		{
			var startFrame = start.Frame;

			if (!startFrame.Contains (x, y)) {
				resx = 0;
				resy = 0;
				return null;
			}

			if (start.InternalSubviews != null) {
				int count = start.InternalSubviews.Count;
				if (count > 0) {
					var rx = x - startFrame.X;
					var ry = y - startFrame.Y;
					for (int i = count - 1; i >= 0; i--) {
						View v = start.InternalSubviews [i];
						if (v.Visible && v.Frame.Contains (rx, ry)) {
							var deep = FindDeepestView (v, rx, ry, out resx, out resy);
							if (deep == null)
								return v;
							return deep;
						}
					}
				}
			}
			resx = x - startFrame.X;
			resy = y - startFrame.Y;
			return start;
		}

		static View FindTopFromView (View view)
		{
			View top = view?.SuperView != null && view?.SuperView != Top
				? view.SuperView : view;

			while (top?.SuperView != null && top?.SuperView != Top) {
				top = top.SuperView;
			}
			return top;
		}

		internal static View mouseGrabView;

		public static void GrabMouse (View view)
		{
			if (view == null)
				return;
			mouseGrabView = view;
			Driver.UncookMouse ();
		}

		public static void UngrabMouse ()
		{
			mouseGrabView = null;
			Driver.CookMouse ();
		}

		public static Action<MouseEvent> RootMouseEvent;

		public static Func<KeyEvent,bool> RootKeyEvent;

		internal static View wantContinuousButtonPressedView;
		static View lastMouseOwnerView;

		static void ProcessMouseEvent (MouseEvent me)
		{
			if (IsMouseDisabled) {
				return;
			}

			var view = FindDeepestView (Current, me.X, me.Y, out int rx, out int ry);

			if (view != null && view.WantContinuousButtonPressed)
				wantContinuousButtonPressedView = view;
			else
				wantContinuousButtonPressedView = null;
			if (view != null) {
				me.View = view;
			}
			RootMouseEvent?.Invoke (me);
			if (mouseGrabView != null) {
				var newxy = mouseGrabView.ScreenToView (me.X, me.Y);
				var nme = new MouseEvent () {
					X = newxy.X,
					Y = newxy.Y,
					Flags = me.Flags,
					OfX = me.X - newxy.X,
					OfY = me.Y - newxy.Y,
					View = view
				};
				if (OutsideFrame (new Point (nme.X, nme.Y), mouseGrabView.Frame)) {
					lastMouseOwnerView?.OnMouseLeave (me);
				}
				if (mouseGrabView != null) {
					mouseGrabView.OnMouseEvent (nme);
					return;
				}
			}

			if ((view == null || view == MdiTop) && !Current.Modal && MdiTop != null
				&& me.Flags != MouseFlags.ReportMousePosition && me.Flags != 0) {

				var top = FindDeepestTop (Top, me.X, me.Y, out _, out _);
				view = FindDeepestView (top, me.X, me.Y, out rx, out ry);

				if (view != null && view != MdiTop && top != Current) {
					MoveCurrent ((Toplevel)top);
				}
			}

			if (view != null) {
				var nme = new MouseEvent () {
					X = rx,
					Y = ry,
					Flags = me.Flags,
					OfX = 0,
					OfY = 0,
					View = view
				};

				if (lastMouseOwnerView == null) {
					lastMouseOwnerView = view;
					view.OnMouseEnter (nme);
				} else if (lastMouseOwnerView != view) {
					lastMouseOwnerView.OnMouseLeave (nme);
					view.OnMouseEnter (nme);
					lastMouseOwnerView = view;
				}

				if (!view.WantMousePositionReports && me.Flags == MouseFlags.ReportMousePosition)
					return;

				if (view.WantContinuousButtonPressed)
					wantContinuousButtonPressedView = view;
				else
					wantContinuousButtonPressedView = null;

				view.OnMouseEvent (nme);

				EnsuresTopOnFront ();
			}
		}

		static bool MoveCurrent (Toplevel top)
		{
			if (MdiTop != null && top != MdiTop && top != Current && Current?.Modal == true && !toplevels.Peek ().Modal) {
				lock (toplevels) {
					toplevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
				}
				var index = 0;
				var savedToplevels = toplevels.ToArray ();
				foreach (var t in savedToplevels) {
					if (!t.Modal && t != Current && t != top && t != savedToplevels [index]) {
						lock (toplevels) {
							toplevels.MoveTo (top, index, new ToplevelEqualityComparer ());
						}
					}
					index++;
				}
				return false;
			}
			if (MdiTop != null && top != MdiTop && top != Current && Current?.Running == false && !top.Running) {
				lock (toplevels) {
					toplevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
				}
				var index = 0;
				foreach (var t in toplevels.ToArray ()) {
					if (!t.Running && t != Current && index > 0) {
						lock (toplevels) {
							toplevels.MoveTo (top, index - 1, new ToplevelEqualityComparer ());
						}
					}
					index++;
				}
				return false;
			}
			if ((MdiTop != null && top?.Modal == true && toplevels.Peek () != top)
				|| (MdiTop != null && Current != MdiTop && Current?.Modal == false && top == MdiTop)
				|| (MdiTop != null && Current?.Modal == false && top != Current)
				|| (MdiTop != null && Current?.Modal == true && top == MdiTop)) {
				lock (toplevels) {
					toplevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
					Current = top;
				}
			}
			return true;
		}

		static bool OutsideFrame (Point p, Rect r)
		{
			return p.X < 0 || p.X > r.Width - 1 || p.Y < 0 || p.Y > r.Height - 1;
		}

		public static RunState Begin (Toplevel toplevel)
		{
			if (toplevel == null) {
				throw new ArgumentNullException (nameof (toplevel));
			} else if (toplevel.IsMdiContainer && MdiTop != null) {
				throw new InvalidOperationException ("Only one Mdi Container is allowed.");
			}

			var rs = new RunState (toplevel);

			Init ();
			if (toplevel is ISupportInitializeNotification initializableNotification &&
			    !initializableNotification.IsInitialized) {
				initializableNotification.BeginInit ();
				initializableNotification.EndInit ();
			} else if (toplevel is ISupportInitialize initializable) {
				initializable.BeginInit ();
				initializable.EndInit ();
			}

			lock (toplevels) {
				if (string.IsNullOrEmpty (toplevel.Id.ToString ())) {
					var count = 1;
					var id = (toplevels.Count + count).ToString ();
					while (toplevels.Count > 0 && toplevels.FirstOrDefault (x => x.Id.ToString () == id) != null) {
						count++;
						id = (toplevels.Count + count).ToString ();
					}
					toplevel.Id = (toplevels.Count + count).ToString ();

					toplevels.Push (toplevel);
				} else {
					var dup = toplevels.FirstOrDefault (x => x.Id.ToString () == toplevel.Id);
					if (dup == null) {
						toplevels.Push (toplevel);
					}
				}

				if (toplevels.FindDuplicates (new ToplevelEqualityComparer ()).Count > 0) {
					throw new ArgumentException ("There are duplicates toplevels Id's");
				}
			}
			if (toplevel.IsMdiContainer) {
				Top = toplevel;
			}

			var refreshDriver = true;
			if (MdiTop == null || toplevel.IsMdiContainer || (Current?.Modal == false && toplevel.Modal)
				|| (Current?.Modal == false && !toplevel.Modal) || (Current?.Modal == true && toplevel.Modal)) {

				if (toplevel.Visible) {
					Current = toplevel;
					SetCurrentAsTop ();
				} else {
					refreshDriver = false;
				}
			} else if ((MdiTop != null && toplevel != MdiTop && Current?.Modal == true && !toplevels.Peek ().Modal)
				|| (MdiTop != null && toplevel != MdiTop && Current?.Running == false)) {
				refreshDriver = false;
				MoveCurrent (toplevel);
			} else {
				refreshDriver = false;
				MoveCurrent (Current);
			}

			Driver.PrepareToRun (MainLoop, ProcessKeyEvent, ProcessKeyDownEvent, ProcessKeyUpEvent, ProcessMouseEvent);
			if (toplevel.LayoutStyle == LayoutStyle.Computed)
				toplevel.SetRelativeLayout (new Rect (0, 0, Driver.Cols, Driver.Rows));
			toplevel.LayoutSubviews ();
			toplevel.PositionToplevels ();
			toplevel.WillPresent ();
			if (refreshDriver) {
				if (MdiTop != null) {
					MdiTop.OnChildLoaded (toplevel);
				}
				toplevel.OnLoaded ();
				Redraw (toplevel);
				toplevel.PositionCursor ();
				Driver.Refresh ();
			}

			return rs;
		}

		public static void End (RunState runState)
		{
			if (runState == null)
				throw new ArgumentNullException (nameof (runState));

			if (MdiTop != null) {
				MdiTop.OnChildUnloaded (runState.Toplevel);
			} else {
				runState.Toplevel.OnUnloaded ();
			}
			runState.Dispose ();
		}

		public static void Shutdown ()
		{
			ResetState ();
		}

		static void ResetState ()
		{
			foreach (var t in toplevels) {
				t.Running = false;
				t.Dispose ();
			}
			toplevels.Clear ();
			Current = null;
			Top = null;

			MainLoop = null;
			Driver?.End ();
			Driver = null;
			Iteration = null;
			RootMouseEvent = null;
			RootKeyEvent = null;
			Resized = null;
			_initialized = false;
			mouseGrabView = null;

			SynchronizationContext.SetSynchronizationContext (syncContext: null);
		}


		static void Redraw (View view)
		{
			view.Redraw (view.Bounds);
			Driver.Refresh ();
		}

		static void Refresh (View view)
		{
			view.Redraw (view.Bounds);
			Driver.Refresh ();
		}

		public static void Refresh ()
		{
			Driver.UpdateScreen ();
			View last = null;
			foreach (var v in toplevels.Reverse ()) {
				if (v.Visible) {
					v.SetNeedsDisplay ();
					v.Redraw (v.Bounds);
				}
				last = v;
			}
			last?.PositionCursor ();
			Driver.Refresh ();
		}

		internal static void End (View view)
		{
			if (toplevels.Peek () != view)
				throw new ArgumentException ("The view that you end with must be balanced");
			toplevels.Pop ();

			(view as Toplevel)?.OnClosed ((Toplevel)view);

			if (MdiTop != null && !((Toplevel)view).Modal && view != MdiTop) {
				MdiTop.OnChildClosed (view as Toplevel);
			}

			if (toplevels.Count == 0) {
				Current = null;
			} else {
				Current = toplevels.Peek ();
				if (toplevels.Count == 1 && Current == MdiTop) {
					MdiTop.OnAllChildClosed ();
				} else {
					SetCurrentAsTop ();
				}
				Refresh ();
			}
		}

		public static void RunLoop (RunState state, bool wait = true)
		{
			if (state == null)
				throw new ArgumentNullException (nameof (state));
			if (state.Toplevel == null)
				throw new ObjectDisposedException ("state");

			bool firstIteration = true;
			for (state.Toplevel.Running = true; state.Toplevel.Running;) {
				if (MainLoop.EventsPending (wait)) {
					if (firstIteration) {
						state.Toplevel.OnReady ();
					}
					firstIteration = false;

					MainLoop.MainIteration ();
					Iteration?.Invoke ();

					EnsureModalOrVisibleAlwaysOnTop (state.Toplevel);
					if ((state.Toplevel != Current && Current?.Modal == true)
						|| (state.Toplevel != Current && Current?.Modal == false)) {
						MdiTop?.OnDeactivate (state.Toplevel);
						state.Toplevel = Current;
						MdiTop?.OnActivate (state.Toplevel);
						Top.SetChildNeedsDisplay ();
						Refresh ();
					}
					if (Driver.EnsureCursorVisibility ()) {
						state.Toplevel.SetNeedsDisplay ();
					}
				} else if (!wait) {
					return;
				}
				if (state.Toplevel != Top
					&& (!Top.NeedDisplay.IsEmpty || Top.ChildNeedsDisplay || Top.LayoutNeeded)) {
					Top.Redraw (Top.Bounds);
					state.Toplevel.SetNeedsDisplay (state.Toplevel.Bounds);
				}
				if (!state.Toplevel.NeedDisplay.IsEmpty || state.Toplevel.ChildNeedsDisplay || state.Toplevel.LayoutNeeded
					|| MdiChildNeedsDisplay ()) {
					state.Toplevel.Redraw (state.Toplevel.Bounds);
					if (DebugDrawBounds) {
						DrawBounds (state.Toplevel);
					}
					state.Toplevel.PositionCursor ();
					Driver.Refresh ();
				} else {
					Driver.UpdateCursor ();
				}
				if (state.Toplevel != Top && !state.Toplevel.Modal
					&& (!Top.NeedDisplay.IsEmpty || Top.ChildNeedsDisplay || Top.LayoutNeeded)) {
					Top.Redraw (Top.Bounds);
				}
			}
		}

		static void EnsureModalOrVisibleAlwaysOnTop (Toplevel toplevel)
		{
			if (!toplevel.Running || (toplevel == Current && toplevel.Visible) || MdiTop == null || toplevels.Peek ().Modal) {
				return;
			}

			foreach (var top in toplevels.Reverse ()) {
				if (top.Modal && top != Current) {
					MoveCurrent (top);
					return;
				}
			}
			if (!toplevel.Visible && toplevel == Current) {
				MoveNext ();
			}
		}

		static bool MdiChildNeedsDisplay ()
		{
			if (MdiTop == null) {
				return false;
			}

			foreach (var top in toplevels) {
				if (top != Current && top.Visible && (!top.NeedDisplay.IsEmpty || top.ChildNeedsDisplay || top.LayoutNeeded)) {
					MdiTop.SetChildNeedsDisplay ();
					return true;
				}
			}
			return false;
		}

		internal static bool DebugDrawBounds = false;

		static void DrawBounds (View v)
		{
			v.DrawFrame (v.Frame, padding: 0, fill: false);
			if (v.InternalSubviews != null && v.InternalSubviews.Count > 0)
				foreach (var sub in v.InternalSubviews)
					DrawBounds (sub);
		}

		public static void Run (Func<Exception, bool> errorHandler = null)
		{
			Run (Top, errorHandler);
		}

		public static void Run<T> (Func<Exception, bool> errorHandler = null) where T : Toplevel, new()
		{
			if (_initialized && Driver != null) {
				var top = new T ();
				if (top.GetType ().BaseType != typeof (Toplevel)) {
					throw new ArgumentException (top.GetType ().BaseType.Name);
				}
				Run (top, errorHandler);
			} else {
				Init (() => new T ());
				Run (Top, errorHandler);
			}
		}

		public static void Run (Toplevel view, Func<Exception, bool> errorHandler = null)
		{
			var resume = true;
			while (resume) {
#if !DEBUG
				try {
#endif
				resume = false;
				var runToken = Begin (view);
				RunLoop (runToken);
				End (runToken);
#if !DEBUG
				}
				catch (Exception error)
				{
					if (errorHandler == null)
					{
						throw;
					}
					resume = errorHandler(error);
				}
#endif
			}
		}

		public static void RequestStop (Toplevel top = null)
		{
			if (MdiTop == null || top == null || (MdiTop == null && top != null)) {
				top = Current;
			}

			if (MdiTop != null && top.IsMdiContainer && top?.Running == true
				&& (Current?.Modal == false || (Current?.Modal == true && Current?.Running == false))) {

				MdiTop.RequestStop ();
			} else if (MdiTop != null && top != Current && Current?.Running == true && Current?.Modal == true
				&& top.Modal && top.Running) {

				var ev = new ToplevelClosingEventArgs (Current);
				Current.OnClosing (ev);
				if (ev.Cancel) {
					return;
				}
				ev = new ToplevelClosingEventArgs (top);
				top.OnClosing (ev);
				if (ev.Cancel) {
					return;
				}
				Current.Running = false;
				top.Running = false;
			} else if ((MdiTop != null && top != MdiTop && top != Current && Current?.Modal == false
				&& Current?.Running == true && !top.Running)
				|| (MdiTop != null && top != MdiTop && top != Current && Current?.Modal == false
				&& Current?.Running == false && !top.Running && toplevels.ToArray () [1].Running)) {

				MoveCurrent (top);
			} else if (MdiTop != null && Current != top && Current?.Running == true && !top.Running
				&& Current?.Modal == true && top.Modal) {
				Current.Running = false;
			} else if (MdiTop != null && Current == top && MdiTop?.Running == true && Current?.Running == true && top.Running
				&& Current?.Modal == true && top.Modal) {
				Current.Running = false;
			} else {
				Toplevel currentTop;
				if (top == Current || (Current?.Modal == true && !top.Modal)) {
					currentTop = Current;
				} else {
					currentTop = top;
				}
				if (!currentTop.Running) {
					return;
				}
				var ev = new ToplevelClosingEventArgs (currentTop);
				currentTop.OnClosing (ev);
				if (ev.Cancel) {
					return;
				}
				currentTop.Running = false;
			}
		}

		public class ResizedEventArgs : EventArgs {
			public int Rows { get; set; }
			public int Cols { get; set; }
		}

		public static Action<ResizedEventArgs> Resized;

		static void TerminalResized ()
		{
			var full = new Rect (0, 0, Driver.Cols, Driver.Rows);
			SetToplevelsSize (full);
			Resized?.Invoke (new ResizedEventArgs () { Cols = full.Width, Rows = full.Height });
			Driver.Clip = full;
			foreach (var t in toplevels) {
				t.SetRelativeLayout (full);
				t.LayoutSubviews ();
				t.PositionToplevels ();
				t.OnResized (full.Size);
			}
			Refresh ();
		}

		static void SetToplevelsSize (Rect full)
		{
			if (MdiTop == null) {
				foreach (var t in toplevels) {
					if (t?.SuperView == null && !t.Modal) {
						t.Frame = full;
						t.Width = full.Width;
						t.Height = full.Height;
					}
				}
			} else {
				Top.Frame = full;
				Top.Width = full.Width;
				Top.Height = full.Height;
			}
		}

		static bool SetCurrentAsTop ()
		{
			if (MdiTop == null && Current != Top && Current?.SuperView == null && Current?.Modal == false) {
				if (Current.Frame != new Rect (0, 0, Driver.Cols, Driver.Rows)) {
					Current.Frame = new Rect (0, 0, Driver.Cols, Driver.Rows);
				}
				Top = Current;
				return true;
			}
			return false;
		}

		public static void MoveNext ()
		{
			if (MdiTop != null && !Current.Modal) {
				lock (toplevels) {
					toplevels.MoveNext ();
					var isMdi = false;
					while (toplevels.Peek () == MdiTop || !toplevels.Peek ().Visible) {
						if (!isMdi && toplevels.Peek () == MdiTop) {
							isMdi = true;
						} else if (isMdi && toplevels.Peek () == MdiTop) {
							MoveCurrent (Top);
							break;
						}
						toplevels.MoveNext ();
					}
					Current = toplevels.Peek ();
				}
			}
		}

		public static void MovePrevious ()
		{
			if (MdiTop != null && !Current.Modal) {
				lock (toplevels) {
					toplevels.MovePrevious ();
					var isMdi = false;
					while (toplevels.Peek () == MdiTop || !toplevels.Peek ().Visible) {
						if (!isMdi && toplevels.Peek () == MdiTop) {
							isMdi = true;
						} else if (isMdi && toplevels.Peek () == MdiTop) {
							MoveCurrent (Top);
							break;
						}
						toplevels.MovePrevious ();
					}
					Current = toplevels.Peek ();
				}
			}
		}

		internal static bool ShowChild (Toplevel top)
		{
			if (top.Visible && MdiTop != null && Current?.Modal == false) {
				lock (toplevels) {
					toplevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
					Current = top;
				}
				return true;
			}
			return false;
		}

		public static void DoEvents ()
		{
			MainLoop.Driver.Wakeup ();
		}

		public static void EnsuresTopOnFront ()
		{
			if (MdiTop != null) {
				return;
			}
			var top = FindTopFromView (Top?.MostFocused);
			if (top != null && Top.Subviews.Count > 1 && Top.Subviews [Top.Subviews.Count - 1] != top) {
				Top.BringSubviewToFront (top);
			}
		}

		internal static List<CultureInfo> GetSupportedCultures ()
		{
			CultureInfo [] culture = CultureInfo.GetCultures (CultureTypes.AllCultures);

			Assembly assembly = Assembly.GetExecutingAssembly ();

			string assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;

			string resourceFilename = $"{Path.GetFileNameWithoutExtension (assembly.Location)}.resources.dll";

			return culture.Where (cultureInfo =>
			     assemblyLocation != null &&
			     Directory.Exists (Path.Combine (assemblyLocation, cultureInfo.Name)) &&
			     File.Exists (Path.Combine (assemblyLocation, cultureInfo.Name, resourceFilename))
			).ToList ();
		}
	}
}

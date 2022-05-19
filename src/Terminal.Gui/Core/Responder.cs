using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Terminal.Gui {
	public class Responder : IDisposable {
		bool disposedValue;

#if DEBUG_IDISPOSABLE
		/// <summary>
		/// For debug purposes to verify objects are being disposed properly
		/// </summary>
		public bool WasDisposed = false;
		/// <summary>
		/// For debug purposes to verify objects are being disposed properly
		/// </summary>
		public int DisposedCount = 0;
		/// <summary>
		/// For debug purposes
		/// </summary>
		public static List<Responder> Instances = new List<Responder> ();
		/// <summary>
		/// For debug purposes
		/// </summary>
		public Responder ()
		{
			Instances.Add (this);
		}
#endif

		public virtual bool CanFocus { get; set; }

		public virtual bool HasFocus { get; }

		public virtual bool Enabled { get; set; } = true;

		public virtual bool Visible { get; set; } = true;

		public virtual bool ProcessHotKey (KeyEvent kb)
		{
			return false;
		}

		public virtual bool ProcessKey (KeyEvent keyEvent)
		{
			return false;
		}

		public virtual bool ProcessColdKey (KeyEvent keyEvent)
		{
			return false;
		}

		public virtual bool OnKeyDown (KeyEvent keyEvent)
		{
			return false;
		}

		public virtual bool OnKeyUp (KeyEvent keyEvent)
		{
			return false;
		}

		public virtual bool MouseEvent (MouseEvent mouseEvent)
		{
			return false;
		}

		public virtual bool OnMouseEnter (MouseEvent mouseEvent)
		{
			return false;
		}

		public virtual bool OnMouseLeave (MouseEvent mouseEvent)
		{
			return false;
		}

		public virtual bool OnEnter (View view)
		{
			return false;
		}

		public virtual bool OnLeave (View view)
		{
			return false;
		}

		public virtual void OnCanFocusChanged () { }

		public virtual void OnEnabledChanged () { }

		public virtual void OnVisibleChanged () { }

		protected virtual void Dispose (bool disposing)
		{
			if (!disposedValue) {
				if (disposing) {
				}

				disposedValue = true;
			}
		}

		public void Dispose ()
		{
			Dispose (disposing: true);
			GC.SuppressFinalize (this);
#if DEBUG_IDISPOSABLE
			WasDisposed = true;
#endif
		}
	}
}

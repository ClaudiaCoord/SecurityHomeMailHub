using System;
using NStack;
using System.Linq;
using System.Collections.Generic;

namespace Terminal.Gui {

	[Flags]
	public enum MenuItemCheckStyle {
		NoCheck = 0b_0000_0000,
		Checked = 0b_0000_0001,
		Radio = 0b_0000_0010,
	};

	public class MenuItem {
		ustring title;
		ShortcutHelper shortcutHelper;
		internal int TitleLength => GetMenuBarItemLength (Title);

		public object Data { get; set; }

		public MenuItem (Key shortcut = Key.Null) : this ("", "", null, null, null, shortcut) { }

		public MenuItem (ustring title, ustring help, Action action, Func<bool> canExecute = null, MenuItem parent = null, Key shortcut = Key.Null)
		{
			Title = title ?? "";
			Help = help ?? "";
			Action = action;
			CanExecute = canExecute;
			Parent = parent;
			shortcutHelper = new ShortcutHelper ();
			if (shortcut != Key.Null) {
				shortcutHelper.Shortcut = shortcut;
			}
		}

		public Rune HotKey;

		public Key Shortcut {
			get => shortcutHelper.Shortcut;
			set {
				if (shortcutHelper.Shortcut != value && (ShortcutHelper.PostShortcutValidation (value) || value == Key.Null)) {
					shortcutHelper.Shortcut = value;
				}
			}
		}

		public ustring ShortcutTag => ShortcutHelper.GetShortcutTag (shortcutHelper.Shortcut);

		public ustring Title {
			get { return title; }
			set {
				if (title != value) {
					title = value;
					GetHotKey ();
				}
			}
		}

		public ustring Help { get; set; }

		public Action Action { get; set; }

		public Func<bool> CanExecute { get; set; }

		public bool IsEnabled ()
		{
			return CanExecute == null ? true : CanExecute ();
		}

		internal int Width => 1 + TitleLength + (Help.ConsoleWidth > 0 ? Help.ConsoleWidth + 2 : 0) +
			(Checked || CheckType.HasFlag (MenuItemCheckStyle.Checked) || CheckType.HasFlag (MenuItemCheckStyle.Radio) ? 2 : 0) +
			(ShortcutTag.ConsoleWidth > 0 ? ShortcutTag.ConsoleWidth + 2 : 0) + 2;

		public bool Checked { set; get; }

		public MenuItemCheckStyle CheckType { get; set; }

		public MenuItem Parent { get; internal set; }

		internal bool IsFromSubMenu { get { return Parent != null; } }

		public MenuItem GetMenuItem ()
		{
			return this;
		}

		public bool GetMenuBarItem ()
		{
			return IsFromSubMenu;
		}

		void GetHotKey ()
		{
			bool nextIsHot = false;
			foreach (var x in title) {
				if (x == MenuBar.HotKeySpecifier) {
					nextIsHot = true;
				} else {
					if (nextIsHot) {
						HotKey = Char.ToUpper ((char)x);
						break;
					}
					nextIsHot = false;
					HotKey = default;
				}
			}
		}

		int GetMenuBarItemLength (ustring title)
		{
			int len = 0;
			foreach (var ch in title) {
				if (ch == MenuBar.HotKeySpecifier)
					continue;
				len += Math.Max (Rune.ColumnWidth (ch), 1);
			}

			return len;
		}
	}

	public class MenuBarItem : MenuItem {
		public MenuBarItem (ustring title, ustring help, Action action, Func<bool> canExecute = null, MenuItem parent = null) : base (title, help, action, canExecute, parent)
		{
			Initialize (title, null, null, true);
		}

		public MenuBarItem (ustring title, MenuItem [] children, MenuItem parent = null)
		{
			Initialize (title, children, parent);
		}

		public MenuBarItem (ustring title, List<MenuItem []> children, MenuItem parent = null)
		{
			Initialize (title, children, parent);
		}

		public MenuBarItem (MenuItem [] children) : this ("", children) { }

		public MenuBarItem () : this (children: new MenuItem [] { }) { }

		void Initialize (ustring title, object children, MenuItem parent = null, bool isTopLevel = false)
		{
			if (!isTopLevel && children == null) {
				throw new ArgumentNullException (nameof (children), "The parameter cannot be null. Use an empty array instead.");
			}
			SetTitle (title ?? "");
			if (parent != null) {
				Parent = parent;
			}
			if (children is List<MenuItem []>) {
				MenuItem [] childrens = new MenuItem [] { };
				foreach (var item in (List<MenuItem []>)children) {
					for (int i = 0; i < item.Length; i++) {
						SetChildrensParent (item);
						Array.Resize (ref childrens, childrens.Length + 1);
						childrens [childrens.Length - 1] = item [i];
					}
				}
				Children = childrens;
			} else if (children is MenuItem []) {
				SetChildrensParent ((MenuItem [])children);
				Children = (MenuItem [])children;
			} else {
				Children = null;
			}
		}

		void SetChildrensParent (MenuItem [] childrens)
		{
			foreach (var child in childrens) {
				if (child != null && child.Parent == null) {
					child.Parent = this;
				}
			}
		}

		public MenuBarItem SubMenu (MenuItem children)
		{
			return children as MenuBarItem;
		}

		public bool IsSubMenuOf (MenuItem menuItem)
		{
			if ((Children == null) || (menuItem == null))
				return false;
			foreach (var child in Children) {
				if (child == menuItem && child.Parent == menuItem.Parent) {
					return true;
				}
			}
			return false;
		}

		public int GetChildrenIndex (MenuItem children)
		{
			if (Children?.Length == 0) {
				return -1;
			}
			int i = 0;
			foreach (var child in Children) {
				if (child == children) {
					return i;
				}
				i++;
			}
			return -1;
		}

		void SetTitle (ustring title)
		{
			if (title == null)
				title = "";
			Title = title;
		}

		public MenuItem [] Children { get; set; }

		internal bool IsTopLevel { get => Parent == null && (Children == null || Children.Length == 0) && Action != null; }
	}

	class Menu : View {
		internal MenuBarItem barItems;
		internal MenuBar host;
		internal int current;
		internal View previousSubFocused;

		internal static Rect MakeFrame (int x, int y, MenuItem [] items)
		{
			if (items == null || items.Length == 0) {
				return new Rect ();
			}
			int maxW = items.Max (z => z?.Width) ?? 0;

			return new Rect (x, y, maxW + 2, items.Length + 2);
		}

		public Menu (MenuBar host, int x, int y, MenuBarItem barItems) : base (MakeFrame (x, y, barItems.Children))
		{
			this.barItems = barItems;
			this.host = host;
			if (barItems.IsTopLevel) {
				ColorScheme = host.ColorScheme;
				CanFocus = true;
			} else {

				current = -1;
				for (int i = 0; i < barItems.Children?.Length; i++) {
					if (barItems.Children [i] != null) {
						current = i;
						break;
					}
				}
				ColorScheme = host.ColorScheme;
				CanFocus = true;
				WantMousePositionReports = host.WantMousePositionReports;
			}

			AddCommand (Command.LineUp, () => MoveUp ());
			AddCommand (Command.LineDown, () => MoveDown ());
			AddCommand (Command.Left, () => { this.host.PreviousMenu (true); return true; });
			AddCommand (Command.Right, () => {
				this.host.NextMenu (!this.barItems.IsTopLevel || (this.barItems.Children != null
					&& current > -1 && current < this.barItems.Children.Length && this.barItems.Children [current].IsFromSubMenu),
					current > -1 && host.UseSubMenusSingleFrame && this.barItems.SubMenu (this.barItems.Children [current]) != null);
				return true;
			});
			AddCommand (Command.Cancel, () => { CloseAllMenus (); return true; });
			AddCommand (Command.Accept, () => { RunSelected (); return true; });

			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.CursorRight, Command.Right);
			AddKeyBinding (Key.Esc, Command.Cancel);
			AddKeyBinding (Key.Enter, Command.Accept);
		}

		internal Attribute DetermineColorSchemeFor (MenuItem item, int index)
		{
			if (item != null) {
				if (index == current) return ColorScheme.Focus;
				if (!item.IsEnabled ()) return ColorScheme.Disabled;
			}
			return GetNormalColor ();
		}

		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (GetNormalColor ());
			DrawFrame (Bounds, padding: 0, fill: true);

			for (int i = Bounds.Y; i < barItems.Children.Length; i++) {
				if (i < 0)
					continue;
				var item = barItems.Children [i];
				Driver.SetAttribute (item == null ? GetNormalColor ()
					: i == current ? ColorScheme.Focus : GetNormalColor ());
				if (item == null) {
					Move (0, i + 1);
					Driver.AddRune (Driver.LeftTee);
				} else if (Frame.X + 1 < Driver.Cols)
					Move (1, i + 1);

				Driver.SetAttribute (DetermineColorSchemeFor (item, i));
				for (int p = Bounds.X; p < Frame.Width - 2; p++) {
					if (p < 0)
						continue;
					if (item == null)
						Driver.AddRune (Driver.HLine);
					else if (i == 0 && p == 0 && host.UseSubMenusSingleFrame && item.Parent.Parent != null)
						Driver.AddRune (Driver.LeftArrow);
					else if (p == Frame.Width - 3 && barItems.SubMenu (barItems.Children [i]) != null)
						Driver.AddRune (Driver.RightArrow);
					else
						Driver.AddRune (' ');
				}

				if (item == null) {
					if (SuperView?.Frame.Right - Frame.X > Frame.Width - 1) {
						Move (Frame.Width - 1, i + 1);
						Driver.AddRune (Driver.RightTee);
					}
					continue;
				}

				ustring textToDraw;
				var checkChar = Driver.Selected;
				var uncheckedChar = Driver.UnSelected;

				if (item.CheckType.HasFlag (MenuItemCheckStyle.Checked)) {
					checkChar = Driver.Checked;
					uncheckedChar = Driver.UnChecked;
				}

				if (item.Checked) {
					textToDraw = ustring.Make (new Rune [] { checkChar, ' ' }) + item.Title;
				} else if (item.CheckType.HasFlag (MenuItemCheckStyle.Checked) || item.CheckType.HasFlag (MenuItemCheckStyle.Radio)) {
					textToDraw = ustring.Make (new Rune [] { uncheckedChar, ' ' }) + item.Title;
				} else {
					textToDraw = item.Title;
				}

				ViewToScreen (2, i + 1, out int vtsCol, out _, false);
				if (vtsCol < Driver.Cols) {
					Move (2, i + 1);
					if (!item.IsEnabled ()) {
						DrawHotString (textToDraw, ColorScheme.Disabled, ColorScheme.Disabled);
					} else if (i == 0 && host.UseSubMenusSingleFrame && item.Parent.Parent != null) {
						var tf = new TextFormatter () {
							Alignment = TextAlignment.Centered,
							HotKeySpecifier = MenuBar.HotKeySpecifier,
							Text = textToDraw
						};
						tf.Draw (ViewToScreen (new Rect (2, i + 1, Frame.Width - 3, 1)),
							i == current ? ColorScheme.Focus : GetNormalColor (),
							i == current ? ColorScheme.HotFocus : ColorScheme.HotNormal,
							SuperView == null ? default : SuperView.ViewToScreen (SuperView.Bounds));
					} else {
						DrawHotString (textToDraw,
							i == current ? ColorScheme.HotFocus : ColorScheme.HotNormal,
							i == current ? ColorScheme.Focus : GetNormalColor ());
					}

					var l = item.ShortcutTag.ConsoleWidth == 0 ? item.Help.ConsoleWidth : item.Help.ConsoleWidth + item.ShortcutTag.ConsoleWidth + 2;
					var col = Frame.Width - l - 2;
					ViewToScreen (col, i + 1, out vtsCol, out _, false);
					if (vtsCol < Driver.Cols) {
						Move (col, 1 + i);
						Driver.AddStr (item.Help);

						if (!item.ShortcutTag.IsEmpty) {
							l = item.ShortcutTag.ConsoleWidth;
							Move (Frame.Width - l - 2, 1 + i);
							Driver.AddStr (item.ShortcutTag);
						}
					}
				}
			}
			PositionCursor ();
		}

		public override void PositionCursor ()
		{
			if (host == null || host.IsMenuOpen)
				if (barItems.IsTopLevel) {
					host.PositionCursor ();
				} else
					Move (2, 1 + current);
			else
				host.PositionCursor ();
		}

		public void Run (Action action)
		{
			if (action == null)
				return;

			Application.UngrabMouse ();
			host.CloseAllMenus ();
			Application.Refresh ();

			Application.MainLoop.AddIdle (() => {
				action ();
				return false;
			});
		}

		public override bool OnLeave (View view)
		{
			return host.OnLeave (view);
		}

		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			if (keyEvent.IsAlt) {
				host.CloseAllMenus ();
				return true;
			}

			return false;
		}

		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			if (keyEvent.IsAlt && keyEvent.Key == Key.AltMask) {
                OnKeyDown(keyEvent);
                return true;
			}

			return false;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			if (barItems.Children != null && Char.IsLetterOrDigit ((char)kb.KeyValue)) {
				var x = Char.ToUpper ((char)kb.KeyValue);
				var idx = -1;
				foreach (var item in barItems.Children) {
					idx++;
					if (item == null) continue;
					if (item.IsEnabled () && item.HotKey == x) {
						current = idx;
						RunSelected ();
						return true;
					}
				}
			}
			return false;
		}

		void RunSelected ()
		{
			if (barItems.IsTopLevel) {
				Run (barItems.Action);
			} else if (current > -1 && barItems.Children [current].Action != null) {
				Run (barItems.Children [current].Action);
			} else if (current == 0 && host.UseSubMenusSingleFrame
				&& barItems.Children [current].Parent.Parent != null) {

				host.PreviousMenu (barItems.Children [current].Parent.IsFromSubMenu, true);
			} else if (current > -1 && barItems.SubMenu (barItems.Children [current]) != null) {

				CheckSubMenu ();
			}
		}

		void CloseAllMenus ()
		{
			Application.UngrabMouse ();
			host.CloseAllMenus ();
		}

		bool MoveDown ()
		{
			if (barItems.IsTopLevel) {
				return true;
			}
			bool disabled;
			do {
				current++;
				if (current >= barItems.Children.Length) {
					current = 0;
				}
				if (this != host.openCurrentMenu && barItems.Children [current]?.IsFromSubMenu == true && host.selectedSub > -1) {
					host.PreviousMenu (true);
					host.SelectEnabledItem (barItems.Children, current, out current);
					host.openCurrentMenu = this;
				}
				var item = barItems.Children [current];
				if (item?.IsEnabled () != true) {
					disabled = true;
				} else {
					disabled = false;
				}
				if (!host.UseSubMenusSingleFrame && host.UseKeysUpDownAsKeysLeftRight && barItems.SubMenu (barItems.Children [current]) != null &&
					!disabled && host.IsMenuOpen) {
					if (!CheckSubMenu ())
						return false;
					break;
				}
				if (!host.IsMenuOpen) {
					host.OpenMenu (host.selected);
				}
			} while (barItems.Children [current] == null || disabled);
			SetNeedsDisplay ();
			if (!host.UseSubMenusSingleFrame)
				host.OnMenuOpened ();
			return true;
		}

		bool MoveUp ()
		{
			if (barItems.IsTopLevel || current == -1) {
				return true;
			}
			bool disabled;
			do {
				current--;
				if (host.UseKeysUpDownAsKeysLeftRight && !host.UseSubMenusSingleFrame) {
					if ((current == -1 || this != host.openCurrentMenu) && barItems.Children [current + 1].IsFromSubMenu && host.selectedSub > -1) {
						current++;
						host.PreviousMenu (true);
						if (current > 0) {
							current--;
							host.openCurrentMenu = this;
						}
						break;
					}
				}
				if (current < 0)
					current = barItems.Children.Length - 1;
				if (!host.SelectEnabledItem (barItems.Children, current, out current, false)) {
					current = 0;
					if (!host.SelectEnabledItem (barItems.Children, current, out current) && !host.CloseMenu (false)) {
						return false;
					}
					break;
				}
				var item = barItems.Children [current];
				if (item?.IsEnabled () != true) {
					disabled = true;
				} else {
					disabled = false;
				}
				if (!host.UseSubMenusSingleFrame && host.UseKeysUpDownAsKeysLeftRight && barItems.SubMenu (barItems.Children [current]) != null &&
					!disabled && host.IsMenuOpen) {
					if (!CheckSubMenu ())
						return false;
					break;
				}
			} while (barItems.Children [current] == null || disabled);
			SetNeedsDisplay ();
			if (!host.UseSubMenusSingleFrame)
				host.OnMenuOpened ();
			return true;
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (!host.handled && !host.HandleGrabView (me, this)) {
				return false;
			}
			host.handled = false;
			bool disabled;
			if (me.Flags == MouseFlags.Button1Clicked) {
				disabled = false;
				if (me.Y < 1)
					return true;
				var meY = me.Y - 1;
				if (meY >= barItems.Children.Length)
					return true;
				var item = barItems.Children [meY];
				if (item == null || !item.IsEnabled ()) disabled = true;
				current = meY;
				if (item != null && !disabled)
					RunSelected ();
				return true;
			} else if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked ||
				me.Flags == MouseFlags.Button1TripleClicked || me.Flags == MouseFlags.ReportMousePosition ||
				me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {

				disabled = false;
				if (me.Y < 1 || me.Y - 1 >= barItems.Children.Length) {
					return true;
				}
				var item = barItems.Children [me.Y - 1];
				if (item == null) return true;
				if (item == null || !item.IsEnabled ()) disabled = true;
				if (item != null && !disabled)
					current = me.Y - 1;
				if (host.UseSubMenusSingleFrame || !CheckSubMenu ()) {
					SetNeedsDisplay ();
					return true;
				}
				host.OnMenuOpened ();
				return true;
			}
			return false;
		}

		internal bool CheckSubMenu ()
		{
			if (current == -1 || barItems.Children [current] == null) {
				return true;
			}
			var subMenu = barItems.SubMenu (barItems.Children [current]);
			if (subMenu != null) {
				int pos = -1;
				if (host.openSubMenu != null) {
					pos = host.openSubMenu.FindIndex (o => o?.barItems == subMenu);
				}
				if (pos == -1 && this != host.openCurrentMenu && subMenu.Children != host.openCurrentMenu.barItems.Children
					&& !host.CloseMenu (false, true)) {
					return false;
				}
				host.Activate (host.selected, pos, subMenu);
			} else if (host.openSubMenu?.Count == 0 || host.openSubMenu?.Last ().barItems.IsSubMenuOf (barItems.Children [current]) == false) {
				return host.CloseMenu (false, true);
			} else {
				SetNeedsDisplay ();
			}
			return true;
		}

		int GetSubMenuIndex (MenuBarItem subMenu)
		{
			int pos = -1;
			if (this != null && Subviews.Count > 0) {
				Menu v = null;
				foreach (var menu in Subviews) {
					if (((Menu)menu).barItems == subMenu)
						v = (Menu)menu;
				}
				if (v != null)
					pos = Subviews.IndexOf (v);
			}

			return pos;
		}

		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}



	public class MenuBar : View {
		internal int selected;
		internal int selectedSub;

		public MenuBarItem [] Menus { get; set; }

		private bool useKeysUpDownAsKeysLeftRight = false;

		public bool UseKeysUpDownAsKeysLeftRight {
			get => useKeysUpDownAsKeysLeftRight;
			set {
				useKeysUpDownAsKeysLeftRight = value;
				if (value && UseSubMenusSingleFrame) {
					UseSubMenusSingleFrame = false;
					SetNeedsDisplay ();
				}
			}
		}

		static ustring shortcutDelimiter = "+";
		public static ustring ShortcutDelimiter {
			get => shortcutDelimiter;
			set {
				if (shortcutDelimiter != value) {
					shortcutDelimiter = value == ustring.Empty ? " " : value;
				}
			}
		}

		new public static Rune HotKeySpecifier => '_';

		private bool useSubMenusSingleFrame;

		public bool UseSubMenusSingleFrame {
			get => useSubMenusSingleFrame;
			set {
				useSubMenusSingleFrame = value;
				if (value && UseKeysUpDownAsKeysLeftRight) {
					useKeysUpDownAsKeysLeftRight = false;
					SetNeedsDisplay ();
				}
			}
		}

		public MenuBar () : this (new MenuBarItem [] { }) { }

		public MenuBar (MenuBarItem [] menus) : base ()
		{
			X = 0;
			Y = 0;
			Width = Dim.Fill ();
			Height = 1;
			Menus = menus;
			selected = -1;
			selectedSub = -1;
			ColorScheme = Colors.Menu;
			WantMousePositionReports = true;
			IsMenuOpen = false;

			AddCommand (Command.Left, () => { MoveLeft (); return true; });
			AddCommand (Command.Right, () => { MoveRight (); return true; });
			AddCommand (Command.Cancel, () => { CloseMenuBar (); return true; });
			AddCommand (Command.Accept, () => { ProcessMenu (selected, Menus [selected]); return true; });

			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.CursorRight, Command.Right);
			AddKeyBinding (Key.Esc, Command.Cancel);
			AddKeyBinding (Key.C | Key.CtrlMask, Command.Cancel);
			AddKeyBinding (Key.CursorDown, Command.Accept);
			AddKeyBinding (Key.Enter, Command.Accept);
		}

		bool openedByAltKey;

		bool isCleaning;

		public override bool OnLeave (View view)
		{
			if ((!(view is MenuBar) && !(view is Menu) || !(view is MenuBar) && !(view is Menu) && openMenu != null) && !isCleaning && !reopen) {
				CleanUp ();
				return true;
			}
			return false;
		}

		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			if (keyEvent.IsAlt || (keyEvent.IsCtrl && keyEvent.Key == (Key.CtrlMask | Key.Space))) {
				openedByAltKey = true;
				SetNeedsDisplay ();
				openedByHotKey = false;
			}
			return false;
		}

		public override bool OnKeyUp (KeyEvent keyEvent)
		{
			if (keyEvent.IsAlt || keyEvent.Key == Key.AltMask || (keyEvent.IsCtrl && keyEvent.Key == (Key.CtrlMask | Key.Space))) {
				if (openedByAltKey && !IsMenuOpen && openMenu == null && (((uint)keyEvent.Key & (uint)Key.CharMask) == 0
					|| ((uint)keyEvent.Key & (uint)Key.CharMask) == (uint)Key.Space)) {
					var mbar = GetMouseGrabViewInstance (this);
					if (mbar != null) {
						mbar.CleanUp ();
					}

					IsMenuOpen = true;
					selected = 0;
					CanFocus = true;
					lastFocused = SuperView == null ? Application.Current.MostFocused : SuperView.MostFocused;
					SetFocus ();
					SetNeedsDisplay ();
					Application.GrabMouse (this);
				} else if (!openedByHotKey) {
					CleanUp ();
				}

				return true;
			}
			return false;
		}

		internal void CleanUp ()
		{
			isCleaning = true;
			if (openMenu != null) {
				CloseAllMenus ();
			}
			openedByAltKey = false;
			IsMenuOpen = false;
			selected = -1;
			CanFocus = false;
			if (lastFocused != null) {
				lastFocused.SetFocus ();
			}
			SetNeedsDisplay ();
			Application.UngrabMouse ();
			isCleaning = false;
		}

		public override void Redraw (Rect bounds)
		{
			Move (0, 0);
			Driver.SetAttribute (GetNormalColor ());
			for (int i = 0; i < Frame.Width; i++)
				Driver.AddRune (' ');

			Move (1, 0);
			int pos = 1;

			for (int i = 0; i < Menus.Length; i++) {
				var menu = Menus [i];
				Move (pos, 0);
				Attribute hotColor, normalColor;
				if (i == selected && IsMenuOpen) {
					hotColor = i == selected ? ColorScheme.HotFocus : ColorScheme.HotNormal;
					normalColor = i == selected ? ColorScheme.Focus :
						GetNormalColor ();
				} else if (openedByAltKey) {
					hotColor = ColorScheme.HotNormal;
					normalColor = GetNormalColor ();
				} else {
					hotColor = GetNormalColor ();
					normalColor = GetNormalColor ();
				}
				DrawHotString (menu.Help.IsEmpty ? $" {menu.Title}  " : $" {menu.Title}  {menu.Help}  ", hotColor, normalColor);
				pos += 1 + menu.TitleLength + (menu.Help.ConsoleWidth > 0 ? menu.Help.ConsoleWidth + 2 : 0) + 2;
			}
			PositionCursor ();
		}

		public override void PositionCursor ()
		{
			if (selected == -1 && HasFocus && Menus.Length > 0) {
				selected = 0;
			}
			int pos = 0;
			for (int i = 0; i < Menus.Length; i++) {
				if (i == selected) {
					pos++;
					if (IsMenuOpen)
						Move (pos + 1, 0);
					else {
						Move (pos + 1, 0);
					}
					return;
				} else {
					pos += 1 + Menus [i].TitleLength + (Menus [i].Help.ConsoleWidth > 0 ? Menus [i].Help.ConsoleWidth + 2 : 0) + 2;
				}
			}
		}

		void Selected (MenuItem item)
		{
			var action = item.Action;

			if (action == null)
				return;

			Application.UngrabMouse ();
			CloseAllMenus ();
			Application.Refresh ();

			Application.MainLoop.AddIdle (() => {
				action ();
				return false;
			});
		}

		public event Action<MenuOpeningEventArgs> MenuOpening;

		public event Action<MenuItem> MenuOpened;

		public event Action<MenuClosingEventArgs> MenuClosing;

		public event Action MenuAllClosed;

		internal Menu openMenu;
		Menu ocm;
		internal Menu openCurrentMenu {
			get => ocm;
			set {
				if (ocm != value) {
					ocm = value;
					if (ocm.current > -1) {
						OnMenuOpened ();
					}
				}
			}
		}
		internal List<Menu> openSubMenu;
		View previousFocused;
		internal bool isMenuOpening;
		internal bool isMenuClosing;

		public bool IsMenuOpen { get; protected set; }

		public virtual MenuOpeningEventArgs OnMenuOpening (MenuBarItem currentMenu)
		{
			var ev = new MenuOpeningEventArgs (currentMenu);
			MenuOpening?.Invoke (ev);
			return ev;
		}

		public virtual void OnMenuOpened ()
		{
			MenuItem mi = null;
			if (openCurrentMenu.barItems.Children != null && openCurrentMenu?.current > -1) {
				mi = openCurrentMenu.barItems.Children [openCurrentMenu.current];
			} else if (openCurrentMenu.barItems.IsTopLevel) {
				mi = openCurrentMenu.barItems;
			} else {
				mi = openMenu.barItems.Children [openMenu.current];
			}
			MenuOpened?.Invoke (mi);
		}

		public virtual MenuClosingEventArgs OnMenuClosing (MenuBarItem currentMenu, bool reopen, bool isSubMenu)
		{
			var ev = new MenuClosingEventArgs (currentMenu, reopen, isSubMenu);
			MenuClosing?.Invoke (ev);
			return ev;
		}

		public virtual void OnMenuAllClosed ()
		{
			MenuAllClosed?.Invoke ();
		}

		View lastFocused;

		public View LastFocused { get; private set; }

		internal void OpenMenu (int index, int sIndex = -1, MenuBarItem subMenu = null)
		{
			isMenuOpening = true;
			var newMenu = OnMenuOpening (Menus [index]);
			if (newMenu.Cancel) {
				isMenuOpening = false;
				return;
			}
			if (newMenu.NewMenuBarItem != null) {
				Menus [index] = newMenu.NewMenuBarItem;
			}
			int pos = 0;
			switch (subMenu) {
			case null:
				lastFocused = lastFocused ?? (SuperView == null ? Application.Current.MostFocused : SuperView.MostFocused);
				if (openSubMenu != null && !CloseMenu (false, true))
					return;
				if (openMenu != null) {
					if (SuperView == null) {
						Application.Current.Remove (openMenu);
					} else {
						SuperView.Remove (openMenu);
					}
					openMenu.Dispose ();
				}

				for (int i = 0; i < index; i++)
					pos += 1 + Menus [i].TitleLength + (Menus [i].Help.ConsoleWidth > 0 ? Menus [i].Help.ConsoleWidth + 2 : 0) + 2;
				openMenu = new Menu (this, Frame.X + pos, Frame.Y + 1, Menus [index]);
				openCurrentMenu = openMenu;
				openCurrentMenu.previousSubFocused = openMenu;

				if (SuperView == null) {
					Application.Current.Add (openMenu);
				} else {
					SuperView.Add (openMenu);
				}
				openMenu.SetFocus ();
				break;
			default:
				if (openSubMenu == null)
					openSubMenu = new List<Menu> ();
				if (sIndex > -1) {
					RemoveSubMenu (sIndex);
				} else {
					var last = openSubMenu.Count > 0 ? openSubMenu.Last () : openMenu;
					if (!UseSubMenusSingleFrame) {
						openCurrentMenu = new Menu (this, last.Frame.Left + last.Frame.Width, last.Frame.Top + 1 + last.current, subMenu);
					} else {
						var first = openSubMenu.Count > 0 ? openSubMenu.First () : openMenu;
						var mbi = new MenuItem [2 + subMenu.Children.Length];
						mbi [0] = new MenuItem () { Title = subMenu.Title, Parent = subMenu };
						mbi [1] = null;
						for (int j = 0; j < subMenu.Children.Length; j++) {
							mbi [j + 2] = subMenu.Children [j];
						}
						var newSubMenu = new MenuBarItem (mbi);
						openCurrentMenu = new Menu (this, first.Frame.Left, first.Frame.Top, newSubMenu);
						last.Visible = false;
						Application.GrabMouse (openCurrentMenu);
					}
					openCurrentMenu.previousSubFocused = last.previousSubFocused;
					openSubMenu.Add (openCurrentMenu);
					if (SuperView == null) {
						Application.Current.Add (openCurrentMenu);
					} else {
						SuperView.Add (openCurrentMenu);
					}
				}
				selectedSub = openSubMenu.Count - 1;
				if (selectedSub > -1 && SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current)) {
					openCurrentMenu.SetFocus ();
				}
				break;
			}
			isMenuOpening = false;
			IsMenuOpen = true;
		}

		public void OpenMenu ()
		{
			var mbar = GetMouseGrabViewInstance (this);
			if (mbar != null) {
				mbar.CleanUp ();
			}

			if (openMenu != null)
				return;
			selected = 0;
			SetNeedsDisplay ();

			previousFocused = SuperView == null ? Application.Current.Focused : SuperView.Focused;
			OpenMenu (selected);
			if (!SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current) && !CloseMenu (false)) {
				return;
			}
			if (!openCurrentMenu.CheckSubMenu ())
				return;
			Application.GrabMouse (this);
		}

		internal void Activate (int idx, int sIdx = -1, MenuBarItem subMenu = null)
		{
			selected = idx;
			selectedSub = sIdx;
			if (openMenu == null)
				previousFocused = SuperView == null ? Application.Current.Focused : SuperView.Focused;

			OpenMenu (idx, sIdx, subMenu);
			if (!SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current)
				&& subMenu == null && !CloseMenu (false)) {

				return;
			}
			SetNeedsDisplay ();
		}

		internal bool SelectEnabledItem (IEnumerable<MenuItem> chldren, int current, out int newCurrent, bool forward = true)
		{
			if (chldren == null) {
				newCurrent = -1;
				return true;
			}

			IEnumerable<MenuItem> childrens;
			if (forward) {
				childrens = chldren;
			} else {
				childrens = chldren.Reverse ();
			}
			int count;
			if (forward) {
				count = -1;
			} else {
				count = childrens.Count ();
			}
			foreach (var child in childrens) {
				if (forward) {
					if (++count < current) {
						continue;
					}
				} else {
					if (--count > current) {
						continue;
					}
				}
				if (child == null || !child.IsEnabled ()) {
					if (forward) {
						current++;
					} else {
						current--;
					}
				} else {
					newCurrent = current;
					return true;
				}
			}
			newCurrent = -1;
			return false;
		}

		public bool CloseMenu (bool ignoreUseSubMenusSingleFrame = false)
		{
			return CloseMenu (false, false, ignoreUseSubMenusSingleFrame);
		}

		bool reopen;

		internal bool CloseMenu (bool reopen = false, bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
		{
			var mbi = isSubMenu ? openCurrentMenu.barItems : openMenu?.barItems;
			if (UseSubMenusSingleFrame && mbi != null &&
				!ignoreUseSubMenusSingleFrame && mbi.Parent != null) {
				return false;
			}
			isMenuClosing = true;
			this.reopen = reopen;
			var args = OnMenuClosing (mbi, reopen, isSubMenu);
			if (args.Cancel) {
				isMenuClosing = false;
				if (args.CurrentMenu.Parent != null)
					openMenu.current = ((MenuBarItem)args.CurrentMenu.Parent).Children.IndexOf (args.CurrentMenu);
				return false;
			}
			switch (isSubMenu) {
			case false:
				if (openMenu != null) {
					if (SuperView == null) {
						Application.Current.Remove (openMenu);
					} else {
						SuperView?.Remove (openMenu);
					}
				}
				SetNeedsDisplay ();
				if (previousFocused != null && previousFocused is Menu && openMenu != null && previousFocused.ToString () != openCurrentMenu.ToString ())
					previousFocused.SetFocus ();
				openMenu?.Dispose ();
				openMenu = null;
				if (lastFocused is Menu || lastFocused is MenuBar) {
					lastFocused = null;
				}
				LastFocused = lastFocused;
				lastFocused = null;
				if (LastFocused != null && LastFocused.CanFocus) {
					if (!reopen) {
						selected = -1;
					}
					LastFocused.SetFocus ();
				} else {
					SetFocus ();
					PositionCursor ();
				}
				IsMenuOpen = false;
				break;

			case true:
				selectedSub = -1;
				SetNeedsDisplay ();
				RemoveAllOpensSubMenus ();
				openCurrentMenu.previousSubFocused.SetFocus ();
				openSubMenu = null;
				IsMenuOpen = true;
				break;
			}
			this.reopen = false;
			isMenuClosing = false;
			return true;
		}

		void RemoveSubMenu (int index, bool ignoreUseSubMenusSingleFrame = false)
		{
			if (openSubMenu == null || (UseSubMenusSingleFrame
				&& !ignoreUseSubMenusSingleFrame && openSubMenu.Count == 0))

				return;
			for (int i = openSubMenu.Count - 1; i > index; i--) {
				isMenuClosing = true;
				Menu menu;
				if (openSubMenu.Count - 1 > 0)
					menu = openSubMenu [i - 1];
				else
					menu = openMenu;
				if (!menu.Visible)
					menu.Visible = true;
				openCurrentMenu = menu;
				openCurrentMenu.SetFocus ();
				if (openSubMenu != null) {
					menu = openSubMenu [i];
					if (SuperView == null) {
						Application.Current.Remove (menu);
					} else {
						SuperView.Remove (menu);
					}
					openSubMenu.Remove (menu);
					menu.Dispose ();
				}
				RemoveSubMenu (i, ignoreUseSubMenusSingleFrame);
			}
			if (openSubMenu.Count > 0)
				openCurrentMenu = openSubMenu.Last ();

			isMenuClosing = false;
		}

		internal void RemoveAllOpensSubMenus ()
		{
			if (openSubMenu != null) {
				foreach (var item in openSubMenu) {
					if (SuperView == null) {
						Application.Current.Remove (item);
					} else {
						SuperView.Remove (item);
					}
					item.Dispose ();
				}
			}
		}

		internal void CloseAllMenus ()
		{
			if (!isMenuOpening && !isMenuClosing) {
				if (openSubMenu != null && !CloseMenu (false, true))
					return;
				if (!CloseMenu (false))
					return;
				if (LastFocused != null && LastFocused != this)
					selected = -1;
				Application.UngrabMouse ();
			}
			IsMenuOpen = false;
			openedByHotKey = false;
			openedByAltKey = false;
			OnMenuAllClosed ();
		}

		View FindDeepestMenu (View view, ref int count)
		{
			count = count > 0 ? count : 0;
			foreach (var menu in view.Subviews) {
				if (menu is Menu) {
					count++;
					return FindDeepestMenu ((Menu)menu, ref count);
				}
			}
			return view;
		}

		internal void PreviousMenu (bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
		{
			switch (isSubMenu) {
			case false:
				if (selected <= 0)
					selected = Menus.Length - 1;
				else
					selected--;

				if (selected > -1 && !CloseMenu (true, false, ignoreUseSubMenusSingleFrame))
					return;
				OpenMenu (selected);
				if (!SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current, false)) {
					openCurrentMenu.current = 0;
					if (!SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current)) {
						CloseMenu (ignoreUseSubMenusSingleFrame);
					}
				}
				break;
			case true:
				if (selectedSub > -1) {
					selectedSub--;
					RemoveSubMenu (selectedSub, ignoreUseSubMenusSingleFrame);
					SetNeedsDisplay ();
				} else
					PreviousMenu ();

				break;
			}
		}

		internal void NextMenu (bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
		{
			switch (isSubMenu) {
			case false:
				if (selected == -1)
					selected = 0;
				else if (selected + 1 == Menus.Length)
					selected = 0;
				else
					selected++;

				if (selected > -1 && !CloseMenu (true, ignoreUseSubMenusSingleFrame))
					return;
				OpenMenu (selected);
				SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current);
				break;
			case true:
				if (UseKeysUpDownAsKeysLeftRight) {
					if (CloseMenu (false, true, ignoreUseSubMenusSingleFrame)) {
						NextMenu (false, ignoreUseSubMenusSingleFrame);
					}
				} else {
					var subMenu = openCurrentMenu.current > -1
						? openCurrentMenu.barItems.SubMenu (openCurrentMenu.barItems.Children [openCurrentMenu.current])
						: null;
					if ((selectedSub == -1 || openSubMenu == null || openSubMenu?.Count == selectedSub) && subMenu == null) {
						if (openSubMenu != null && !CloseMenu (false, true))
							return;
						NextMenu (false, ignoreUseSubMenusSingleFrame);
					} else if (subMenu != null || (openCurrentMenu.current > -1
						&& !openCurrentMenu.barItems.Children [openCurrentMenu.current].IsFromSubMenu)) {
						selectedSub++;
						openCurrentMenu.CheckSubMenu ();
					} else {
						if (CloseMenu (false, true, ignoreUseSubMenusSingleFrame)) {
							NextMenu (false, ignoreUseSubMenusSingleFrame);
						}
						return;
					}

					SetNeedsDisplay ();
					if (UseKeysUpDownAsKeysLeftRight)
						openCurrentMenu.CheckSubMenu ();
				}
				break;
			}
		}

		bool openedByHotKey;
		internal bool FindAndOpenMenuByHotkey (KeyEvent kb)
		{
			var c = ((uint)kb.Key & (uint)Key.CharMask);
			for (int i = 0; i < Menus.Length; i++) {
				var mi = Menus [i];
				int p = mi.Title.IndexOf (MenuBar.HotKeySpecifier);
				if (p != -1 && p + 1 < mi.Title.RuneCount) {
					if (Char.ToUpperInvariant ((char)mi.Title [p + 1]) == c) {
						ProcessMenu (i, mi);
						return true;
					}
				}
			}
			return false;
		}

		internal bool FindAndOpenMenuByShortcut (KeyEvent kb, MenuItem [] children = null)
		{
			if (children == null) {
				children = Menus;
			}

			var key = kb.KeyValue;
			var keys = ShortcutHelper.GetModifiersKey (kb);
			key |= (int)keys;
			for (int i = 0; i < children.Length; i++) {
				var mi = children [i];
				if (mi == null) {
					continue;
				}
				if ((!(mi is MenuBarItem mbiTopLevel) || mbiTopLevel.IsTopLevel) && mi.Shortcut != Key.Null && mi.Shortcut == (Key)key) {
					var action = mi.Action;
					if (action != null) {
						Application.MainLoop.AddIdle (() => {
							action ();
							return false;
						});
					}
					return true;
				}
				if (mi is MenuBarItem menuBarItem && !menuBarItem.IsTopLevel && FindAndOpenMenuByShortcut (kb, menuBarItem.Children)) {
					return true;
				}
			}

			return false;
		}

		private void ProcessMenu (int i, MenuBarItem mi)
		{
			if (selected < 0 && IsMenuOpen) {
				return;
			}

			if (mi.IsTopLevel) {
				var menu = new Menu (this, i, 0, mi);
				menu.Run (mi.Action);
				menu.Dispose ();
			} else {
				openedByHotKey = true;
				Application.GrabMouse (this);
				selected = i;
				OpenMenu (i);
				if (!SelectEnabledItem (openCurrentMenu.barItems.Children, openCurrentMenu.current, out openCurrentMenu.current) && !CloseMenu (false)) {
					return;
				}
				if (!openCurrentMenu.CheckSubMenu ())
					return;
			}
			SetNeedsDisplay ();
		}

		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (kb.Key == Key.F9) {
				if (!IsMenuOpen)
					OpenMenu ();
				else
					CloseAllMenus ();
				return true;
			}

			if (kb.IsAlt && kb.Key == Key.AltMask && openMenu == null) {
				OnKeyDown (kb);
				OnKeyUp (kb);
				return true;
			} else if (kb.IsAlt && !kb.IsCtrl && !kb.IsShift) {
				if (FindAndOpenMenuByHotkey (kb)) return true;
			}
			return base.ProcessHotKey (kb);
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			if (InvokeKeybindings (kb) == true)
				return true;

			var key = kb.KeyValue;
			if ((key >= 'a' && key <= 'z') || (key >= 'A' && key <= 'Z') || (key >= '0' && key <= '9')) {
				char c = Char.ToUpper ((char)key);

				if (selected == -1 || Menus [selected].IsTopLevel)
					return false;

				foreach (var mi in Menus [selected].Children) {
					if (mi == null)
						continue;
					int p = mi.Title.IndexOf (MenuBar.HotKeySpecifier);
					if (p != -1 && p + 1 < mi.Title.RuneCount) {
						if (mi.Title [p + 1] == c) {
							Selected (mi);
							return true;
						}
					}
				}
			}

			return false;
		}

		void CloseMenuBar ()
		{
			if (!CloseMenu (false))
				return;
			if (openedByAltKey) {
				openedByAltKey = false;
				LastFocused?.SetFocus ();
			}
			SetNeedsDisplay ();
		}

		void MoveRight ()
		{
			selected = (selected + 1) % Menus.Length;
			OpenMenu (selected);
			SetNeedsDisplay ();
		}

		void MoveLeft ()
		{
			selected--;
			if (selected < 0)
				selected = Menus.Length - 1;
			OpenMenu (selected);
			SetNeedsDisplay ();
		}

		public override bool ProcessColdKey (KeyEvent kb)
		{
			return FindAndOpenMenuByShortcut (kb);
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (!handled && !HandleGrabView (me, this)) {
				return false;
			}
			handled = false;

			if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked || me.Flags == MouseFlags.Button1TripleClicked || me.Flags == MouseFlags.Button1Clicked ||
				(me.Flags == MouseFlags.ReportMousePosition && selected > -1) ||
				(me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && selected > -1)) {
				int pos = 1;
				int cx = me.X;
				for (int i = 0; i < Menus.Length; i++) {
					if (cx >= pos && cx < pos + 1 + Menus [i].TitleLength + Menus [i].Help.ConsoleWidth + 2) {
						if (me.Flags == MouseFlags.Button1Clicked) {
							if (Menus [i].IsTopLevel) {
								var menu = new Menu (this, i, 0, Menus [i]);
								menu.Run (Menus [i].Action);
								menu.Dispose ();
							} else if (!IsMenuOpen) {
								Activate (i);
							}
						} else if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked || me.Flags == MouseFlags.Button1TripleClicked) {
							if (IsMenuOpen && !Menus [i].IsTopLevel) {
								CloseAllMenus ();
							} else if (!Menus [i].IsTopLevel) {
								Activate (i);
							}
						} else if (selected != i && selected > -1 && (me.Flags == MouseFlags.ReportMousePosition ||
							me.Flags == MouseFlags.Button1Pressed && me.Flags == MouseFlags.ReportMousePosition)) {
							if (IsMenuOpen) {
								if (!CloseMenu (true, false)) {
									return true;
								}
								Activate (i);
							}
						} else {
							if (IsMenuOpen)
								Activate (i);
						}
						return true;
					}
					pos += 1 + Menus [i].TitleLength + 2;
				}
			}
			return false;
		}

		internal bool handled;
		internal bool isContextMenuLoading;

		internal bool HandleGrabView (MouseEvent me, View current)
		{
			if (Application.mouseGrabView != null) {
				if (me.View is MenuBar || me.View is Menu) {
					var mbar = GetMouseGrabViewInstance (me.View);
					if (mbar != null) {
						if (me.Flags == MouseFlags.Button1Clicked) {
							mbar.CleanUp ();
							Application.GrabMouse (me.View);
						} else {
							handled = false;
							return false;
						}
					}
					if (me.View != current) {
						Application.UngrabMouse ();
						var v = me.View;
						Application.GrabMouse (v);
						MouseEvent nme;
						if (me.Y > -1) {
							var newxy = v.ScreenToView (me.X, me.Y);
							nme = new MouseEvent () {
								X = newxy.X,
								Y = newxy.Y,
								Flags = me.Flags,
								OfX = me.X - newxy.X,
								OfY = me.Y - newxy.Y,
								View = v
							};
						} else {
							nme = new MouseEvent () {
								X = me.X + current.Frame.X,
								Y = 0,
								Flags = me.Flags,
								View = v
							};
						}

						v.MouseEvent (nme);
						return false;
					}
				} else if (!isContextMenuLoading && !(me.View is MenuBar || me.View is Menu)
					&& me.Flags != MouseFlags.ReportMousePosition && me.Flags != 0) {

					Application.UngrabMouse ();
					if (IsMenuOpen)
						CloseAllMenus ();
					handled = false;
					return false;
				} else {
					handled = false;
					isContextMenuLoading = false;
					return false;
				}
			} else if (!IsMenuOpen && (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked
				|| me.Flags == MouseFlags.Button1TripleClicked || me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))) {

				Application.GrabMouse (current);
			} else if (IsMenuOpen && (me.View is MenuBar || me.View is Menu)) {
				Application.GrabMouse (me.View);
			} else {
				handled = false;
				return false;
			}

			handled = true;

			return true;
		}

		MenuBar GetMouseGrabViewInstance (View view)
		{
			if (view == null || Application.mouseGrabView == null) {
				return null;
			}

			MenuBar hostView = null;
			if (view is MenuBar) {
				hostView = (MenuBar)view;
			} else if (view is Menu) {
				hostView = ((Menu)view).host;
			}

			var grabView = Application.mouseGrabView;
			MenuBar hostGrabView = null;
			if (grabView is MenuBar) {
				hostGrabView = (MenuBar)grabView;
			} else if (grabView is Menu) {
				hostGrabView = ((Menu)grabView).host;
			}

			return hostView != hostGrabView ? hostGrabView : null;
		}

		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}

	public class MenuOpeningEventArgs : EventArgs {
		public MenuBarItem CurrentMenu { get; }

		public MenuBarItem NewMenuBarItem { get; set; }
		public bool Cancel { get; set; }

		public MenuOpeningEventArgs (MenuBarItem currentMenu)
		{
			CurrentMenu = currentMenu;
		}
	}

	public class MenuClosingEventArgs : EventArgs {
		public MenuBarItem CurrentMenu { get; }

		public bool Reopen { get; }

		public bool IsSubMenu { get; }

		public bool Cancel { get; set; }

		public MenuClosingEventArgs (MenuBarItem currentMenu, bool reopen, bool isSubMenu)
		{
			CurrentMenu = currentMenu;
			Reopen = reopen;
			IsSubMenu = isSubMenu;
		}
	}
}

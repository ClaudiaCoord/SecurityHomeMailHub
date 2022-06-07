using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Terminal.Gui {



	public class TableView : View {

		public class CellActivatedEventArgs : EventArgs {
			public DataTable Table { get; }


			public int Col { get; }

			public int Row { get; }

			public CellActivatedEventArgs (DataTable t, int col, int row)
			{
				Table = t;
				Col = col;
				Row = row;
			}
		}

		private int columnOffset;
		private int rowOffset;
		private int selectedRow;
		private int selectedColumn;
		private DataTable table;
		private TableStyle style = new TableStyle ();
		private Key cellActivationKey = Key.Enter;

		Point? scrollLeftPoint;
		Point? scrollRightPoint;

		public const int DefaultMaxCellWidth = 100;

		public DataTable Table { get => table; set { table = value; Update (); } }

		public TableStyle Style { get => style; set { style = value; Update (); } }

		public bool FullRowSelect { get; set; }

		public bool MultiSelect { get; set; } = true;

		public Stack<TableSelection> MultiSelectedRegions { get; } = new Stack<TableSelection> ();

		public int ColumnOffset {
			get => columnOffset;

			set => columnOffset = Table == null ? 0 : Math.Max (0, Math.Min (Table.Columns.Count - 1, value));
		}

		public int RowOffset {
			get => rowOffset;
			set => rowOffset = Table == null ? 0 : Math.Max (0, Math.Min (Table.Rows.Count - 1, value));
		}

		public int SelectedColumn {
			get => selectedColumn;

			set {
				var oldValue = selectedColumn;

				selectedColumn = Table == null ? 0 : Math.Min (Table.Columns.Count - 1, Math.Max (0, value));

				if (oldValue != selectedColumn)
					OnSelectedCellChanged (new SelectedCellChangedEventArgs (Table, oldValue, SelectedColumn, SelectedRow, SelectedRow));
			}
		}

		public int SelectedRow {
			get => selectedRow;
			set {

				var oldValue = selectedRow;

				selectedRow = Table == null ? 0 : Math.Min (Table.Rows.Count - 1, Math.Max (0, value));

				if (oldValue != selectedRow)
					OnSelectedCellChanged (new SelectedCellChangedEventArgs (Table, SelectedColumn, SelectedColumn, oldValue, selectedRow));
			}
		}

		public int MaxCellWidth { get; set; } = DefaultMaxCellWidth;

		public string NullSymbol { get; set; } = "-";

		public char SeparatorSymbol { get; set; } = ' ';

		public event Action<SelectedCellChangedEventArgs> SelectedCellChanged;

		public event Action<CellActivatedEventArgs> CellActivated;

		public Key CellActivationKey {
			get => cellActivationKey;
			set {
				if (cellActivationKey != value) {
					ReplaceKeyBinding (cellActivationKey, value);
					
					AddKeyBinding (value, Command.Accept);
					cellActivationKey = value;
				}
			}
		}

		public TableView (DataTable table) : this ()
		{
			this.Table = table;
		}

		public TableView () : base ()
		{
			CanFocus = true;

			AddCommand (Command.Right, () => { ChangeSelectionByOffset (1, 0, false); return true; });
			AddCommand (Command.Left, () => { ChangeSelectionByOffset (-1, 0, false); return true; });
			AddCommand (Command.LineUp, () => { ChangeSelectionByOffset (0, -1, false); return true; });
			AddCommand (Command.LineDown, () => { ChangeSelectionByOffset (0, 1, false); return true; });
			AddCommand (Command.PageUp, () => { PageUp (false); return true; });
			AddCommand (Command.PageDown, () => { PageDown (false); return true; });
			AddCommand (Command.LeftHome, () => { ChangeSelectionToStartOfRow (false);  return true; });
			AddCommand (Command.RightEnd, () => { ChangeSelectionToEndOfRow (false); return true; });
			AddCommand (Command.TopHome, () => { ChangeSelectionToStartOfTable(false); return true; });
			AddCommand (Command.BottomEnd, () => { ChangeSelectionToEndOfTable (false); return true; });

			AddCommand (Command.RightExtend, () => { ChangeSelectionByOffset (1, 0, true); return true; });
			AddCommand (Command.LeftExtend, () => { ChangeSelectionByOffset (-1, 0, true); return true; });
			AddCommand (Command.LineUpExtend, () => { ChangeSelectionByOffset (0, -1, true); return true; });
			AddCommand (Command.LineDownExtend, () => { ChangeSelectionByOffset (0, 1, true); return true; });
			AddCommand (Command.PageUpExtend, () => { PageUp (true); return true; });
			AddCommand (Command.PageDownExtend, () => { PageDown (true); return true; });
			AddCommand (Command.LeftHomeExtend, () => { ChangeSelectionToStartOfRow (true); return true; });
			AddCommand (Command.RightEndExtend, () => { ChangeSelectionToEndOfRow (true); return true; });
			AddCommand (Command.TopHomeExtend, () => { ChangeSelectionToStartOfTable (true); return true; });
			AddCommand (Command.BottomEndExtend, () => { ChangeSelectionToEndOfTable (true); return true; });

			AddCommand (Command.SelectAll, () => { SelectAll(); return true; });
			AddCommand (Command.Accept, () => { OnCellActivated(new CellActivatedEventArgs (Table, SelectedColumn, SelectedRow)); return true; });

			AddKeyBinding (Key.CursorLeft, Command.Left);
			AddKeyBinding (Key.CursorRight, Command.Right);
			AddKeyBinding (Key.CursorUp, Command.LineUp);
			AddKeyBinding (Key.CursorDown, Command.LineDown);
			AddKeyBinding (Key.PageUp, Command.PageUp);
			AddKeyBinding (Key.PageDown, Command.PageDown);
			AddKeyBinding (Key.Home, Command.LeftHome);
			AddKeyBinding (Key.End, Command.RightEnd);
			AddKeyBinding (Key.Home | Key.CtrlMask, Command.TopHome);
			AddKeyBinding (Key.End | Key.CtrlMask, Command.BottomEnd);

			AddKeyBinding (Key.CursorLeft | Key.ShiftMask, Command.LeftExtend);
			AddKeyBinding (Key.CursorRight | Key.ShiftMask, Command.RightExtend);
			AddKeyBinding (Key.CursorUp | Key.ShiftMask, Command.LineUpExtend);
			AddKeyBinding (Key.CursorDown| Key.ShiftMask, Command.LineDownExtend);
			AddKeyBinding (Key.PageUp | Key.ShiftMask, Command.PageUpExtend);
			AddKeyBinding (Key.PageDown | Key.ShiftMask, Command.PageDownExtend);
			AddKeyBinding (Key.Home | Key.ShiftMask, Command.LeftHomeExtend);
			AddKeyBinding (Key.End | Key.ShiftMask, Command.RightEndExtend);
			AddKeyBinding (Key.Home | Key.CtrlMask | Key.ShiftMask, Command.TopHomeExtend);
			AddKeyBinding (Key.End | Key.CtrlMask | Key.ShiftMask, Command.BottomEndExtend);

			AddKeyBinding (Key.A | Key.CtrlMask, Command.SelectAll);
			AddKeyBinding (CellActivationKey, Command.Accept);
		}

		public override void Redraw (Rect bounds)
			{
				Move (0, 0);
				var frame = Frame;

				scrollRightPoint = null;
				scrollLeftPoint = null;

				var columnsToRender = CalculateViewport (bounds).ToArray ();

				Driver.SetAttribute (GetNormalColor ());

				Driver.AddStr (new string (' ', bounds.Width));

				int line = 0;

				if (ShouldRenderHeaders ()) {
			if (Style.ShowHorizontalHeaderOverline) {
					RenderHeaderOverline (line, bounds.Width, columnsToRender);
					line++;
				}

				RenderHeaderMidline (line, columnsToRender);
				line++;

				if (Style.ShowHorizontalHeaderUnderline) {
					RenderHeaderUnderline (line, bounds.Width, columnsToRender);
					line++;
				}
			}

			int headerLinesConsumed = line;

			for (; line < frame.Height; line++) {

				ClearLine (line, bounds.Width);

				var rowToRender = RowOffset + (line - headerLinesConsumed);

				if (Table == null || rowToRender >= Table.Rows.Count || rowToRender < 0)
					continue;

				RenderRow (line, rowToRender, columnsToRender);
			}
		}

		private void ClearLine (int row, int width)
		{
			Move (0, row);
			Driver.SetAttribute (GetNormalColor ());
			Driver.AddStr (new string (' ', width));
		}

		private int GetHeaderHeightIfAny ()
		{
			return ShouldRenderHeaders () ? GetHeaderHeight () : 0;
		}

		private int GetHeaderHeight ()
		{
			int heightRequired = 1;

			if (Style.ShowHorizontalHeaderOverline)
				heightRequired++;

			if (Style.ShowHorizontalHeaderUnderline)
				heightRequired++;

			return heightRequired;
		}

		private void RenderHeaderOverline (int row, int availableWidth, ColumnToRender [] columnsToRender)
		{
			for (int c = 0; c < availableWidth; c++) {

				var rune = Driver.HLine;

				if (Style.ShowVerticalHeaderLines) {

					if (c == 0) {
						rune = Driver.ULCorner;
					}
					else if (columnsToRender.Any (r => r.X == c + 1)) {
						rune = Driver.TopTee;
					} else if (c == availableWidth - 1) {
						rune = Driver.URCorner;
					}
					  else if (Style.ExpandLastColumn == false &&
						   columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c)) {
						rune = Driver.TopTee;
					}
				}

				AddRuneAt (Driver, c, row, rune);
			}
		}

		private void RenderHeaderMidline (int row, ColumnToRender [] columnsToRender)
		{
			ClearLine (row, Bounds.Width);

			if (style.ShowVerticalHeaderLines)
				AddRune (0, row, Driver.VLine);

			for (int i = 0; i < columnsToRender.Length; i++) {

				var current = columnsToRender [i];

				var colStyle = Style.GetColumnStyleIfAny (current.Column);
				var colName = current.Column.ColumnName;

				RenderSeparator (current.X - 1, row, true);

				Move (current.X, row);

				Driver.AddStr (TruncateOrPad (colName, colName, current.Width, colStyle));

				if (Style.ExpandLastColumn == false && current.IsVeryLast) {
					RenderSeparator (current.X + current.Width - 1, row, true);
				}
			}

			if (style.ShowVerticalHeaderLines)
				AddRune (Bounds.Width - 1, row, Driver.VLine);
		}

		private void RenderHeaderUnderline (int row, int availableWidth, ColumnToRender [] columnsToRender)
		{
			for (int c = 0; c < availableWidth; c++) {

				var rune = Driver.HLine;

				if (Style.ShowVerticalHeaderLines) {
					if (c == 0) {
						rune = Style.ShowVerticalCellLines ? Driver.LeftTee : Driver.LLCorner;

						if(Style.ShowHorizontalScrollIndicators && ColumnOffset > 0)
						{
							rune = Driver.LeftArrow;
							scrollLeftPoint = new Point(c,row);
						}
							
					}
					else if (columnsToRender.Any (r => r.X == c + 1)) {

						rune = Style.ShowVerticalCellLines ? '┼' : Driver.BottomTee;
					} else if (c == availableWidth - 1) {

						rune = Style.ShowVerticalCellLines ? Driver.RightTee : Driver.LRCorner;

						if(Style.ShowHorizontalScrollIndicators &&
							ColumnOffset + columnsToRender.Length < Table.Columns.Count)
						{
							rune = Driver.RightArrow;
							scrollRightPoint = new Point(c,row);
						}

					}
					  else if (Style.ExpandLastColumn == false &&
							  columnsToRender.Any (r => r.IsVeryLast && r.X + r.Width - 1 == c)) {
						rune = Style.ShowVerticalCellLines ? '┼' : Driver.BottomTee;
					}
				}

				AddRuneAt (Driver, c, row, rune);
			}

		}
		private void RenderRow (int row, int rowToRender, ColumnToRender [] columnsToRender)
		{
			var rowScheme = (Style.RowColorGetter?.Invoke (
				new RowColorGetterArgs(Table,rowToRender))) ?? ColorScheme;

			if (style.ShowVerticalCellLines)
				AddRune (0, row, Driver.VLine);

			Move (0, row);
			Driver.SetAttribute (FullRowSelect && IsSelected (0, rowToRender) ? rowScheme.HotFocus
				: Enabled ? rowScheme.Normal : rowScheme.Disabled);
			Driver.AddStr (new string (' ', Bounds.Width));

			for (int i = 0; i < columnsToRender.Length; i++) {

				var current = columnsToRender [i];

				var colStyle = Style.GetColumnStyleIfAny (current.Column);

				Move (current.X, row);

				bool isSelectedCell = IsSelected (current.Column.Ordinal, rowToRender);

				var val = Table.Rows [rowToRender] [current.Column];

				var representation = GetRepresentation (val, colStyle);

				var colorSchemeGetter = colStyle?.ColorGetter;

				ColorScheme scheme;
				if(colorSchemeGetter != null) {
					scheme = colorSchemeGetter(
						new CellColorGetterArgs (Table, rowToRender, current.Column.Ordinal, val, representation,rowScheme));

					if(scheme == null) {
						scheme = rowScheme;
					}
				}
				else {
					scheme = rowScheme;
				}

				var cellColor = isSelectedCell ? scheme.HotFocus : Enabled ? scheme.Normal : scheme.Disabled;

				var render = TruncateOrPad (val, representation, current.Width, colStyle);

				bool isPrimaryCell = current.Column.Ordinal == selectedColumn && rowToRender == selectedRow;
				
				RenderCell (cellColor,render,isPrimaryCell);
								
				if (scheme != rowScheme) {
					Driver.SetAttribute (isSelectedCell ? rowScheme.HotFocus
						: Enabled ? rowScheme.Normal : rowScheme.Disabled);
				}

				if (!FullRowSelect)
					Driver.SetAttribute (Enabled ? rowScheme.Normal : rowScheme.Disabled);

				RenderSeparator (current.X - 1, row, false);

				if (Style.ExpandLastColumn == false && current.IsVeryLast) {
					RenderSeparator (current.X + current.Width - 1, row, false);
				}
			}

			if (style.ShowVerticalCellLines)
				AddRune (Bounds.Width - 1, row, Driver.VLine);
		}

		protected virtual void RenderCell (Attribute cellColor, string render,bool isPrimaryCell)
		{
			if (Style.InvertSelectedCellFirstCharacter && isPrimaryCell) {

				if (render.Length > 0) {
					Driver.SetAttribute (Driver.MakeAttribute (cellColor.Background, cellColor.Foreground));
					Driver.AddRune (render [0]);

					if (render.Length > 1) {
						Driver.SetAttribute (cellColor);
						Driver.AddStr (render.Substring (1));
					}
				}
			} else {
				Driver.SetAttribute (cellColor);
				Driver.AddStr (render);
			}
		}

		private void RenderSeparator (int col, int row, bool isHeader)
		{
			if (col < 0)
				return;

			var renderLines = isHeader ? style.ShowVerticalHeaderLines : style.ShowVerticalCellLines;

			Rune symbol = renderLines ? Driver.VLine : SeparatorSymbol;
			AddRune (col, row, symbol);
		}

		void AddRuneAt (ConsoleDriver d, int col, int row, Rune ch)
		{
			Move (col, row);
			d.AddRune (ch);
		}

		private string TruncateOrPad (object originalCellValue, string representation, int availableHorizontalSpace, ColumnStyle colStyle)
		{
			if (string.IsNullOrEmpty (representation))
				return representation;

			if (representation.Sum (c => Rune.ColumnWidth (c)) < availableHorizontalSpace) {

				int toPad = availableHorizontalSpace - (representation.Sum (c => Rune.ColumnWidth (c)) + 1      );

				switch (colStyle?.GetAlignment (originalCellValue) ?? TextAlignment.Left) {

				case TextAlignment.Left:
					return representation + new string (' ', toPad);
				case TextAlignment.Right:
					return new string (' ', toPad) + representation;

				case TextAlignment.Centered:
				case TextAlignment.Justified:
					return
						new string (' ', (int)Math.Floor (toPad / 2.0)) +   
						representation +
						 new string (' ', (int)Math.Ceiling (toPad / 2.0));   
				}
			}

			return new string (representation.TakeWhile (c => (availableHorizontalSpace -= Rune.ColumnWidth (c)) > 0).ToArray ());
		}

		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (Table == null || Table.Columns.Count <= 0) {
				PositionCursor ();
				return false;
			}

			var result = InvokeKeybindings (keyEvent);
			if (result != null) {
				PositionCursor ();
				return true;
			}

			return false;
		}

		public void SetSelection (int col, int row, bool extendExistingSelection)
		{
			if (!MultiSelect || !extendExistingSelection)
				MultiSelectedRegions.Clear ();

			if (extendExistingSelection) {
				if (MultiSelectedRegions.Count == 0) {
					var rect = CreateTableSelection (SelectedColumn, SelectedRow, col, row);
					MultiSelectedRegions.Push (rect);
				} else {
					var head = MultiSelectedRegions.Pop ();
					var newRect = CreateTableSelection (head.Origin.X, head.Origin.Y, col, row);
					MultiSelectedRegions.Push (newRect);
				}
			}

			SelectedColumn = col;
			SelectedRow = row;
		}

		public void ChangeSelectionByOffset (int offsetX, int offsetY, bool extendExistingSelection)
		{
			SetSelection (SelectedColumn + offsetX, SelectedRow + offsetY, extendExistingSelection);
			Update ();
		}

		public void PageUp(bool extend)
		{
			ChangeSelectionByOffset (0, -(Bounds.Height - GetHeaderHeightIfAny ()), extend);
			Update ();
		}

		public void PageDown(bool extend)
		{
			ChangeSelectionByOffset (0, Bounds.Height - GetHeaderHeightIfAny (), extend);
			Update ();
		}

		public void ChangeSelectionToStartOfTable (bool extend)
		{
			SetSelection (0, 0, extend);
			Update ();
		}

		public void ChangeSelectionToEndOfTable(bool extend)
		{
			SetSelection (Table.Columns.Count - 1, Table.Rows.Count - 1, extend);
			Update ();
		}


		public void ChangeSelectionToEndOfRow (bool extend)
		{
			SetSelection (Table.Columns.Count - 1, SelectedRow, extend);
			Update ();
		}

		public void ChangeSelectionToStartOfRow (bool extend)
		{
			SetSelection (0, SelectedRow, extend);
			Update ();
		}

		public void SelectAll ()
		{
			if (Table == null || !MultiSelect || Table.Rows.Count == 0)
				return;

			MultiSelectedRegions.Clear ();

			MultiSelectedRegions.Push (new TableSelection (new Point (SelectedColumn, SelectedRow), new Rect (0, 0, Table.Columns.Count, table.Rows.Count)));
			Update ();
		}

		public IEnumerable<Point> GetAllSelectedCells ()
		{
			if (Table == null || Table.Rows.Count == 0)
				yield break;

			EnsureValidSelection ();

			if (MultiSelect && MultiSelectedRegions.Any ()) {

				var yMin = MultiSelectedRegions.Min (r => r.Rect.Top);
				var yMax = MultiSelectedRegions.Max (r => r.Rect.Bottom);

				var xMin = FullRowSelect ? 0 : MultiSelectedRegions.Min (r => r.Rect.Left);
				var xMax = FullRowSelect ? Table.Columns.Count : MultiSelectedRegions.Max (r => r.Rect.Right);

				for (int y = yMin; y < yMax; y++) {
					for (int x = xMin; x < xMax; x++) {
						if (IsSelected (x, y)) {
							yield return new Point (x, y);
						}
					}
				}
			} else {

				if (FullRowSelect) {
					for (int x = 0; x < Table.Columns.Count; x++) {
						yield return new Point (x, SelectedRow);
					}
				} else {
					yield return new Point (SelectedColumn, SelectedRow);
				}
			}
		}

		private TableSelection CreateTableSelection (int pt1X, int pt1Y, int pt2X, int pt2Y)
		{
			var top = Math.Min (pt1Y, pt2Y);
			var bot = Math.Max (pt1Y, pt2Y);

			var left = Math.Min (pt1X, pt2X);
			var right = Math.Max (pt1X, pt2X);

			return new TableSelection (new Point (pt1X, pt1Y), new Rect (left, top, right - left + 1, bot - top + 1));
		}

		public bool IsSelected (int col, int row)
		{
			if (MultiSelect && MultiSelectedRegions.Any (r => r.Rect.Contains (col, row)))
				return true;

			if (FullRowSelect && MultiSelect && MultiSelectedRegions.Any (r => r.Rect.Bottom > row && r.Rect.Top <= row))
				return true;

			return row == SelectedRow &&
					(col == SelectedColumn || FullRowSelect);
		}

		public override void PositionCursor ()
		{
			if (Table == null) {
				base.PositionCursor ();
				return;
			}

			var screenPoint = CellToScreen (SelectedColumn, SelectedRow);

			if (screenPoint != null)
				Move (screenPoint.Value.X, screenPoint.Value.Y);
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
				me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp &&
				me.Flags != MouseFlags.WheeledLeft && me.Flags != MouseFlags.WheeledRight)
				return false;

			if (!HasFocus && CanFocus) {
				SetFocus ();
			}

			if (Table == null || Table.Columns.Count <= 0) {
				return false;
			}

			switch (me.Flags) {
			case MouseFlags.WheeledDown:
				RowOffset++;
				EnsureValidScrollOffsets ();
				SetNeedsDisplay ();
				return true;

			case MouseFlags.WheeledUp:
				RowOffset--;
				EnsureValidScrollOffsets ();
				SetNeedsDisplay ();
				return true;

			case MouseFlags.WheeledRight:
				ColumnOffset++;
				EnsureValidScrollOffsets ();
				SetNeedsDisplay ();
				return true;

			case MouseFlags.WheeledLeft:
				ColumnOffset--;
				EnsureValidScrollOffsets ();
				SetNeedsDisplay ();
				return true;
			}

			if (me.Flags.HasFlag (MouseFlags.Button1Clicked)) {

				if (scrollLeftPoint != null 
					&& scrollLeftPoint.Value.X == me.X
					&& scrollLeftPoint.Value.Y == me.Y)
				{
					ColumnOffset--;
					EnsureValidScrollOffsets ();
					SetNeedsDisplay ();
				}

				if (scrollRightPoint != null 
					&& scrollRightPoint.Value.X == me.X
					&& scrollRightPoint.Value.Y == me.Y)
				{
					ColumnOffset++;
					EnsureValidScrollOffsets ();
					SetNeedsDisplay ();
				}

				var hit = ScreenToCell (me.X, me.Y);
				if (hit != null) {

					SetSelection (hit.Value.X, hit.Value.Y, me.Flags.HasFlag (MouseFlags.ButtonShift));
					Update ();
				}
			}

			if (me.Flags == MouseFlags.Button1DoubleClicked) {
				var hit = ScreenToCell (me.X, me.Y);
				if (hit != null) {
					OnCellActivated (new CellActivatedEventArgs (Table, hit.Value.X, hit.Value.Y));
				}
			}

			return false;
		}

		public Point? ScreenToCell (int clientX, int clientY)
		{
			if (Table == null || Table.Columns.Count <= 0)
				return null;

			var viewPort = CalculateViewport (Bounds);

			var headerHeight = GetHeaderHeightIfAny ();

			var col = viewPort.LastOrDefault (c => c.X <= clientX);

			if (clientY < headerHeight)
				return null;

			var rowIdx = RowOffset - headerHeight + clientY;

			if (col != null && rowIdx >= 0) {

				return new Point (col.Column.Ordinal, rowIdx);
			}

			return null;
		}

		public Point? CellToScreen (int tableColumn, int tableRow)
		{
			if (Table == null || Table.Columns.Count <= 0)
				return null;

			var viewPort = CalculateViewport (Bounds);

			var headerHeight = GetHeaderHeightIfAny ();

			var colHit = viewPort.FirstOrDefault (c => c.Column.Ordinal == tableColumn);

			if (colHit == null)
				return null;

			if (RowOffset > tableRow)
				return null;

			if (tableRow > RowOffset + (Bounds.Height - headerHeight))
				return null;

			return new Point (colHit.X, tableRow + headerHeight - RowOffset);
		}
		public void Update ()
		{
			if (Table == null) {
				SetNeedsDisplay ();
				return;
			}

			EnsureValidScrollOffsets ();
			EnsureValidSelection ();

			EnsureSelectedCellIsVisible ();

			SetNeedsDisplay ();
		}

		public void EnsureValidScrollOffsets ()
		{
			if (Table == null) {
				return;
			}

			ColumnOffset = Math.Max (Math.Min (ColumnOffset, Table.Columns.Count - 1), 0);
			RowOffset = Math.Max (Math.Min (RowOffset, Table.Rows.Count - 1), 0);
		}


		public void EnsureValidSelection ()
		{
			if (Table == null) {

				MultiSelectedRegions.Clear ();
				return;
			}

			SelectedColumn = Math.Max (Math.Min (SelectedColumn, Table.Columns.Count - 1), 0);
			SelectedRow = Math.Max (Math.Min (SelectedRow, Table.Rows.Count - 1), 0);

			var oldRegions = MultiSelectedRegions.ToArray ().Reverse ();

			MultiSelectedRegions.Clear ();

			foreach (var region in oldRegions) {
				if (region.Rect.Top >= Table.Rows.Count)
					continue;

				if (region.Rect.Left >= Table.Columns.Count)
					continue;

				region.Origin = new Point (
					Math.Max (Math.Min (region.Origin.X, Table.Columns.Count - 1), 0),
					Math.Max (Math.Min (region.Origin.Y, Table.Rows.Count - 1), 0));

				region.Rect = Rect.FromLTRB (region.Rect.Left,
					region.Rect.Top,
					Math.Max (Math.Min (region.Rect.Right, Table.Columns.Count), 0),
					Math.Max (Math.Min (region.Rect.Bottom, Table.Rows.Count), 0)
					);

				MultiSelectedRegions.Push (region);
			}

		}

		public void EnsureSelectedCellIsVisible ()
		{
			if (Table == null || Table.Columns.Count <= 0) {
				return;
			}

			var columnsToRender = CalculateViewport (Bounds).ToArray ();
			var headerHeight = GetHeaderHeightIfAny ();

			if (SelectedColumn < columnsToRender.Min (r => r.Column.Ordinal)) {
				ColumnOffset = SelectedColumn;
			}

			if (SelectedColumn > columnsToRender.Max (r => r.Column.Ordinal)) {

				if(Style.SmoothHorizontalScrolling) {

					while(SelectedColumn > columnsToRender.Max (r => r.Column.Ordinal)) {

						ColumnOffset++;
						columnsToRender = CalculateViewport (Bounds).ToArray ();

						if (ColumnOffset >= Table.Columns.Count - 1)
							break;

					}
				}
				else {
					ColumnOffset = SelectedColumn;
				}
				
			}

			if (SelectedRow >= RowOffset + (Bounds.Height - headerHeight)) {
				RowOffset = SelectedRow - (Bounds.Height - headerHeight) + 1;
			}
			if (SelectedRow < RowOffset) {
				RowOffset = SelectedRow;
			}
		}

		protected virtual void OnSelectedCellChanged (SelectedCellChangedEventArgs args)
		{
			SelectedCellChanged?.Invoke (args);
		}

		protected virtual void OnCellActivated (CellActivatedEventArgs args)
		{
			CellActivated?.Invoke (args);
		}

		private IEnumerable<ColumnToRender> CalculateViewport (Rect bounds, int padding = 1)
		{
			if (Table == null || Table.Columns.Count <= 0)
				yield break;

			int usedSpace = 0;

			if (Style.ShowVerticalHeaderLines || Style.ShowVerticalCellLines)
				usedSpace += 1;

			int availableHorizontalSpace = bounds.Width;
			int rowsToRender = bounds.Height;

			if (ShouldRenderHeaders ())
				rowsToRender -= GetHeaderHeight ();

			bool first = true;
			var lastColumn = Table.Columns.Cast<DataColumn> ().Last ();

			foreach (var col in Table.Columns.Cast<DataColumn> ().Skip (ColumnOffset)) {

				int startingIdxForCurrentHeader = usedSpace;
				var colStyle = Style.GetColumnStyleIfAny (col);
				int colWidth;

				usedSpace += colWidth = CalculateMaxCellWidth (col, rowsToRender, colStyle) + padding;

				if (!first && usedSpace > availableHorizontalSpace)
					yield break;

				yield return new ColumnToRender (col, startingIdxForCurrentHeader,
					Math.Min (availableHorizontalSpace, colWidth),
					lastColumn == col);
				first = false;
			}
		}

		private bool ShouldRenderHeaders ()
		{
			if (Table == null || Table.Columns.Count == 0)
				return false;

			return Style.AlwaysShowHeaders || rowOffset == 0;
		}

		private int CalculateMaxCellWidth (DataColumn col, int rowsToRender, ColumnStyle colStyle)
		{
			int spaceRequired = col.ColumnName.Sum (c => Rune.ColumnWidth (c));

			if (RowOffset < 0)
				return spaceRequired;


			for (int i = RowOffset; i < RowOffset + rowsToRender && i < Table.Rows.Count; i++) {

				spaceRequired = Math.Max (spaceRequired, GetRepresentation (Table.Rows [i] [col], colStyle).Sum (c => Rune.ColumnWidth (c)));
			}

			if (colStyle != null) {

				if (spaceRequired > colStyle.MaxWidth) {
					spaceRequired = colStyle.MaxWidth;
				}

				if (spaceRequired < colStyle.MinWidth) {
					spaceRequired = colStyle.MinWidth;
				}
			}

			if (spaceRequired > MaxCellWidth)
				spaceRequired = MaxCellWidth;


			return spaceRequired;
		}

		private string GetRepresentation (object value, ColumnStyle colStyle)
		{
			if (value == null || value == DBNull.Value) {
				return NullSymbol;
			}

			return colStyle != null ? colStyle.GetRepresentation (value) : value.ToString ();
		}

		public delegate ColorScheme CellColorGetterDelegate (CellColorGetterArgs args);

		public delegate ColorScheme RowColorGetterDelegate (RowColorGetterArgs args);

		#region Nested Types
		public class ColumnStyle {

			public TextAlignment Alignment { get; set; }

			public Func<object, TextAlignment> AlignmentGetter;

			public Func<object, string> RepresentationGetter;

			public CellColorGetterDelegate ColorGetter;

			public string Format { get; set; }

			public int MaxWidth { get; set; } = TableView.DefaultMaxCellWidth;

			public int MinWidth { get; set; }

			public TextAlignment GetAlignment (object cellValue)
			{
				if (AlignmentGetter != null)
					return AlignmentGetter (cellValue);

				return Alignment;
			}

			public string GetRepresentation (object value)
			{
				if (!string.IsNullOrWhiteSpace (Format)) {

					if (value is IFormattable f)
						return f.ToString (Format, null);
				}


				if (RepresentationGetter != null)
					return RepresentationGetter (value);

				return value?.ToString ();
			}
		}
		public class TableStyle {

			public bool AlwaysShowHeaders { get; set; } = false;

			public bool ShowHorizontalHeaderOverline { get; set; } = true;

			public bool ShowHorizontalHeaderUnderline { get; set; } = true;

			public bool ShowVerticalCellLines { get; set; } = true;

			public bool ShowVerticalHeaderLines { get; set; } = true;

			public bool ShowHorizontalScrollIndicators { get; set; } = true;

			public bool InvertSelectedCellFirstCharacter { get; set; } = false;

			public Dictionary<DataColumn, ColumnStyle> ColumnStyles { get; set; } = new Dictionary<DataColumn, ColumnStyle> ();

			public RowColorGetterDelegate RowColorGetter {get;set;}

			public bool ExpandLastColumn {get;set;} = true;

			public bool SmoothHorizontalScrolling { get; set; } = true;
			
			public ColumnStyle GetColumnStyleIfAny (DataColumn col)
			{
				return ColumnStyles.TryGetValue (col, out ColumnStyle result) ? result : null;
			}

			public ColumnStyle GetOrCreateColumnStyle (DataColumn col)
			{
				if (!ColumnStyles.ContainsKey (col))
					ColumnStyles.Add (col, new ColumnStyle ());

				return ColumnStyles [col];
			}
		}

		internal class ColumnToRender {

			public DataColumn Column { get; set; }

			public int X { get; set; }

			public int Width { get; }

			public bool IsVeryLast { get; }

			public ColumnToRender (DataColumn col, int x, int width, bool isVeryLast)
			{
				Column = col;
				X = x;
				Width = width;
				IsVeryLast = isVeryLast;
			}

		}

		public class CellColorGetterArgs {

			public DataTable Table { get; }

			public int RowIndex { get; }

			public int ColIdex { get; }

			public object CellValue { get; }

			public string Representation { get; }

			public ColorScheme RowScheme { get; }

			internal CellColorGetterArgs (DataTable table, int rowIdx, int colIdx, object cellValue, string representation, ColorScheme rowScheme)
			{
				Table = table;
				RowIndex = rowIdx;
				ColIdex = colIdx;
				CellValue = cellValue;
				Representation = representation;
				RowScheme = rowScheme;
			}

		}

		public class RowColorGetterArgs {

			public DataTable Table { get; }

			public int RowIndex { get; }

			internal RowColorGetterArgs (DataTable table, int rowIdx)
			{
				Table = table;
				RowIndex = rowIdx;
			}
		}

		public class SelectedCellChangedEventArgs : EventArgs {
			public DataTable Table { get; }


			public int OldCol { get; }


			public int NewCol { get; }


			public int OldRow { get; }


			public int NewRow { get; }

			public SelectedCellChangedEventArgs (DataTable t, int oldCol, int newCol, int oldRow, int newRow)
			{
				Table = t;
				OldCol = oldCol;
				NewCol = newCol;
				OldRow = oldRow;
				NewRow = newRow;
			}
		}

		public class TableSelection {

			public Point Origin { get; set; }

			public Rect Rect { get; set; }

			public TableSelection (Point origin, Rect rect)
			{
				Origin = origin;
				Rect = rect;
			}

			public override string ToString () =>
			    $"Origin:{Origin}, Rect:{Rect}";
		}
		#endregion
	}
}

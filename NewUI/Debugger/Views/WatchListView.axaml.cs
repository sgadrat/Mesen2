using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Linq;
using Mesen.Debugger.ViewModels;
using Avalonia.Threading;
using Avalonia.Input;
using Mesen.Debugger.Utilities;
using Mesen.Config;
using System;
using Mesen.Debugger.Controls;
using Mesen.Utilities;

namespace Mesen.Debugger.Views
{
	public class WatchListView : UserControl
	{
		private WatchListViewModel? _model;

		public WatchListView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}

		protected override void OnDataContextChanged(EventArgs e)
		{
			if(DataContext is WatchListViewModel model) {
				_model = model;
			}
			base.OnDataContextChanged(e);
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();
			InitContextMenu();
		}

		private void InitContextMenu()
		{
			MesenDataGrid grid = this.FindControl<MesenDataGrid>("DataGrid");

			DebugShortcutManager.CreateContextMenu(this, new object[] {
				new ContextMenuAction() {
					ActionType = ActionType.Delete,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.WatchList_Delete),
					IsEnabled = () => grid.SelectedItems.Count > 0,
					OnClick = () => {
						_model!.DeleteWatch(grid.SelectedItems.Cast<WatchValueInfo>().ToList());
					}
				},

				new Separator(),

				new ContextMenuAction() {
					ActionType = ActionType.MoveUp,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.WatchList_MoveUp),
					IsEnabled = () => grid.SelectedItems.Count == 1 && grid.SelectedIndex > 0,
					OnClick = () => {
						_model!.MoveUp(grid.SelectedIndex);
					}
				},

				new ContextMenuAction() {
					ActionType = ActionType.MoveDown,
					Shortcut = () => ConfigManager.Config.Debug.Shortcuts.Get(DebuggerShortcut.WatchList_MoveDown),
					IsEnabled = () => grid.SelectedItems.Count == 1 && grid.SelectedIndex < _model!.WatchEntries.Count - 2,
					OnClick = () => {
						_model!.MoveDown(grid.SelectedIndex);
					}
				},

				new Separator(),

				new ContextMenuAction() {
					ActionType = ActionType.RowDisplayFormat,
					SubActions = new() {
						new ContextMenuAction() {
							ActionType = ActionType.RowFormatBinary,
							OnClick = () => _model!.SetSelectionFormat(WatchFormatStyle.Binary, 1, grid.SelectedItems.Cast<WatchValueInfo>().ToList())
						},
						new Separator(),
						new ContextMenuAction() {
							ActionType = ActionType.RowFormatHex8Bits,
							OnClick = () => _model!.SetSelectionFormat(WatchFormatStyle.Hex, 1, grid.SelectedItems.Cast<WatchValueInfo>().ToList())
						},
						new ContextMenuAction() {
							ActionType = ActionType.RowFormatHex16Bits,
							OnClick = () => _model!.SetSelectionFormat(WatchFormatStyle.Hex, 2, grid.SelectedItems.Cast<WatchValueInfo>().ToList())
						},
						new ContextMenuAction() {
							ActionType = ActionType.RowFormatHex24Bits,
							OnClick = () => _model!.SetSelectionFormat(WatchFormatStyle.Hex, 3, grid.SelectedItems.Cast<WatchValueInfo>().ToList())
						},
						new Separator(),
						new ContextMenuAction() {
							ActionType = ActionType.RowFormatSigned8Bits,
							OnClick = () => _model!.SetSelectionFormat(WatchFormatStyle.Signed, 1, grid.SelectedItems.Cast<WatchValueInfo>().ToList())
						},
						new ContextMenuAction() {
							ActionType = ActionType.RowFormatSigned16Bits,
							OnClick = () => _model!.SetSelectionFormat(WatchFormatStyle.Signed, 2, grid.SelectedItems.Cast<WatchValueInfo>().ToList())
						},
						new ContextMenuAction() {
							ActionType = ActionType.RowFormatSigned24Bits,
							OnClick = () => _model!.SetSelectionFormat(WatchFormatStyle.Signed, 3, grid.SelectedItems.Cast<WatchValueInfo>().ToList())
						},
						new Separator(),
						new ContextMenuAction() {
							ActionType = ActionType.RowFormatUnsigned,
							OnClick = () => _model!.SetSelectionFormat(WatchFormatStyle.Unsigned, 1, grid.SelectedItems.Cast<WatchValueInfo>().ToList())
						},
						new Separator(),
						new ContextMenuAction() {
							ActionType = ActionType.ClearFormat,
							OnClick = () => _model!.ClearSelectionFormat(grid.SelectedItems.Cast<WatchValueInfo>().ToList())
						}
					}
				},


				new Separator(),

				new ContextMenuAction() {
					ActionType = ActionType.WatchDecimalDisplay,
					IsSelected = () => ConfigManager.Config.Debug.Debugger.WatchFormat == WatchFormatStyle.Unsigned,
					OnClick = () => {
						ConfigManager.Config.Debug.Debugger.WatchFormat = WatchFormatStyle.Unsigned;
					}
				},

				new ContextMenuAction() {
					ActionType = ActionType.WatchHexDisplay,
					IsSelected = () => ConfigManager.Config.Debug.Debugger.WatchFormat == WatchFormatStyle.Hex,
					OnClick = () => {
						ConfigManager.Config.Debug.Debugger.WatchFormat = WatchFormatStyle.Hex;
					}
				},

				new ContextMenuAction() {
					ActionType = ActionType.WatchBinaryDisplay,
					IsSelected = () => ConfigManager.Config.Debug.Debugger.WatchFormat == WatchFormatStyle.Binary,
					OnClick = () => {
						ConfigManager.Config.Debug.Debugger.WatchFormat = WatchFormatStyle.Binary;
					}
				},

				new Separator(),

				new ContextMenuAction() {
					ActionType = ActionType.Import,
					OnClick = async () => {
						string? filename = await FileDialogHelper.OpenFile(null, VisualRoot, FileDialogHelper.WatchFileExt);
						if(filename !=null) {
							_model!.Manager.Import(filename);
						}
					}
				},

				new ContextMenuAction() {
					ActionType = ActionType.Export,
					OnClick = async () => {
						string? filename = await FileDialogHelper.SaveFile(null, null, VisualRoot, FileDialogHelper.WatchFileExt);
						if(filename != null) {
							_model!.Manager.Export(filename);
						}
					}
				},
			});
		}

		private void OnCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e)
		{
			if(e.EditAction == DataGridEditAction.Commit) {
				Dispatcher.UIThread.Post(() => {
					int index = e.Row.GetIndex();
					((WatchListViewModel)DataContext!).EditWatch(index, ((WatchValueInfo)e.Row.DataContext!).Expression);
				});
			}
		}

		private static bool IsTextKey(Key key)
		{
			return key >= Key.A && key <= Key.Z || key >= Key.D0 && key <= Key.D9 || key >= Key.NumPad0 && key <= Key.Divide || key >= Key.OemSemicolon && key <= Key.Oem102;
		}

		private void OnGridKeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Escape) {
				((DataGrid)sender).CancelEdit();
			} else if(IsTextKey(e.Key)) {
				((DataGrid)sender).CurrentColumn = ((DataGrid)sender).Columns[0];
				((DataGrid)sender).BeginEdit();
			}
		}
	}
}

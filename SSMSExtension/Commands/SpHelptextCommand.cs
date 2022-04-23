using EnvDTE;
using EnvDTE80;
using Microsoft.SqlServer.Management.UI.Grid;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using SSMSExtension.Controls;
using SSMSExtension.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Web;
using Task = System.Threading.Tasks.Task;

namespace SSMSExtension.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    sealed class SpHelptextCommand
    {
        public const int ExecuteSpHelpStatementCommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("41af3e7b-cff6-4b6a-b35f-2bd9aa8ea92f");
        public static SpHelptextCommand Instance { get; private set; }
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;
        public CommandEvents QueryExecuteEvent { get; private set; }
        private readonly AsyncPackage package;
        private readonly DTE2 dte;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpHelptextWindowCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SpHelptextCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            dte = (DTE2)ServiceProvider.GetServiceAsync(typeof(DTE)).Result;

            // Create execute current statement menu item
            var menuCommandID = new CommandID(CommandSet, ExecuteSpHelpStatementCommandId);
            var menuCommand = new OleMenuCommand(Command_ExecAsync, menuCommandID);
            menuCommand.BeforeQueryStatus += Command_QueryStatus;
            commandService.AddCommand(menuCommand);
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in FacetsDataWindowCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SpHelptextCommand(package, commandService);
        }

        private void Command_QueryStatus(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is OleMenuCommand menuCommand)
            {
                menuCommand.Enabled = false;
                menuCommand.Supported = false;

                if (dte.HasActiveDocument())
                {
                    menuCommand.Enabled = dte.ActiveWindow.HWnd == dte.ActiveDocument.ActiveWindow.HWnd;
                    menuCommand.Supported = true;
                }
            }
        }

        private async void Command_ExecAsync(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (sender is OleMenuCommand menuCommand)
            {
                var executor = new ExecutorHelper(dte);
                var selection = executor.ExecuteSpHelpStatement();

                var queryWindow = dte.ActiveDocument;
                await Task.Delay(1500);
                GetResults(selection);
                queryWindow.Activate();
            }
        }

        public void GetResults(string selection)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var objType = ServiceCache.ScriptFactory.GetType();
            var method1 = objType.GetMethod("GetCurrentlyActiveFrameDocView", BindingFlags.NonPublic | BindingFlags.Instance);
            var Result = method1.Invoke(ServiceCache.ScriptFactory, new object[] { ServiceCache.VSMonitorSelection, false, null });

            var objType2 = Result.GetType();
            var field = objType2.GetField("m_sqlResultsControl", BindingFlags.NonPublic | BindingFlags.Instance);
            var SQLResultsControl = field.GetValue(Result);

            var m_gridResultsPage = GetNonPublicField(SQLResultsControl, "m_gridResultsPage");
            CollectionBase gridContainers = GetNonPublicField(m_gridResultsPage, "m_gridContainers") as CollectionBase;

            var output = new StringBuilder();

            foreach (var gridContainer in gridContainers)
            {
                var grid = GetNonPublicField(gridContainer, "m_grid") as GridControl;
                var gridStorage = grid.GridStorage;
                var schemaTable = GetNonPublicField(gridStorage, "m_schemaTable") as DataTable;

                var data = new DataTable();

                for (long i = 0; i < gridStorage.NumRows(); i++)
                {
                    var rowItems = new List<object>();

                    for (int c = 0; c < schemaTable.Rows.Count; c++)
                    {
                        var columnName = schemaTable.Rows[c][0].ToString();
                        var columnType = schemaTable.Rows[c][12] as Type;

                        if (!data.Columns.Contains(columnName))
                        {
                            data.Columns.Add(columnName, columnType);
                        }

                        var cellData = gridStorage.GetCellDataAsString(i, c + 1);

                        if (cellData == "NULL")
                        {
                            rowItems.Add(null);

                            continue;
                        }

                        if (columnType == typeof(bool))
                        {
                            cellData = cellData == "0" ? "False" : "True";
                        }

                        Console.WriteLine($"Parsing {columnName} with '{cellData}'");
                        var typedValue = Convert.ChangeType(cellData, columnType, CultureInfo.InvariantCulture);

                        output.Append(cellData);

                        rowItems.Add(typedValue);
                    }

                    data.Rows.Add(rowItems.ToArray());
                }

                data.AcceptChanges();


                var encoded = HttpUtility.HtmlEncode(output.ToString());
                var jsonHttp = JsonConvert.SerializeObject(encoded, Formatting.Indented);

                // Get the instance number 0 of this tool window. This window is single instance so this instance
                // is actually the only one.
                // The last flag is set to true so that if the tool window does not exists it will be created.
                ToolWindowPane window = this.package.FindToolWindow(typeof(SpHelptextWindow), 0, true);
                if ((null == window) || (null == window.Frame))
                {
                    throw new NotSupportedException("Cannot create tool window");
                }

                var diveWindow = window as SpHelptextWindow;
                diveWindow.SetData(jsonHttp);
                diveWindow.SetHeader(selection);

                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            }
        }

        public object GetNonPublicField(object obj, string field)
        {
            FieldInfo f = obj.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);

            return f?.GetValue(obj);
        }

    }
}

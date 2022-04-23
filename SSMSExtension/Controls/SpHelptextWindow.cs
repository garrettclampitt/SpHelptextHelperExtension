namespace SSMSExtension.Controls
{
    using Microsoft.VisualStudio.Shell;
    using System;
    using System.Runtime.InteropServices;

    [Guid("30dbdb0d-a886-4edc-82ed-25344f8bd6e2")]
    public class SpHelptextWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpHelptextWindow"/> class.
        /// </summary>
        public SpHelptextWindow() : base(null)
        {
            this.Caption = "sp_helptext";
            this.Content = new SpHelptextWindowControl();
        }

        public void SetData(string json) => ((SpHelptextWindowControl)Content).SetData(json);

        public void SetHeader(string title) => ((SpHelptextWindowControl)Content).SetHeader(title);
    }
}

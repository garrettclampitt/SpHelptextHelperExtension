using EnvDTE80;

namespace SpHelptextHelperExtension.Helpers
{
    static class Helpers
    {
        public static bool HasActiveDocument(this DTE2 dte)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            if (dte != null && dte.ActiveDocument != null)
            {
                var doc = (dte.ActiveDocument.DTE)?.ActiveDocument;
                return doc != null;
            }

            return false;
        }

        public static EnvDTE.Document GetDocument(this DTE2 dte)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            if (dte.HasActiveDocument())
            {
                return (dte.ActiveDocument.DTE)?.ActiveDocument;
            }

            return null;
        }
    }
}

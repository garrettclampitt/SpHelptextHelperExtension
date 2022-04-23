using EnvDTE80;

namespace SSMSExtension.Helpers
{
    static class Helpers
    {
        public static bool HasActiveDocument(this DTE2 dte)
        {
            if (dte != null && dte.ActiveDocument != null)
            {
                var doc = (dte.ActiveDocument.DTE)?.ActiveDocument;
                return doc != null;
            }

            return false;
        }

        public static EnvDTE.Document GetDocument(this DTE2 dte)
        {
            if (dte.HasActiveDocument())
            {
                return (dte.ActiveDocument.DTE)?.ActiveDocument;
            }

            return null;
        }
    }
}

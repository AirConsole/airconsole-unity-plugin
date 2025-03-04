#if !DISABLE_AIRCONSOLE
using System.Text;
using System.Xml;

namespace NDream.AirConsole.Editor {
    // Approach based on https://stackoverflow.com/questions/56886994/c-sharp-adding-attribute-to-xml-element-appends-the-namespace-to-the-end-of-the
    internal class AndroidXmlDocument : XmlDocument {
        private readonly string documentPath;
        private readonly XmlNamespaceManager nsManager;
        internal readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
        internal readonly string ToolsXmlNamespace = "http://schemas.android.com/tools";

        internal AndroidXmlDocument(string path) {
            documentPath = path;
            using (XmlTextReader reader = new(documentPath)) {
                reader.Read();
                Load(reader);
            }
            nsManager = new XmlNamespaceManager(NameTable);
            nsManager.AddNamespace("android", AndroidXmlNamespace);
            nsManager.AddNamespace("tools", ToolsXmlNamespace);
        }

        /// <summary>
        /// Saves the XML document at the current documentPath.
        /// </summary>
        /// <returns>Returns the path at which the document was saved.</returns>
        internal string Save() => SaveAs(documentPath);

        private string SaveAs(string path) {
            using (XmlTextWriter writer = new(path, new UTF8Encoding(false))) {
                writer.Formatting = Formatting.Indented;
                Save(writer);
            }
            return path;
        }
    }
}
#endif
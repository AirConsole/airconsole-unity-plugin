#if !DISABLE_AIRCONSOLE
using System.Text;
using System.Xml;

namespace NDream.AirConsole.Editor {
    // Approach based on https://stackoverflow.com/questions/56886994/c-sharp-adding-attribute-to-xml-element-appends-the-namespace-to-the-end-of-the
    internal class AndroidXmlDocument : XmlDocument {
        private readonly string documentPath;
        private readonly XmlNamespaceManager nsManager;
        internal readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
        internal readonly string ToolsXmlNamespace = "http://schemas.android.com/apk/res/tools";

        internal AndroidXmlDocument(string path) {
            documentPath = path;
            using (XmlTextReader reader = new XmlTextReader(documentPath)) {
                reader.Read();
                Load(reader);
            }
            nsManager = new XmlNamespaceManager(NameTable);
            nsManager.AddNamespace("android", AndroidXmlNamespace);
            nsManager.AddNamespace("tools", ToolsXmlNamespace);
        }

        internal string Save() => SaveAs(documentPath);

        private string SaveAs(string path) {
            using (XmlTextWriter writer = new XmlTextWriter(path, new UTF8Encoding(false))) {
                writer.Formatting = Formatting.Indented;
                Save(writer);
            }
            return path;
        }
    }
}
#endif
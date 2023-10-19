#if !DISABLE_AIRCONSOLE
using System.Text;
using System.Xml;

// Approach based on https://stackoverflow.com/questions/56886994/c-sharp-adding-attribute-to-xml-element-appends-the-namespace-to-the-end-of-the
internal class AndroidXmlDocument : XmlDocument
{
    private string m_Path;
    protected XmlNamespaceManager nsMgr;
    public readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
    public readonly string ToolsXmlNamespace = "http://schemas.android.com/apk/res/tools";
    public AndroidXmlDocument(string path)
    {
        m_Path = path;
        using (var reader = new XmlTextReader(m_Path))
        {
            reader.Read();
            Load(reader);
        }
        nsMgr = new XmlNamespaceManager(NameTable);
        nsMgr.AddNamespace("android", AndroidXmlNamespace);
        nsMgr.AddNamespace("tools", ToolsXmlNamespace);
    }

    public string Save()
    {
        return SaveAs(m_Path);
    }

    public string SaveAs(string path)
    {
        using (var writer = new XmlTextWriter(path, new UTF8Encoding(false)))
        {
            writer.Formatting = Formatting.Indented;
            Save(writer);
        }
        return path;
    }
}
#endif
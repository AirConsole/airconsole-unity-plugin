#if !DISABLE_AIRCONSOLE
using System.Xml;

namespace NDream.AirConsole.Editor {
	// Approach based on https://stackoverflow.com/questions/56886994/c-sharp-adding-attribute-to-xml-element-appends-the-namespace-to-the-end-of-the
	internal class AndroidManifest : AndroidXmlDocument {
		private readonly XmlElement ApplicationElement;

		internal AndroidManifest(string path) : base(path) { ApplicationElement = SelectSingleNode("/manifest/application") as XmlElement; }

		internal XmlAttribute GenerateAttribute(string prefix, string key, string value, string XmlNamespace) {
			XmlAttribute attr = CreateAttribute(prefix, key, XmlNamespace);
			attr.Value = value;
			return attr;
		}

		internal void SetAttribute(XmlAttribute Attribute) { ApplicationElement.Attributes.Append(Attribute); }
	}
}
#endif
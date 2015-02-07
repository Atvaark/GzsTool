using System.Xml.Serialization;

namespace GzsTool.Pftxs
{
    [XmlType("Entry")]
    public class PftxsLogEntry
    {
        [XmlAttribute("FileDirectory")]
        public string FileDirectory { get; set; }

        [XmlAttribute("FileName")]
        public string FileName { get; set; }

        [XmlAttribute("SubFileCount")]
        public int SubFileCount { get; set; }
    }
}

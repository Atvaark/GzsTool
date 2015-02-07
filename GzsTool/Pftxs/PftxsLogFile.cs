using System.Collections.Generic;
using System.Xml.Serialization;

namespace GzsTool.Pftxs
{
    [XmlRoot("PftxsFile")]
    public class PftxsLogFile
    {
        public PftxsLogFile()
        {
            Entries = new List<PftxsLogEntry>();
        }

        [XmlAttribute("ArchiveName")]
        public string ArchiveName { get; set; }

        [XmlArray("Entries")]
        public List<PftxsLogEntry> Entries { get; set; }
    }
}

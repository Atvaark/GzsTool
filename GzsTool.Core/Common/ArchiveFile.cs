using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using GzsTool.Core.Common.Interfaces;

namespace GzsTool.Core.Common
{
    [XmlType]
    public abstract class ArchiveFile
    {
        public abstract void Read(Stream input);
        public abstract IEnumerable<FileDataStreamContainer> ExportFiles(Stream input);
        public abstract void Write(Stream output, IDirectory inputDirectory);

        [XmlAttribute("Name")]
        public string Name { get; set; }
    }
}

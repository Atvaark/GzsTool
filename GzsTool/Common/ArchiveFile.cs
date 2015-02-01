using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace GzsTool.Common
{
    [XmlType]
    public abstract class ArchiveFile
    {
        public abstract void Read(Stream input);
        public abstract IEnumerable<FileDataContainer> ExportFiles(Stream input);
        public abstract void Write(Stream output, string directory);
    }
}

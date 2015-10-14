using System.Xml.Serialization;

namespace GzsTool.Core.Fpk
{
    public enum FpkType : byte
    {
        [XmlEnum("Fpk")] Fpk = 0x00,
        [XmlEnum("Fpkd")] Fpkd = 0x64
    }
}

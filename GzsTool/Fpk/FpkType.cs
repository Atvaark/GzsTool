using System.Xml.Serialization;

namespace GzsTool.Fpk
{
    public enum FpkType : byte
    {
        [XmlEnum("Fpk")] Fpk = 0x00,
        [XmlEnum("Fpkd")] Fpkd = 0x64
    }
}

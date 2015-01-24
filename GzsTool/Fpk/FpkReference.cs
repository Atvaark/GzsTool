using System.IO;
using System.Xml.Serialization;

namespace GzsTool.Fpk
{
    [XmlType("Reference")]
    public class FpkReference
    {
        [XmlElement("FileName")]
        public FpkString ReferenceFileName { get; set; }

        public static FpkReference ReadFpkReference(Stream input)
        {
            FpkReference fpkReference = new FpkReference();
            fpkReference.Read(input);
            return fpkReference;
        }

        private void Read(Stream input)
        {
            ReferenceFileName = FpkString.ReadFpkString(input);
        }
    }
}

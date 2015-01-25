using System.IO;
using System.Xml.Serialization;

namespace GzsTool.Fpk
{
    [XmlType("Reference", Namespace = "Fpk")]
    public class FpkReference
    {
        public FpkReference()
        {
            ReferenceFilePath = new FpkString();
        }

        [XmlIgnore]
        public FpkString ReferenceFilePath { get; set; }

        [XmlAttribute("FilePath")]
        public string FilePath
        {
            get { return ReferenceFilePath.Value; }
            set { ReferenceFilePath.Value = value; }
        }

        public static FpkReference ReadFpkReference(Stream input)
        {
            FpkReference fpkReference = new FpkReference();
            fpkReference.Read(input);
            return fpkReference;
        }

        private void Read(Stream input)
        {
            ReferenceFilePath = FpkString.ReadFpkString(input);
        }

        public void WriteFilePath(FileStream output)
        {
            ReferenceFilePath.WriteString(output);
        }

        public void Write(FileStream output)
        {
            ReferenceFilePath.Write(output);
        }
    }
}

using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using GzsTool.Utility;

namespace GzsTool.Fpk
{
    public class FpkString
    {
        [XmlAttribute("Value")]
        public string Value { get; set; }

        [XmlIgnore]
        public int OffsetString { get; set; }

        [XmlIgnore]
        public int Length { get; set; }

        [XmlIgnore]
        public bool NameFound { get; set; }

        public static FpkString ReadFpkString(Stream input)
        {
            FpkString fpkString = new FpkString();
            fpkString.Read(input);
            return fpkString;
        }

        private void Read(Stream input)
        {
            BinaryReader reader = new BinaryReader(input, Encoding.Default, true);
            OffsetString = reader.ReadInt32();
            reader.Skip(4);
            Length = reader.ReadInt32();
            reader.Skip(4);

            long endPosition = input.Position;
            input.Position = OffsetString;
            Value = reader.ReadString(Length);
            input.Position = endPosition;
        }

        public override string ToString()
        {
            return Value;
        }

        public bool ResolveString(byte[] md5Hash)
        {
            bool resolved;
            byte[] entryNameHash = Hashing.Md5HashText(Value);

            if (entryNameHash.SequenceEqual(md5Hash) == false)
            {
                string fileName;
                resolved = Hashing.TryGetFileNameFromMd5Hash(md5Hash, Value, out fileName);
                Value = fileName;
            }
            else
            {
                resolved = true;
            }

            NameFound = resolved;
            return resolved;
        }
    }
}

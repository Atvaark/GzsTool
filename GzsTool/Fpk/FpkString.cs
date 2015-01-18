using System.IO;
using System.Text;

namespace GzsTool.Fpk
{
    public class FpkString
    {
        public string Name { get; set; }
        public int OffsetString { get; set; }
        public int Length { get; set; }

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
            Name = reader.ReadString(Length);
            input.Position = endPosition;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

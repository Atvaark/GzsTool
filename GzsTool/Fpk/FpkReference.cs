using System.IO;

namespace GzsTool.Fpk
{
    internal class FpkReference
    {
        public FpkString Reference { get; set; }

        public static FpkReference ReadFpkReference(Stream input)
        {
            FpkReference fpkReference = new FpkReference();
            fpkReference.Read(input);
            return fpkReference;
        }

        private void Read(Stream input)
        {
            Reference = FpkString.ReadFpkString(input);
        }
    }
}

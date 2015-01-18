using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GzsTool
{
    internal static class Hashing
    {
        private static readonly MD5 Md5 = MD5.Create();
        private static readonly Dictionary<ulong, string> HashNameDictionary = new Dictionary<ulong, string>();

        private static readonly Dictionary<byte[], string> Md5HashNameDictionary =
            new Dictionary<byte[], string>(new StructuralEqualityComparer<byte[]>());

        private static readonly Dictionary<int, string> TypeExtensions = new Dictionary<int, string>
        {
            {0, ""},
            {1, "xml"},
            {2, "json"},
            {3, "ese"},
            {4, "fxp"},
            {5, "fpk"},
            {6, "fpkd"},
            {7, "fpkl"},
            {8, "aib"},
            {9, "frig"},
            {10, "mtar"},
            {11, "gani"},
            {12, "evb"},
            {13, "evf"},
            {14, "ag.evf"},
            {15, "cc.evf"},
            {16, "fx.evf"},
            {17, "sd.evf"},
            {18, "vo.evf"},
            {19, "fsd"},
            {20, "fage"},
            {21, "fago"},
            {22, "fag"},
            {23, "fagx"},
            {24, "fagp"},
            {25, "frdv"},
            {26, "fdmg"},
            {27, "des"},
            {28, "fdes"},
            {29, "aibc"},
            {30, "mtl"},
            {31, "fsml"},
            {32, "fox"},
            {33, "fox2"},
            {34, "las"},
            {35, "fstb"},
            {36, "lua"},
            {37, "fcnp"},
            {38, "fcnpx"},
            {39, "sub"},
            {40, "fova"},
            {41, "lad"},
            {42, "lani"},
            {43, "vfx"},
            {44, "vfxbin"},
            {45, "frt"},
            {46, "gpfp"},
            {47, "gskl"},
            {48, "geom"},
            {49, "tgt"},
            {50, "path"},
            {51, "fmdl"},
            {52, "ftex"},
            {53, "htre"},
            {54, "tre2"},
            {55, "grxla"},
            {56, "grxoc"},
            {57, "mog"},
            {58, "pftxs"},
            {59, "nav2"},
            {60, "bnd"},
            {61, "parts"},
            {62, "phsd"},
            {63, "ph"},
            {64, "veh"},
            {65, "sdf"},
            {66, "sad"},
            {67, "sim"},
            {68, "fclo"},
            {69, "clo"},
            {70, "lng"},
            {71, "uig"},
            {72, "uil"},
            {73, "uif"},
            {74, "uia"},
            {75, "fnt"},
            {76, "utxl"},
            {77, "uigb"},
            {78, "vfxdb"},
            {79, "rbs"},
            {80, "aia"},
            {81, "aim"},
            {82, "aip"},
            {83, "aigc"},
            {84, "aig"},
            {85, "ait"},
            {86, "fsm"},
            {87, "obr"},
            {88, "obrb"},
            {89, "lpsh"},
            {90, "sani"},
            {91, "rdb"},
            {92, "phep"},
            {93, "simep"},
            {94, "atsh"},
            {95, "txt"},
            {96, "1.ftexs"},
            {97, "2.ftexs"},
            {98, "3.ftexs"},
            {99, "4.ftexs"},
            {100, "5.ftexs"},
            {101, "sbp"},
            {102, "mas"},
            {103, "rdf"},
            {104, "wem"},
            {105, "lba"},
            {106, "uilb"}
        };

        private static ulong HashFileName(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            const ulong seed0 = 0x9ae16a3b2f90404f;
            ulong seed1 = text.Length > 0 ? (uint) ((text[0]) << 16) + (uint) text.Length : 0;
            return CityHash.CityHash.CityHash64WithSeeds(text + "\0", seed0, seed1) & 0xFFFFFFFFFFFF;
        }

        internal static string GetFileNameFromHash(ulong hash, int fileExtensionId)
        {
            string fileName;
            string fileExtension = TypeExtensions[fileExtensionId];
            ulong hashMasked = hash & 0xFFFFFFFFFFFF;
            if (HashNameDictionary.TryGetValue(hashMasked, out fileName) == false)
            {
                fileName = String.Format("{0:x}", hashMasked);
            }
            fileName = String.Format("{0}.{1}", fileName, fileExtension);
            return fileName;
        }

        public static void ReadDictionary(string path)
        {
            foreach (var line in File.ReadAllLines(path))
            {
                ulong hash = HashFileName(line);
                if (HashNameDictionary.ContainsKey(hash) == false)
                {
                    HashNameDictionary.Add(hash, line);
                }
            }
        }

        internal static byte[] Md5HashText(string text)
        {
            return Md5.ComputeHash(Encoding.Default.GetBytes(text));
        }

        public static void ReadMd5Dictionary(string path)
        {
            foreach (var line in File.ReadAllLines(path))
            {
                byte[] md5Hash = Md5HashText(line);
                if (Md5HashNameDictionary.ContainsKey(md5Hash) == false)
                {
                    Md5HashNameDictionary.Add(md5Hash, line);
                }
            }
        }

        internal static string GetFileNameFromMd5Hash(byte[] md5Hash, string entryName)
        {
            string fileName;
            if (Md5HashNameDictionary.TryGetValue(md5Hash, out fileName) == false)
            {
                fileName = string.Format("{0}{1}", BitConverter.ToString(md5Hash).Replace("-", ""),
                    GetFileExtension(entryName));
            }
            return fileName;
        }

        private static string GetFileExtension(string entryName)
        {
            string extension = "";
            int index = entryName.LastIndexOf(".", StringComparison.Ordinal);
            if (index != -1)
            {
                extension = entryName.Substring(index, entryName.Length - index);
            }

            return extension;
        }
    }
}

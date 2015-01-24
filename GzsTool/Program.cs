using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using GzsTool.Fpk;
using GzsTool.Gzs;
using GzsTool.Utility;

namespace GzsTool
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Hashing.ReadPs3PathIdFile("pathid_list_ps3.bin");
            Hashing.ReadDictionary("dictionary.txt");
            Hashing.ReadMd5Dictionary("fpk_dict.txt");

            if (args.Length == 1)
            {
                string path = args[0];
                if (File.Exists(path))
                {
                    if (path.EndsWith(".g0s"))
                    {
                        ReadGzsArchive(path);
                        return;
                    }
                    if (path.EndsWith(".fpk") || path.EndsWith(".fpkd"))
                    {
                        ReadFpkArchive(path);
                        return;
                    }
                }
                else if (Directory.Exists(path))
                {
                    ReadFpkArchives(path);
                    return;
                }
            }
            ShowUsageInfo();
        }

        private static void ShowUsageInfo()
        {
            Console.WriteLine("GzsTool by Atvaark\n" +
                              "  A tool for unpacking g0s, fpk and fpkd files\n" +
                              "Usage:\n" +
                              "  GzsTool file_path|folder_path\n" +
                              "Examples:\n" +
                              "  GzsTool file_path.g0s  - Unpacks the g0s file\n" +
                              "  GzsTool file_path.fpk  - Unpacks the fpk file\n" +
                              "  GzsTool file_path.fpkd - Unpacks the fpkd file\n" +
                              "  GzsTool folder_path    - Unpacks all fpk and fpkd files in the folder");
        }

        private static void ReadGzsArchive(string path)
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string outputDirectory = Path.Combine(fileDirectory, fileNameWithoutExtension);
            string extension = Path.GetExtension(path).Replace(".", "");
            string xmlOutputPath = Path.Combine(fileDirectory,
                string.Format("{0}_{1}.xml", fileNameWithoutExtension, extension));


            using (FileStream input = new FileStream(path, FileMode.Open))
            using (FileStream xmlOutStream = new FileStream(xmlOutputPath, FileMode.Create))
            {
                GzsFile file = GzsFile.ReadGzsFile(input);
                file.Name = Path.GetFileName(path);
                file.ExportFiles(input, outputDirectory);
                XmlSerializer serializer = new XmlSerializer(typeof (GzsFile));
                serializer.Serialize(xmlOutStream, file);
            }
        }

        private static void ReadFpkArchives(string path)
        {
            var extensions = new List<string>
            {
                ".fpk",
                ".fpkd"
            };
            var files = GetFileList(new DirectoryInfo(path), true, extensions);
            foreach (var file in files)
            {
                ReadFpkArchive(file.FullName);
            }
        }

        private static void ReadFpkArchive(string path)
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path).Replace(".", "");
            string outputDirectory = string.Format("{0}\\{1}_{2}", fileDirectory, fileNameWithoutExtension, extension);
            string xmlOutputPath = Path.Combine(fileDirectory,
                string.Format("{0}_{1}.xml", fileNameWithoutExtension, extension));

            using (FileStream input = new FileStream(path, FileMode.Open))
            using (FileStream xmlOutput = new FileStream(xmlOutputPath, FileMode.Create))
            {
                FpkFile file = FpkFile.ReadFpkFile(input);
                file.Name = Path.GetFileName(path);
                file.ExportEntries(outputDirectory);
                XmlSerializer serializer = new XmlSerializer(typeof (FpkFile));
                serializer.Serialize(xmlOutput, file);
            }
        }

        private static List<FileInfo> GetFileList(DirectoryInfo fileDirectory, bool recursively, List<string> extensions)
        {
            List<FileInfo> files = new List<FileInfo>();
            if (recursively)
            {
                foreach (var directory in fileDirectory.GetDirectories())
                {
                    files.AddRange(GetFileList(directory, recursively, extensions));
                }
            }
            files.AddRange(
                fileDirectory.GetFiles()
                    .Where(f => extensions.Contains(f.Extension, StringComparer.CurrentCultureIgnoreCase)));
            return files;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using GzsTool.Common;
using GzsTool.Fpk;
using GzsTool.Gzs;
using GzsTool.Pftxs;
using GzsTool.Utility;

namespace GzsTool
{
    internal static class Program
    {
        private static readonly XmlSerializer ArchiveSerializer = new XmlSerializer(typeof (ArchiveFile),
            new[] {typeof (FpkFile), typeof (GzsFile), typeof (PftxsFile)});

        private static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                ReadDictionaries();
                string path = args[0];
                if (File.Exists(path))
                {
                    if (path.EndsWith(".g0s", StringComparison.CurrentCultureIgnoreCase))
                    {
                        ReadGzsArchive(path);
                        return;
                    }
                    if (path.EndsWith(".fpk", StringComparison.CurrentCultureIgnoreCase) ||
                        path.EndsWith(".fpkd", StringComparison.CurrentCultureIgnoreCase))
                    {
                        ReadFpkArchive(path);
                        return;
                    }
                    if (path.EndsWith(".pftxs", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ReadPftxsArchive(path);
                    }
                    if (path.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase))
                    {
                        WriteArchive(path);
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

        private static void ReadDictionaries()
        {
            string executingAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            const string gzsDictionaryName = "gzs_dictionary.txt";
            const string fpkDictionaryName = "fpk_dictionary.txt";
            // TODO: Enable reading the ps3 file when there is actually a need for it.
            ////Hashing.ReadPs3PathIdFile(Path.Combine(executingAssemblyLocation, "pathid_list_ps3.bin"));
            try
            {
                Console.WriteLine("Reading {0}", gzsDictionaryName);
                Hashing.ReadDictionary(Path.Combine(executingAssemblyLocation, gzsDictionaryName));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading {0}: {1}", gzsDictionaryName, e.Message);
            }
            try
            {
                Console.WriteLine("Reading {0}", fpkDictionaryName);
                Hashing.ReadMd5Dictionary(Path.Combine(executingAssemblyLocation, fpkDictionaryName));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading {0}: {1}", fpkDictionaryName, e.Message);
            }
        }

        private static void ShowUsageInfo()
        {
            Console.WriteLine("GzsTool by Atvaark\n" +
                              "  A tool for unpacking and repacking g0s, fpk, fpkd and pftxs files\n" +
                              "Usage:\n" +
                              "  GzsTool file_path|folder_path\n" +
                              "Examples:\n" +
                              "  GzsTool file_path.g0s      - Unpacks the g0s file\n" +
                              "  GzsTool file_path.fpk      - Unpacks the fpk file\n" +
                              "  GzsTool file_path.fpkd     - Unpacks the fpkd file\n" +
                              "  GzsTool file_path.pftxs    - Unpacks the pftxs file\n" +
                              "  GzsTool folder_path        - Unpacks all fpk and fpkd files in the folder\n" +
                              "  GzsTool file_path.g0s.xml  - Repacks the g0s file\n" +
                              "  GzsTool file_path.fpk.xml  - Repacks the fpk file\n" +
                              "  GzsTool file_path.fpkd.xml - Repacks the fpkd file\n" +
                              "  GzsTool file_path.pftxs.xml- Repacks the pftxs file");
        }

        private static void ReadGzsArchive(string path)
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string outputDirectory = Path.Combine(fileDirectory, fileNameWithoutExtension);
            string xmlOutputPath = Path.Combine(fileDirectory,
                string.Format("{0}.xml", Path.GetFileName(path)));

            using (FileStream input = new FileStream(path, FileMode.Open))
            using (FileStream xmlOutput = new FileStream(xmlOutputPath, FileMode.Create))
            {
                GzsFile gzsFile = GzsFile.ReadGzsFile(input);
                gzsFile.Name = Path.GetFileName(path);
                foreach (var exportedFile in gzsFile.ExportFiles(input))
                {
                    Console.WriteLine(exportedFile.FileName);
                    WriteExportedFile(exportedFile, outputDirectory);
                }
                ArchiveSerializer.Serialize(xmlOutput, gzsFile);
            }
        }

        private static void ReadFpkArchives(string path)
        {
            var extensions = new List<string>
            {
                ".fpk",
                ".fpkd"
            };
            var files = GetFilesWithExtension(new DirectoryInfo(path), extensions);
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
                string.Format("{0}.xml", Path.GetFileName(path)));

            using (FileStream input = new FileStream(path, FileMode.Open))
            using (FileStream xmlOutput = new FileStream(xmlOutputPath, FileMode.Create))
            {
                FpkFile fpkFile = FpkFile.ReadFpkFile(input);
                fpkFile.Name = Path.GetFileName(path);
                foreach (var exportedFile in fpkFile.ExportFiles(input))
                {
                    Console.WriteLine(exportedFile.FileName);
                    WriteExportedFile(exportedFile, outputDirectory);
                }
                ArchiveSerializer.Serialize(xmlOutput, fpkFile);
            }
        }

        private static void ReadPftxsArchive(string path)
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string outputDirectory = string.Format("{0}\\{1}_pftxs", fileDirectory, fileNameWithoutExtension);
            string xmlOutputPath = Path.Combine(fileDirectory,
                string.Format("{0}.xml", Path.GetFileName(path)));

            using (FileStream input = new FileStream(path, FileMode.Open))
            using (FileStream xmlOutput = new FileStream(xmlOutputPath, FileMode.Create))
            {
                PftxsFile pftxsFile = PftxsFile.ReadPftxsFile(input);
                pftxsFile.Name = Path.GetFileName(path);
                foreach (var exportedFile in pftxsFile.ExportFiles(input))
                {
                    Console.WriteLine(exportedFile.FileName);
                    WriteExportedFile(exportedFile, outputDirectory);
                }
                ArchiveSerializer.Serialize(xmlOutput, pftxsFile);
            }
        }

        private static void WriteExportedFile(FileDataStreamContainer fileDataStreamContainer, string outputDirectory)
        {
            string outputPath = Path.Combine(outputDirectory, fileDataStreamContainer.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            using (FileStream output = new FileStream(outputPath, FileMode.Create))
            {
                fileDataStreamContainer.DataStream.CopyTo(output);
            }
        }

        private static void WriteArchive(string path)
        {
            var directory = Path.GetDirectoryName(path);

            // HACK: Until PftxsLogFile is an ArchiveFile
            if (path.EndsWith(".pftxs.xml", StringComparison.InvariantCultureIgnoreCase))
            {
                PftxsUtility.PackPftxFile(path);
                return;
            }

            using (FileStream xmlInput = new FileStream(path, FileMode.Open))
            {
                object file = ArchiveSerializer.Deserialize(xmlInput);
                FpkFile fpkFile = file as FpkFile;
                if (fpkFile != null)
                {
                    WriteFpkArchive(fpkFile, directory);
                }
                GzsFile gzsFile = file as GzsFile;
                if (gzsFile != null)
                {
                    WriteGzsArchive(gzsFile, directory);
                }
            }
        }

        private static void WriteGzsArchive(GzsFile gzsFile, string workingDirectory)
        {
            string outputPath = Path.Combine(workingDirectory, gzsFile.Name);
            string fileSystemInputDirectory = Path.Combine(workingDirectory,
                Path.GetFileNameWithoutExtension(gzsFile.Name));
            AbstractDirectory inputDirectory = new FileSystemDirectory(fileSystemInputDirectory);


            using (FileStream output = new FileStream(outputPath, FileMode.Create))
            {
                gzsFile.Write(output, inputDirectory);
            }
        }

        private static void WriteFpkArchive(FpkFile fpkFile, string workingDirectory)
        {
            string outputPath = Path.Combine(workingDirectory, fpkFile.Name);
            string fileSystemInputDirectory = string.Format("{0}\\{1}_{2}", workingDirectory,
                Path.GetFileNameWithoutExtension(fpkFile.Name), Path.GetExtension(fpkFile.Name).Replace(".", ""));
            AbstractDirectory inputDirectory = new FileSystemDirectory(fileSystemInputDirectory);
            using (FileStream output = new FileStream(outputPath, FileMode.Create))
            {
                fpkFile.Write(output, inputDirectory);
            }
        }

        private static IEnumerable<FileInfo> GetFilesWithExtension(DirectoryInfo fileDirectory,
            ICollection<string> extensions)
        {
            foreach (var file in fileDirectory.GetFiles("*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(file.FullName);
                if (extensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
                    yield return file;
            }
        }
    }
}

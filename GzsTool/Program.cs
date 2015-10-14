using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using GzsTool.Core.Common;
using GzsTool.Core.Common.Interfaces;
using GzsTool.Core.Fpk;
using GzsTool.Core.Pftxs;
using GzsTool.Core.Qar;
using GzsTool.Core.Sbp;
using GzsTool.Core.Utility;

namespace GzsTool
{
    public static class Program
    {
        private static readonly XmlSerializer ArchiveSerializer = new XmlSerializer(
            typeof(ArchiveFile),
            new[] { typeof(FpkFile), typeof(PftxsFile), typeof(QarFile), typeof(SbpFile) });

        private static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                ReadDictionaries();
                string path = args[0];
                if (File.Exists(path))
                {
                    string extension = Path.GetExtension(path);
                    switch (extension)
                    {
                        case ".dat":
                            ReadArchive<QarFile>(path);
                            return;
                        case ".fpk":
                        case ".fpkd":
                            ReadArchive<FpkFile>(path);
                            return;
                        case ".pftxs":
                            ReadArchive<PftxsFile>(path);
                            return;
                        case ".sbp":
                            ReadArchive<SbpFile>(path);
                            return;
                        case ".xml":
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
            const string qarDictionaryName = "qar_dictionary.txt";
            try
            {
                Hashing.ReadDictionary(Path.Combine(executingAssemblyLocation, qarDictionaryName));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading {0}: {1}", qarDictionaryName, e.Message);
            }

            const string fpkDictionaryName = "fpk_dictionary.txt";
            try
            {
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
                              "  A tool for unpacking and repacking qar, fpk, fpkd, pftxs and sbp files\n" +
                              "Usage:\n" +
                              "  GzsTool file_path|folder_path\n" +
                              "Examples:\n" +
                              "  GzsTool file_path.dat      - Unpacks the qar file\n" +
                              "  GzsTool file_path.fpk      - Unpacks the fpk file\n" +
                              "  GzsTool file_path.fpkd     - Unpacks the fpkd file\n" +
                              "  GzsTool file_path.pftxs    - Unpacks the pftxs file\n" +
                              "  GzsTool file_path.sbp      - Unpacks the sbp file\n" +
                              "  GzsTool folder_path        - Unpacks all fpk and fpkd files in the folder\n" +
                              "  GzsTool file_path.dat.xml  - Repacks the qar file\n" +
                              "  GzsTool file_path.fpk.xml  - Repacks the fpk file\n" +
                              "  GzsTool file_path.fpkd.xml - Repacks the fpkd file\n" +
                              "  GzsTool file_path.pftxs.xml- Repacks the pftxs file\n" +
                              "  GzsTool file_path.sbp.xml  - Repacks the sbp file");
        }

        private static void ReadArchive<T>(string path) where T : ArchiveFile, new()
        {
            string fileDirectory = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path).Replace(".", "");
            string outputDirectoryPath = string.Format("{0}\\{1}_{2}", fileDirectory, fileNameWithoutExtension, extension);
            string xmlOutputPath = Path.Combine(fileDirectory,
                string.Format("{0}.xml", Path.GetFileName(path)));
            IDirectory outputDirectory = new FileSystemDirectory(outputDirectoryPath);

            using (FileStream input = new FileStream(path, FileMode.Open))
            using (FileStream xmlOutput = new FileStream(xmlOutputPath, FileMode.Create))
            {
                T file = new T();
                file.Name = Path.GetFileName(path);
                file.Read(input);
                foreach (var exportedFile in file.ExportFiles(input))
                {
                    Console.WriteLine(exportedFile.FileName);
                    outputDirectory.WriteFile(exportedFile.FileName, exportedFile.DataStream);
                }

                ArchiveSerializer.Serialize(xmlOutput, file);
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
                ReadArchive<FpkFile>(file.FullName);
            }
        }

        private static void WriteArchive(string path)
        {
            var directory = Path.GetDirectoryName(path);
            using (FileStream xmlInput = new FileStream(path, FileMode.Open))
            {
                ArchiveFile file = ArchiveSerializer.Deserialize(xmlInput) as ArchiveFile;
                if (file == null)
                {
                    Console.WriteLine("Error: Unknown archive type");
                    return;
                }

                WriteArchive(file, directory);
            }
        }

        private static void WriteArchive(ArchiveFile archiveFile, string workingDirectory)
        {
            string outputPath = Path.Combine(workingDirectory, archiveFile.Name);
            string fileSystemInputDirectory = string.Format("{0}\\{1}_{2}", workingDirectory,
                Path.GetFileNameWithoutExtension(archiveFile.Name), Path.GetExtension(archiveFile.Name).Replace(".", ""));
            IDirectory inputDirectory = new FileSystemDirectory(fileSystemInputDirectory);
            using (FileStream output = new FileStream(outputPath, FileMode.Create))
            {
                archiveFile.Write(output, inputDirectory);
            }
        }

        private static IEnumerable<FileInfo> GetFilesWithExtension(
            DirectoryInfo fileDirectory,
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

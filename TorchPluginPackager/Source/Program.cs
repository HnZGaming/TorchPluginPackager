using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;

namespace TorchPluginPackager
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var parseArgs = Parser.Default.ParseArguments<ProgramOptions>(args);
            parseArgs.WithNotParsed(o => OnArgumentsParseFailed(o));
            parseArgs.WithParsed(o => OnArgumentsParsed(o));
        }

        static void OnArgumentsParseFailed(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }
        }

        static void OnArgumentsParsed(ProgramOptions args)
        {
            var name = args.Name;
            var manifestFilePath = args.ManifestFilePath;
            var binDirPath = args.BuildPath;
            var refDirPaths = args.ReferencePaths;
            var outputDirPath = args.OutputDirPath; // can be null

            Console.WriteLine($"name: {name}");
            Console.WriteLine($"manifest: {manifestFilePath}");
            Console.WriteLine($"build: {binDirPath}");
            Console.WriteLine($"references: {string.Join(", ", refDirPaths)}");

            var refFilePaths = new HashSet<string>();
            foreach (var refDirPath in refDirPaths)
            foreach (var refFilePath in Directory.GetFiles(refDirPath, "*.dll"))
            {
                var refFileName = Path.GetFileName(refFilePath);
                refFilePaths.Add(refFileName);
            }

            var includedFilePaths = new List<string>();
            var binFilePaths = Directory.GetFiles(binDirPath, "*.dll");
            foreach (var binFilePath in binFilePaths)
            {
                var fileName = Path.GetFileName(binFilePath);
                var excluded = refFilePaths.Contains(fileName);
                if (!excluded)
                {
                    includedFilePaths.Add(binFilePath);
                }

                var includedChar = excluded ? 'x' : 'o';
                Console.WriteLine($"{includedChar} {fileName}");
            }
            
            includedFilePaths.Add(manifestFilePath);

            using (var outStream = new MemoryStream())
            using (var zipper = new ZipOutputStream(outStream))
            {
                zipper.UseZip64 = UseZip64.Off;
                var crc = new Crc32();

                foreach (var binFilePath in includedFilePaths)
                {
                    var file = File.ReadAllBytes(binFilePath);
                    var fileName = Path.GetFileName(binFilePath);

                    crc.Reset();
                    crc.Update(file);

                    var zipEntry = new ZipEntry(fileName)
                    {
                        DateTime = DateTime.Now,
                        Size = file.Length,
                        Crc = crc.Value,
                    };

                    zipper.PutNextEntry(zipEntry);
                    zipper.Write(file, 0, file.Length);
                }

                zipper.Finish();
                zipper.Flush();

                var zipBytes = outStream.ToArray();
                var outputFilePath = Path.Combine(outputDirPath, $"{name}.zip");
                File.WriteAllBytes(outputFilePath, zipBytes);
            }

            Console.WriteLine("Done");
        }
    }
}
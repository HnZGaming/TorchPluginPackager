using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;

namespace TorchPluginPackager
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ProgramOptions>(args)
                .WithNotParsed(o => OnArgumentsParseFailed(o))
                .WithParsed(o => OnArgumentsParsed(o));
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
            var diagnostics = new List<AssemblyDiagnostic>();

            var name = args.Name;
            var manifestFilePath = args.ManifestFilePath;
            var binDirPaths = args.BuildPaths.ToArray();
            var refDirPaths = args.ReferencePaths.ToArray();
            var outputDirPath = args.OutputDirPath; // can be null
            var exceptNames = new HashSet<string>(args.ExceptNames ?? new string[0]);

            Console.WriteLine($"name: {name}");
            Console.WriteLine($"manifest: {manifestFilePath}");
            Console.WriteLine($"builds: {string.Join(", ", binDirPaths)}");
            Console.WriteLine($"references: {string.Join(", ", refDirPaths)}");
            Console.WriteLine($"strict version: {args.StrictVersion}");

            var refFilePaths = new SetDictionary<string, Version>();
            foreach (var refDirPath in refDirPaths)
            foreach (var refFilePath in Directory.GetFiles(refDirPath, "*.dll"))
            {
                var refFileName = Path.GetFileName(refFilePath);
                try
                {
                    var assembly = AssemblyName.GetAssemblyName(refFilePath);
                    refFilePaths.Add(refFileName, assembly.Version);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to load reference dll '{refFileName}': {e.Message}");
                }
            }

            var includedFilePaths = new List<string>();
            var binFilePaths = binDirPaths.SelectMany(p => Directory.GetFiles(p, "*.dll"));
            foreach (var binFilePath in binFilePaths)
            {
                var fileName = Path.GetFileName(binFilePath);
                if (exceptNames.Contains(fileName))
                {
                    diagnostics.Add(new AssemblyDiagnostic
                    {
                        AssemblyName = fileName,
                        Included = false,
                    });

                    continue;
                }

                var refAssemblyVersions = new HashSet<Version>(refFilePaths.GetValues(fileName));
                if (refAssemblyVersions.Any())
                {
                    var assemblyName = AssemblyName.GetAssemblyName(binFilePath);
                    var binAssemblyVersion = assemblyName.Version;
                    var matchVersion = refAssemblyVersions.Contains(binAssemblyVersion);
                    var included = !matchVersion && args.StrictVersion;

                    if (included)
                    {
                        includedFilePaths.Add(binFilePath);
                        Console.WriteLine(
                            $"Found a reference DLL with the same name & different version: {assemblyName}, " +
                            $"[{string.Join(", ", refAssemblyVersions)}] -> {binAssemblyVersion}");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Found a reference DLL with the same name & same version: '{assemblyName.Name}', " +
                            $"[{string.Join(", ", refAssemblyVersions)}] -> {binAssemblyVersion}");
                    }

                    diagnostics.Add(new AssemblyDiagnostic
                    {
                        AssemblyName = fileName,
                        ReferenceExisted = true,
                        Included = included,
                        VersionMatch = matchVersion,
                        AssemblyVersion = binAssemblyVersion.ToString(),
                        ReferenceAssemblyVersion = binAssemblyVersion.ToString(),
                    });
                    continue;
                }

                includedFilePaths.Add(binFilePath);

                diagnostics.Add(new AssemblyDiagnostic
                {
                    AssemblyName = fileName,
                    ReferenceExisted = false,
                    Included = true,
                    AssemblyVersion = "",
                    ReferenceAssemblyVersion = "",
                });
            }

            includedFilePaths.Add(manifestFilePath);

            var mainAssemblyPath = Path.Combine(binDirPaths[0], $"{args.Name}.dll");
            var mainAssembly = AssemblyName.GetAssemblyName(mainAssemblyPath);
            var mainAssemblyVersion = $"v{mainAssembly.Version}";
            var manifestUpdater = new ManifestUpdater(manifestFilePath);
            manifestUpdater.SetVersion(mainAssemblyVersion);
            manifestUpdater.Write();

            foreach (var existingPluginFilePath in Directory.GetFiles(outputDirPath))
            {
                if (Path.GetFileName(existingPluginFilePath).StartsWith(name))
                {
                    File.Delete(existingPluginFilePath);
                }
            }

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
                var outputFilePath = Path.Combine(outputDirPath, $"{name}-{mainAssemblyVersion}.zip");
                File.WriteAllBytes(outputFilePath, zipBytes);
            }

            foreach (var d in AssemblyDiagnostic.Sort(diagnostics))
            {
                Console.WriteLine(d);
            }

            foreach (var includedFilePath in includedFilePaths)
            {
                var includedFileName = Path.GetFileName(includedFilePath);
                Console.WriteLine($"included: {includedFileName}");
            }

            Console.WriteLine("Done");
        }
    }
}
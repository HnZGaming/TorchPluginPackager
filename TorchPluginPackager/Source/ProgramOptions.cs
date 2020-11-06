using System.Collections.Generic;
using CommandLine;

namespace TorchPluginPackager
{
    public sealed class ProgramOptions
    {
        [Option('n', "name", HelpText = "Result Zip file name")]
        public string Name { get; set; }

        [Option('m', "manifest", Required = true, HelpText = "Plugin manifest file path")]
        public string ManifestFilePath { get; set; }

        [Option('b', "build", Required = true, HelpText = "Build directory path")]
        public string BuildPath { get; set; }

        [Option('r', "references", Required = true, HelpText = "Reference directory path(s)")]
        public IEnumerable<string> ReferencePaths { get; set; }

        [Option('o', "out", Required = true, HelpText = "Output directory (Plugins) path")]
        public string OutputDirPath { get; set; }

        [Option('s', "strict-version", Required = false, Default = true, HelpText = "Include name-conflicting DLLs with different versions")]
        public bool StrictVersion { get; set; }
        
        [Option('e', "except", Required = false, HelpText = "DLL names that should be excluded")]
        public IEnumerable<string> ExceptNames { get; set; }
    }
}
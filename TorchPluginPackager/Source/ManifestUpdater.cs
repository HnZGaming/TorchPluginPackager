using System.IO;
using System.Text.RegularExpressions;

namespace TorchPluginPackager
{
    public sealed class ManifestUpdater
    {
        readonly string _filePath;
        string _text;

        public ManifestUpdater(string filePath)
        {
            _filePath = filePath;
            _text = File.ReadAllText(filePath);
        }

        public void SetVersion(string version)
        {
            var regex = new Regex("<Version>.+?</Version>");
            _text = regex.Replace(_text, $"<Version>{version}</Version>");
        }

        public void Write()
        {
            File.WriteAllText(_filePath, _text);
        }
    }
}
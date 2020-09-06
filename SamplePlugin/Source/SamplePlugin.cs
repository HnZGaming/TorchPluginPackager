using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using NLog;
using Torch;
using Torch.API;

namespace SamplePlugin
{
    public class SamplePlugin : TorchPluginBase
    {
        readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            _logger.Info("Sample Plugin imported");

            var messageOriginal = "foo";
            var messageZipped = Zip(messageOriginal);
            var messageUnzipped = Unzip(messageZipped);
            var zipSuccess = messageOriginal == messageUnzipped;
            _logger.Info($"SharpZipLib.dll success: {zipSuccess}");
        }

        static byte[] Zip(string message)
        {
            var rawBytes = Encoding.UTF8.GetBytes(message);
            using (var inStream = new MemoryStream(rawBytes))
            using (var zipper = new ZipOutputStream(inStream))
            {
                zipper.Write(rawBytes, 0, rawBytes.Length);
                zipper.Finish();
                zipper.Flush();
                return inStream.ToArray();
            }
        }

        static string Unzip(byte[] bytes)
        {
            using (var outStream = new MemoryStream(bytes))
            using (var unZipper = new ZipInputStream(outStream))
            {
                unZipper.Write(bytes, 0, bytes.Length);
                unZipper.Flush();
                var unzipped = outStream.ToArray();
                return Encoding.UTF8.GetString(unzipped);
            }
        }
    }
}
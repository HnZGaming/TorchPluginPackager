using System.Collections.Generic;
using System.Linq;

namespace TorchPluginPackager
{
    internal struct AssemblyDiagnostic
    {
        public string AssemblyName { get; set; }
        public bool Included { get; set; }
        public bool ReferenceExisted { get; set; }
        public bool VersionMatch { get; set; }
        public string AssemblyVersion { get; set; }
        public string ReferenceAssemblyVersion { get; set; }

        public override string ToString()
        {
            if (!Included)
            {
                return $"x {AssemblyName}";
            }

            if (!ReferenceExisted)
            {
                return $"o {AssemblyName} ({AssemblyVersion})";
            }

            if (VersionMatch)
            {
                return $"o {AssemblyName} ({AssemblyVersion})";
            }

            return $"o {AssemblyName} ({ReferenceAssemblyVersion} -> {AssemblyVersion})";
        }

        public static IEnumerable<AssemblyDiagnostic> Sort(IEnumerable<AssemblyDiagnostic> self)
        {
            return self.Where(d => !d.Included).Concat(
                self.Where(d => d.Included && d.VersionMatch)).Concat(
                self.Where(d => d.Included && !d.VersionMatch));
        }
    }
}
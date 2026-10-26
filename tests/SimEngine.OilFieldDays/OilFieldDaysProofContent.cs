using System.IO;
using Beep.SimEngine.ExtensionProof;
namespace Beep.SimEngine.Tests
{
    internal static class OilFieldDaysProofContent
    {
        internal static string Read(string file)
        {
            using (var stream = typeof(OilFieldDaysProofContent).Assembly.GetManifestResourceStream("proof." + file))
            {
                if (stream == null) return FrameworkProofContent.Read(file);
                using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
            }
        }
    }
}

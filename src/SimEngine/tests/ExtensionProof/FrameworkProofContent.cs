using System;
using System.IO;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Explicit authored proof file; no engine content defaults or automatic registrations.</summary>
    public static class FrameworkProofContent
    {
        public static string Access => Read("open-access.json");
        public static string Read(string file)
        {
            using (Stream stream = typeof(FrameworkProofContent).Assembly.GetManifestResourceStream("proof." + file)
                ?? throw new InvalidOperationException("missing authored proof." + file))
            using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
        }
    }
}

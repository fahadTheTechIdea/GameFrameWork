using System;
using System.IO;

namespace Beep.SimEngine.Hosting
{
    /// <summary>
    /// Content files from a folder on disk (desktop hosts, servers, tools). Paths may not escape the
    /// root. Platforms with their own file systems supply their own source or a <see cref="PreloadedFileSource"/>.
    /// </summary>
    public sealed class DiskFileSource : IFileSource
    {
        private readonly string _root;

        public DiskFileSource(string root) { _root = Path.GetFullPath(root); }

        public string Root => _root;
        public bool Exists(string path) => File.Exists(Resolve(path));
        public string ReadText(string path) => File.ReadAllText(Resolve(path));

        private string Resolve(string path)
        {
            string full = Path.GetFullPath(Path.Combine(_root, (path ?? "").Replace('/', Path.DirectorySeparatorChar)));
            string prefix = _root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? _root : _root + Path.DirectorySeparatorChar;
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new UnauthorizedAccessException("path escapes the content root: " + path);
            return full;
        }

        /// <summary>
        /// Finds a content folder (one holding manifest.json) named <paramref name="folder"/> in
        /// <paramref name="start"/> or any folder above it. Null when there is none.
        /// </summary>
        public static string FindUpwards(string start, string folder = "content")
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, folder);
                if (File.Exists(Path.Combine(candidate, "manifest.json"))) return candidate;
                dir = dir.Parent;
            }
            return null;
        }
    }
}

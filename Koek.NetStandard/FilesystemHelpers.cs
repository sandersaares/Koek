﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Koek
{
    public static partial class NetStandardHelpers
    {
        /// <summary>
        /// Resolves a relative path, using all the paths in the PATH environment variable as potential roots.
        /// May be extended in the future to also use other potential roots for candidate paths.
        /// </summary>
        public static string ResolvePath(this HelpersContainerClasses.Filesystem container, string inputPath)
        {
            Helpers.Argument.ValidateIsNotNullOrWhitespace(inputPath, "inputPath");

            if (Path.IsPathRooted(inputPath))
                return inputPath;

            List<string> roots = new List<string>();
            roots.AddRange((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator));

            // The current directory is a valid root path, as well.
            roots.Add(Environment.CurrentDirectory);

            foreach (string root in roots)
            {
                string candidate = Path.Combine(root, inputPath);

                if (File.Exists(candidate))
                    return candidate;
            }

            throw new ArgumentException("Unable to resolve path to " + inputPath, nameof(inputPath));
        }

        /// <summary>
        /// Ensures that an empty directory exists at the specified path, emptying or creating it as needed.
        /// </summary>
        public static void EnsureEmptyDirectory(this HelpersContainerClasses.Filesystem container, string path)
        {
            Helpers.Argument.ValidateIsNotNullOrWhitespace(path, nameof(path));

            if (Directory.Exists(path))
                ClearDirectory(path);
            else
                Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Recursively deletes all files and subfolders from a folder.
        /// </summary>
        private static void ClearDirectory(string path)
        {
            Helpers.Argument.ValidateIsNotNullOrWhitespace(path, nameof(path));

            DirectoryInfo target = new DirectoryInfo(path);

            foreach (var file in target.GetFiles())
            {
                file.IsReadOnly = false;
                file.Delete();
            }

            foreach (var directory in target.GetDirectories())
            {
                ClearDirectory(directory.FullName);
                directory.Delete();
            }
        }

        /// <summary>
        /// Copies files from one directory to another.
        /// </summary>
        public static void CopyFiles(this HelpersContainerClasses.Filesystem container, string from, string to, string searchPattern = "*", bool recursive = false, bool overwrite = false)
        {
            Helpers.Argument.ValidateIsNotNullOrWhitespace(from, nameof(from));
            Helpers.Argument.ValidateIsNotNullOrWhitespace(to, nameof(to));

            var directorySeparatorChars = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

            if (!directorySeparatorChars.Contains(from.Last()))
                from += Path.DirectorySeparatorChar;

            if (!directorySeparatorChars.Contains(to.Last()))
                to += Path.DirectorySeparatorChar;

            if (string.IsNullOrWhiteSpace(searchPattern))
                searchPattern = "*";

            if (!Directory.Exists(from))
                throw new ArgumentException("The directory to copy files from does not exist.", nameof(from));

            var files = Directory.GetFiles(from, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                // Replace the root with the new root.
                if (!file.StartsWith(from))
                    throw new InvalidOperationException(string.Format("Copying files ran into a file that came from a strange place. {0} is not under the root {1}.", file, from));

                var newPath = file.Replace(from, to);

                // Ensure that the directory exists.
                // Directory cannot be null, since that would mean it's not rooted, which is impossible because we got it from GetFiles().
                Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);

                File.Copy(file, newPath, overwrite);
            }
        }

        /// <summary>
        /// Removes a root from a path, making it relative to the root.
        /// 
        /// E.g. /a/b/c with a root of /a will become b/c
        /// </summary>
        public static string RemoveRoot(this HelpersContainerClasses.Filesystem container, string path, string root)
        {
            Helpers.Argument.ValidateIsNotNullOrWhitespace(path, nameof(path));
            Helpers.Argument.ValidateIsNotNullOrWhitespace(root, nameof(root));

            if (!Path.IsPathRooted(path))
                throw new ArgumentException("Path must be rooted.", nameof(path));

            if (!Path.IsPathRooted(root))
                throw new ArgumentException("Root path must be rooted.", nameof(root));

            if (!path.StartsWith(root))
                throw new ArgumentException("Path does not start with item root path.", nameof(path));

            var relativePath = path.Substring(root.Length);
            relativePath = relativePath.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return relativePath;
        }

        /// <summary>
        /// Appends or prepends some text to the filename if needed in order to make the provided path unique.
        /// </summary>
        public static string MakeUnique(this HelpersContainerClasses.Filesystem container, string path)
        {
            Helpers.Argument.ValidateIsNotNullOrEmpty(path, nameof(path));

            if (!File.Exists(path))
                return path;

            // Append a GUID. Good enough for easy uniqueness.
            var filename = Path.GetFileNameWithoutExtension(path) + "-" + Guid.NewGuid() + Path.GetExtension(path);

            return Path.Combine(Path.GetDirectoryName(path)!, filename);
        }

        /// <summary>
        /// On non-Microsoft operating systems, grants execute permissions to the file at the specified path.
        /// On Microsoft operating systems, does nothing.
        /// </summary>
        /// <remarks>
        /// Permissions are granted to everyone. Do it manually if you wish to be more restrictive.
        /// </remarks>
        public static void EnsureExecutePermission(this HelpersContainerClasses.Filesystem container, string path)
        {
            if (!Helpers.Environment.IsNonMicrosoftOperatingSystem())
                return;

            // End result of all that escaping: sh -c "chmod +x \"/some/nice file\""
            ExternalTool.Execute("sh", string.Format("-c \"chmod +x \\\"{0}\\\"\"", path), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Gets the directory containing the current application's entry point binary.
        /// </summary>
        public static string GetBinDirectory(this HelpersContainerClasses.Filesystem container)
        {
            // If we have one, we want the entry point assembly as this one will be in the right location.
            var assembly = Assembly.GetEntryAssembly();

            // However, web projects do not have an entry assembly, so pick whatever is left.
            if (assembly == null)
                assembly = Assembly.GetExecutingAssembly();

            // Maybe some types of projects still get it wrong here but we'll see when we get to it.

#pragma warning disable SYSLIB0012 // Type or member is obsolete
            return Path.GetDirectoryName(new Uri(assembly.CodeBase!).LocalPath)!;
#pragma warning restore SYSLIB0012 // Type or member is obsolete
        }
    }
}
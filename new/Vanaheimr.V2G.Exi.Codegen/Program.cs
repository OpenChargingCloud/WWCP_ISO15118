/*
 * Copyright (c) 2021-2025 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP ISO/IEC 15118 <https://github.com/OpenChargingCloud/WWCP_ISO15118>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Emit;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Grammar;
using cloud.charging.open.protocols.ISO15118.EXI.SourceGenerator.Xsd;

namespace cloud.charging.open.protocols.ISO15118.EXI.Codegen
{
    /// <summary>
    /// Roslyn-free driver for the EXI codec generator.
    ///
    /// <para>
    /// The incremental generator can only contribute C# to the compilation it runs in, so every
    /// other target language needs a driver that runs the same front end
    /// (<see cref="XsdReader"/> → <see cref="GrammarBuilder"/>) and writes the chosen
    /// <see cref="ICodecEmitter"/>'s output to disk. This is that driver.
    /// </para>
    ///
    /// <para>
    /// It also serves as a differential test of the seam: emitting <c>--lang csharp</c> for a
    /// schema set must reproduce, byte for byte, what the Roslyn generator puts in
    /// <c>obj/.../generated/</c> for the same inputs.
    /// </para>
    /// </summary>
    public static class Program
    {
        private static readonly ICodecEmitter[] Emitters =
        [
            CSharpCodecEmitter.Instance,
            KotlinCodecEmitter.Instance,
            SwiftCodecEmitter.Instance,
            TypeScriptCodecEmitter.Instance,
        ];

        public static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (UsageException ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                Console.Error.WriteLine();
                Usage();
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("error: " + ex.Message);
                return 1;
            }
        }

        private static int Run(string[] args)
        {
            var opts = Options.Parse(args);

            var emitter = Emitters.FirstOrDefault(e =>
                              string.Equals(e.Language, opts.Language, StringComparison.OrdinalIgnoreCase))
                          ?? throw new UsageException(
                                 $"unknown --lang '{opts.Language}'. Known: " +
                                 string.Join(", ", Emitters.Select(e => e.Language)));

            var schema = XsdReader.ParseSet(opts.XsdPaths.Select(File.ReadAllText));
            var plan   = GrammarBuilder.Build(schema, opts.Fragments);
            var files  = emitter.Emit(plan, opts.Namespace, opts.CodecClass);

            // --out is the directory the files go in. Every back end emits one file per type, so a
            // path that looks like a file is a leftover from the single-file era; quietly treating
            // it as a directory would create one called `Iso15118_2Codec.kt` holding a hundred
            // files — the shape of the old invocation with none of its meaning. Say so instead.
            //
            // "Looks like a file" is decided by the path itself, never by whether the directory
            // happens to exist yet: the old rule wrote a *file* named after the directory when that
            // directory had not been created, which is silent and easy to miss.
            if (!Directory.Exists(opts.Output) && Path.GetExtension(opts.Output).Length > 0)
                throw new UsageException(
                    $"--out '{opts.Output}' names a file, but the {emitter.Language} back end emits " +
                    $"{files.Count} files. Pass the directory they belong in.");

            Directory.CreateDirectory(opts.Output);

            var removed = RemoveStaleOutput(opts.Output, emitter.FileExtension,
                                            new HashSet<string>(files.Select(f => f.FileName),
                                                                StringComparer.OrdinalIgnoreCase));

            var total = 0;
            foreach (var file in files)
            {
                Write(Path.Combine(opts.Output, file.FileName), file.Source);
                total += file.Source.Length;
            }

            Console.WriteLine($"{emitter.Language}: {opts.Output} — {files.Count} file(s), " +
                              $"{total:N0} chars" + (removed > 0 ? $", {removed} stale removed" : ""));
            return 0;
        }

        /// <summary>UTF-8 without BOM, line endings exactly as the emitter produced them.</summary>
        private static void Write(string path, string source) =>
            File.WriteAllText(path, source, new System.Text.UTF8Encoding(false));

        /// <summary>
        /// Deletes generated files left over from an earlier run that this one no longer produces —
        /// a type that was renamed or dropped would otherwise keep its file, and in Kotlin a stale
        /// declaration next to its replacement is a duplicate-declaration error at best and a
        /// silently outdated codec at worst.
        /// </summary>
        /// <remarks>
        /// Only files this generator wrote are candidates, identified by their first line. Nothing
        /// hand-written is touched, whatever it is called — the output directory is a package
        /// directory that also holds hand-written sources.
        /// </remarks>
        private static int RemoveStaleOutput(string directory, string extension, HashSet<string> keep)
        {
            var removed = 0;

            foreach (var path in Directory.GetFiles(directory, "*" + extension))
            {
                if (keep.Contains(Path.GetFileName(path)))
                    continue;

                using (var reader = new StreamReader(path))
                    if (reader.ReadLine() != "// <auto-generated/>")
                        continue;

                File.Delete(path);
                removed++;
            }

            return removed;
        }

        private static void Usage()
        {
            Console.Error.WriteLine("""
                Usage:
                  cloud.charging.open.protocols.ISO15118.EXI.Codegen --xsd <file>[;<file>...] --out <path>
                                            [--lang csharp|kotlin|swift]
                                            [--namespace <ns>] [--codec <class>]
                                            [--fragments <Elem>[,<Elem>...]]

                  --xsd        One or more XSD files forming ONE schema set (types resolve across
                               the whole set). Repeatable, or ';'-separated.
                  --out        Output DIRECTORY, created if needed, holding one file per generated
                               type; stale files from earlier runs are removed. A path with an
                               extension is rejected — every back end splits its output.
                  --lang       Target language. Default: csharp.
                  --namespace  Generated namespace / package.
                  --codec      Generated codec class name.
                  --fragments  Global elements to emit EXI fragment codecs for (XMLDSig).
                """);
        }

        private sealed class UsageException(string message) : Exception(message);

        private sealed record Options(
            IReadOnlyList<string> XsdPaths,
            string                Output,
            string                Language,
            string                Namespace,
            string                CodecClass,
            string[]              Fragments)
        {
            public static Options Parse(string[] args)
            {
                var xsds       = new List<string>();
                string? output = null, lang = null, ns = null, codec = null;
                var fragments  = Array.Empty<string>();

                for (var i = 0; i < args.Length; i++)
                {
                    string Next(string flag) =>
                        i + 1 < args.Length ? args[++i] : throw new UsageException($"{flag} needs a value.");

                    switch (args[i])
                    {
                        case "--xsd":
                            xsds.AddRange(Next("--xsd").Split([';'], StringSplitOptions.RemoveEmptyEntries));
                            break;
                        case "--out":       output    = Next("--out");       break;
                        case "--lang":      lang      = Next("--lang");      break;
                        case "--namespace": ns        = Next("--namespace"); break;
                        case "--codec":     codec     = Next("--codec");     break;
                        case "--fragments":
                            fragments = Next("--fragments")
                                        .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
                            break;
                        default:
                            throw new UsageException($"unexpected argument '{args[i]}'.");
                    }
                }

                if (xsds.Count == 0) throw new UsageException("--xsd is required.");
                if (output is null)  throw new UsageException("--out is required.");

                foreach (var p in xsds)
                    if (!File.Exists(p))
                        throw new UsageException($"XSD not found: {p}");

                return new Options(
                    xsds,
                    output,
                    lang  ?? "csharp",
                    ns    ?? "Generated",
                    codec ?? "Codec",
                    fragments);
            }
        }
    }
}

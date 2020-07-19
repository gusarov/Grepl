using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Grepl.Model;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Grepl
{
	using static Tools;

	class OutputControlOptions
	{
		/// <summary>
		/// L
		/// file names only, no content
		/// </summary>
		public bool FilesWithMatches { get; set; }
	}

	class FilesSelectorOptions
	{
		// public bool BinAsText { get; set; }
		public bool Recursive { get; set; }
		public List<string> IncludeGlobs { get; } = new List<string>();
		public List<string> ExcludeGlobs { get; } = new List<string>();
	}

	public class Executor
	{
		internal OutputControlOptions OutputControlOptions { get; } = new OutputControlOptions();
		internal FilesSelectorOptions FilesSelectorOptions { get; } = new FilesSelectorOptions();
		private bool Recursive => FilesSelectorOptions.Recursive;

		private const int FileBufferSize = 16 * 1024;

		public string ReplaceTo { get; set; }

		public string Dir { get; set; }

		public List<string> Files => FilesSelectorOptions.IncludeGlobs;
		public List<string> Patterns { get; } = new List<string>();

		public bool Save { get; set; }
		public bool GroupMatchesByContext { get; set; }

		private readonly List<Regex> _regexes = new List<Regex>();

		public void Execute()
		{
			if (Shared.Instance.Debug)
			{
				using (Color(ConsoleColor.DarkGray))
				{
					Console.WriteLine("Patterns:");
					foreach (var pattern in Patterns)
					{
						Console.WriteLine(pattern);
					}
				}
			}

			// Console.WriteLine("Replace to " + ReplaceTo);

			foreach (var pattern in Patterns)
			{
				var pattern2 = ImprovePattern(pattern);
				_regexes.Add(new Regex(pattern2, RegexOptions.Compiled | RegexOptions.Multiline));
			}

			if (string.IsNullOrWhiteSpace(Dir))
			{
				throw new Exception("Directory not defined");
			}

			if (!Directory.Exists(Dir))
			{
				throw new Exception("Directory does not exists");
			}

			if (Files.Count == 0)
			{
				if (Recursive)
				{
					Files.Add("**/*");
				}
				else
				{
					// STDIN
					Files.Add("-");
				}
			}

			var groupedContext = GroupMatchesByContext
				? new Dictionary<ColoredMessage, CaptureResult>()
				: null;


			if (Files.Count == 1 && (File.Exists(Files[0]) || Files[0] == "-"))
			{
				// single
				Process(file: Files[0], printFileName: false);
			}
			else
			{
				var di = new DirectoryInfo(Dir);

				var includes = Files
						.Select(x => new
						{
							IsFullPath = Path.GetFullPath(x) == x,
							Pattern = x,
						})
						.Select(x =>
							x.IsFullPath
								? x.Pattern.Substring(di.FullName.Length + 1)
								: (Recursive
									? (x.Pattern.StartsWith("**/") ? x.Pattern : "**/" + x.Pattern)
									: x.Pattern)
						)
					;

				var excludes = FilesSelectorOptions.ExcludeGlobs
						.Select(x => new
						{
							IsFullPath = Path.GetFullPath(x) == x,
							Pattern = x,
						})
						.Select(x =>
							x.IsFullPath
								? x.Pattern.Substring(di.FullName.Length + 1)
								: x.Pattern
						)
					;

				var matcher = new Matcher();
				matcher.AddIncludePatterns(includes);
				matcher.AddExcludePatterns(excludes);

				var res = matcher.Execute(new DirectoryInfoWrapper(di));
				foreach (var filePatternMatch in res.Files.OrderBy(x => x.Path))
				{
					var file = filePatternMatch.Path;
					if (Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar)
					{
						file = file.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
					}
					Process(file, printFileName: true, groupedContext);
				}
			}

			if (groupedContext != null)
			{
				foreach (var kvp in groupedContext.OrderBy(x=>x.Value.FileNames.First()))
				{
					Console.WriteLine();
					if (kvp.Value.FileNames.Count > 1)
					{
						using (Color(ConsoleColor.Magenta))
						{
							Console.WriteLine($"{kvp.Value.FileNames.Count} files:");
						}
					}
					
					foreach (var file in kvp.Value.FileNames)
					{
						PrintFileName(file);
					}

					kvp.Key.ToConsole();
				}
				/*
				if (groupedContext.Count > 1)
				{
					// using (Color(ConsoleColor.Gray))
					{
						Console.WriteLine();
						Console.WriteLine($"{groupedContext.Count} groups of similar context");
					}
				}
				*/
			}
		}

		private string ImprovePattern(string pattern)
		{
			if (pattern.Length >=2 && pattern[^1] == '$' && pattern[^2] != '\\')
			{
				// $ should match CR LF if any, not just LF
				return pattern.Substring(0, pattern.Length - 1) + @"(?=\r?\n|$)";
			}

			return pattern;
		}

		void PrintFileName(string file)
		{
			if (!GroupMatchesByContext && !OutputControlOptions.FilesWithMatches)
			{
				Console.WriteLine();
			}

			var fileName = Path.GetFileName(file);
			var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
			var ext = fileName.Substring(fileNameWithoutExt.Length);

			var path = file.Substring(0, file.Length - fileName.Length);

			using (Color(ConsoleColor.DarkMagenta))
			{
				Console.Write(path);
				using (Color(ConsoleColor.Magenta))
				{
					Console.WriteLine(fileName);
					// Output.Write(fileNameWithoutExt);
				}
				// Output.WriteLine(ext);
			}
		}

		class Line
		{
			public int Start; // not including cr lf
			// public int End; // not including cr lf, if the line feed at the end - mean there is 1 extra empty line

			public SortedDictionary<int, LineMatch> Matches = new SortedDictionary<int, LineMatch>();
		}

		class LineMatch
		{
			public LineMatch(Regex regex, Match match)
			{
				Regex = regex;
				Match = match;
			}

			public Regex Regex { get; }
			public Match Match { get; }
		}

		void Process(string file, bool printFileName, Dictionary<ColoredMessage, CaptureResult> ctxGroups = null)
		{
			var output = new ColoredMessage();

			var body = ReadAllText(file, out var encoding);

			var matchLines = new SortedDictionary<int, Line>();

			var replaced = body;
			foreach (var regex in _regexes)
			{
				var matches = regex.Matches(body);

				// var currentlnStart = -2; // initially unknown
				foreach (Match match in matches)
				{
					// find the start of the line
					var lineStart = body.LastIndexOf('\n', match.Index);
					lineStart++;
					if (!matchLines.TryGetValue(lineStart, out var line))
					{
						matchLines[lineStart] = line = new Line
						{
							Start = lineStart,
						};
					}

					line.Matches.Add(match.Index, new LineMatch(regex, match));
				}

				if (ReplaceTo != null)
				{
					replaced = regex.Replace(replaced, ReplaceTo);
					// var rr = regex.ReplaceBreakout(replaced, ReplaceTo);
				}

			}

			if (ctxGroups == null && printFileName && matchLines.Any())
			{
				PrintFileName(file);
			}

			if (!OutputControlOptions.FilesWithMatches)
			{

				foreach (var line in matchLines.Values)
				{
					var eol = body.IndexOf('\n', line.Start);
					if (eol == -1)
					{
						eol = body.Length - 1; // jump to last char
					}
					else
					{
						eol--; // jump to char before \n
					}

					// Windows CR LF file
					if (body[eol] == '\r')
					{
						eol--; // jump one more time
					}

					// var str = body.Substring(line.Key, eol);
					void PrintLine(bool replaceMode = false)
					{
						var printPosition = line.Start;
						foreach (var lineMatch in line.Matches.Values)
						{
							var match = lineMatch.Match;

							if (match.Index > printPosition)
							{
								output.Write(ConsoleColor.Gray,
									body.Substring(printPosition, match.Index - printPosition));
							}

							if (!replaceMode)
							{
								output.Write(ConsoleColor.Red, match.Value);
							}
							else
							{
								output.Write(ConsoleColor.Green,
									GetReplacementString(lineMatch.Regex, match, body, ReplaceTo));
							}

							printPosition = match.Index + match.Length;
						}

						var len = eol + 1 - printPosition;
						if (len >= 0 && printPosition < body.Length && (len - printPosition) < body.Length)
						{
							output.Write(ConsoleColor.Gray, body.Substring(printPosition, len) + Environment.NewLine);
						}
						else
						{
							output.Write(null, Environment.NewLine);
						}
					}

					PrintLine();
					if (ReplaceTo != null)
					{
						PrintLine(true);
					}
				}
			}

			if (Save && matchLines.Any() && body != replaced)
			{
				// open existing file for replace!
				using var fileStream = new FileStream(file,
					FileMode.Open,
					FileAccess.Write,
					FileShare.Read,
					FileBufferSize,
					FileOptions.SequentialScan);

				using var sw = new StreamWriter(fileStream, encoding);
				sw.Write(replaced);
				sw.Flush();
				fileStream.SetLength(fileStream.Position); // truncate!
			}

			if (ctxGroups != null)
			{
				// context grouping... will print later
				if (output.Parts.Count > 0)
				{
					if (!ctxGroups.TryGetValue(output, out var capture))
					{
						ctxGroups[output] = capture = new CaptureResult();
					}
					capture.FileNames.Add(file);
				}
			}
			else
			{
				// no context grouping, print directly now
				if (!OutputControlOptions.FilesWithMatches)
				{
					output.ToConsole();
				}
			}
		}

		private static string GetReplacementString(Regex regex, Match match, string input, string replacement)
		{
			if (replacement.Contains('$'))
			{
				try
				{
					var rep = ReplacementBreakout.ReplaceBreakout(regex, match, input, replacement);
					return rep;
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
					return "{preview_not_available_yet_but_you_can_save}";
				}
			}
			else
			{
				return replacement;
			}
		}

		private static string ReadAllText(string file, out Encoding encoding)
		{
			if (file == "-")
			{
				var res = System.Console.In.ReadToEnd();
				encoding = System.Console.InputEncoding;
				return res;
			}

			// StreamReader(detectEncodingFromByteOrderMarks) have a bug.
			// When UTF8 NO Bom file is passed, detected encoding have a preamble

			string body;
			using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, FileBufferSize, FileOptions.SequentialScan);
			var buf = new byte[3];
			var len = fs.Read(buf, 0,3);
			fs.Position = 0;
			using var sr = new StreamReader(file);
			body = sr.ReadToEnd();
			encoding = sr.CurrentEncoding;

			if (Equals(encoding, Encoding.UTF8) && !(buf[0] == 0xEF && buf[1] == 0xBB && buf[2] == 0xBF))
			{
				encoding = new UTF8Encoding(false);
			}
			return body;
		}
	}
}

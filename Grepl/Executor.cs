using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Grepl
{
	using static Tools;

	public class Executor
	{
		private const int FileBufferSize = 16 * 1024;

		public string ReplaceTo { get; set; }

		public string Dir { get; set; }
		public List<string> Files { get; } = new List<string>();

		public List<string> Patterns { get; } = new List<string>();
		public bool Recursive { get; set; }
		public bool Save { get; set; }

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

			if (Files.Count == 0 && Recursive)
			{
				if (Recursive)
				{
					Files.Add("*.*");
				}
				else
				{
					// STDIN
					Files.Add("-");
				}
			}

			foreach (var filePattern in Files)
			{
				if (filePattern.Contains('*') || filePattern.Contains('?'))
				{
					foreach (var file in Directory.GetFiles(Dir, filePattern, Recursive
						? SearchOption.AllDirectories
						: SearchOption.TopDirectoryOnly).OrderBy(x => x))
					{
						var subPath = file.Substring(Dir.Length + 1);
						Process(subPath, printFileName: true);
					}
				}
				else
				{
					Process(file: filePattern, printFileName: false);
				}
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
			Console.WriteLine();

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

		void Process(string file, bool printFileName)
		{
			if (file == "-")
			{
				throw new NotImplementedException("STDIN is not implemented yet");
			}

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

			if (printFileName && matchLines.Any())
			{
				PrintFileName(file);
			}

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
				void print(bool replaced = false)
				{
					var printPosition = line.Start;
					foreach (var lineMatch in line.Matches.Values)
					{
						var match = lineMatch.Match;

						if (match.Index > printPosition)
						{
							using (Color(ConsoleColor.Gray))
							{
								Console.Write(body.Substring(printPosition, match.Index - printPosition));
							}
						}

						if (!replaced)
						{

							using (Color(ConsoleColor.Red))
							{
								Console.Write(match.Value);
							}
						}
						else
						{
							using (Color(ConsoleColor.Green))
							{
								var str = GetReplacementString(lineMatch.Regex, match, body, ReplaceTo);
								Console.Write(str);
							}
						}

						printPosition = match.Index + match.Length;
					}

					var len = eol + 1 - printPosition;
					if (len >= 0 && printPosition < body.Length && (len - printPosition) < body.Length)
					{
						using (Color(ConsoleColor.Gray))
						{
							Console.WriteLine(body.Substring(printPosition, len));
						}
					}
					else
					{
						Console.WriteLine();
					}
				}

				print();
				if (ReplaceTo != null)
				{
					print(true);
				}
			}

			if (Save && matchLines.Any())
			{
				// open existing file for replace!
				using var fileStream = new FileStream(file,
					FileMode.Open,
					FileAccess.Write,
					FileShare.Read,
					FileBufferSize,
					FileOptions.SequentialScan);

#if DEBUG
				Debug.WriteLine(encoding);
				if (encoding is UTF8Encoding u8)
				{
					Debug.WriteLine(string.Join(" ", u8.Preamble
						.ToArray().Select(x=>x.ToString("X2"))));
				}
				else if (encoding is UnicodeEncoding u16)
				{
					Debug.WriteLine(string.Join(" ", u16.Preamble
						.ToArray().Select(x => x.ToString("X2"))));
				}
#endif

				using var sw = new StreamWriter(fileStream, encoding); // todo preserve ENCODING!!
				sw.Write(replaced);
				sw.Flush();
				fileStream.SetLength(fileStream.Position); // truncate!
				// File.WriteAllText(file, replaced);
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

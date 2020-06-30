using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

			var groupedContext = GroupMatchesByContext
				? new Dictionary<string, CaptureResult>()
				: null;

			foreach (var filePattern in Files)
			{
				if (filePattern.Contains('*') || filePattern.Contains('?'))
				{
					foreach (var file in Directory.GetFiles(Dir, filePattern, Recursive
						? SearchOption.AllDirectories
						: SearchOption.TopDirectoryOnly).OrderBy(x => x))
					{
						var subPath = file.Substring(Dir.Length + 1);
						Process(subPath, printFileName: true, groupedContext);
					}
				}
				else
				{
					Process(file: filePattern, printFileName: false);
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
					// Console.WriteLine(kvp.Key);

					// Save is already done, but colorful printing is not, so, preview one file
					var orig = Save;
					try
					{
						Save = false;
						Process(kvp.Value.FileNames.First(), printFileName: false, ctxGroups: null);
					}
					finally
					{
						Save = orig;
					}
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
			if (!GroupMatchesByContext)
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

		void Process(string file, bool printFileName, Dictionary<string, CaptureResult> ctxGroups = null)
		{
			var ctxKey = ctxGroups != null ? new StringBuilder() : null;

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

			if (ctxKey == null && printFileName && matchLines.Any())
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
				void PrintLine(bool replaceMode = false)
				{
					var printPosition = line.Start;
					foreach (var lineMatch in line.Matches.Values)
					{
						var match = lineMatch.Match;

						if (match.Index > printPosition)
						{
							Print(ctxKey, ConsoleColor.Gray, body.Substring(printPosition, match.Index - printPosition));
						}

						if (!replaceMode)
						{
							Print(ctxKey, ConsoleColor.Red, match.Value);
						}
						else
						{
							Print(ctxKey, ConsoleColor.Green, GetReplacementString(lineMatch.Regex, match, body, ReplaceTo));
						}

						printPosition = match.Index + match.Length;
					}

					var len = eol + 1 - printPosition;
					if (len >= 0 && printPosition < body.Length && (len - printPosition) < body.Length)
					{
						Print(ctxKey, ConsoleColor.Gray, body.Substring(printPosition, len) + Environment.NewLine);
					}
					else
					{
						Print(ctxKey, null, Environment.NewLine);
					}
				}

				PrintLine();
				if (ReplaceTo != null)
				{
					PrintLine(true);
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
			}

			if (ctxKey != null && ctxKey.Length > 0)
			{
				var key = ctxKey.ToString();
				if (!ctxGroups.TryGetValue(key, out var capture))
				{
					ctxGroups[key] = capture = new CaptureResult();
				}

				capture.FileNames.Add(file);
			}
		}

		void Print(StringBuilder sb, ConsoleColor? color, string msg)
		{
			if (sb != null)
			{
				sb.Append(msg);
			}
			else
			{
				if (color == null)
				{
					Console.Write(msg);
				}
				else
				{
					using (Color(color.Value))
					{
						Console.Write(msg);
					}
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

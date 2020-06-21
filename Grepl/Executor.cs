using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Grepl
{
	using static Tools;

	public class Executor
	{
		public string ReplaceTo { get; set; }

		public string Dir { get; set; }
		public List<string> Files { get; } = new List<string>();

		public List<string> Patterns { get; } = new List<string>();
		public bool Recursive { get; set; }
		public bool Save { get; set; }
		public bool Debug { get; set; }

		private readonly List<Regex> _regexes = new List<Regex>();

		public void Execute()
		{
			if (Debug)
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
				_regexes.Add(new Regex(pattern, RegexOptions.Compiled));
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
						: SearchOption.TopDirectoryOnly))
					{
						var subPath = file.Substring(Dir.Length + 1);
						Process(subPath);
					}
				}
				else
				{
					Process(file: filePattern);
				}
			}
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

		void Process(string file)
		{
			if (file == "-")
			{
				throw new NotImplementedException("STDIN is not implemented yet");
			}
			bool filePrinted = false;
			var lines = File.ReadAllLines(file);
			foreach (var line in lines)
			{
				foreach (var regex in _regexes)
				{
					int printPosition = 0;
					var matches = regex.Matches(line);
					if (matches.Any())
					{
						if (!filePrinted)
						{
							filePrinted = true;
							PrintFileName(file);
						}
					}
					foreach (Match match in matches)
					{
						if (match.Index > printPosition)
						{
							using (Color(ConsoleColor.Gray))
							{
								Console.Write(line.Substring(printPosition, match.Index - printPosition));
							}
						}
						using (Color(ConsoleColor.Red))
						{
							Console.Write(match.Value);
						}
						if (ReplaceTo != null)
						{
							using (Color(ConsoleColor.Green))
							{
								Console.Write(ReplaceTo);
							}
						}
						printPosition = match.Index + match.Length;
					}

					if (matches.Any())
					{
						using (Color(ConsoleColor.Gray))
						{
							Console.WriteLine(line.Substring(printPosition));
						}
					}
				}
			}
		}
	}
}

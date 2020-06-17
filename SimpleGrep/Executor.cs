using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleGrep
{
	public class Executor
	{
		public string Dir { get; set; }
		public List<string> Files { get; } = new List<string>();

		public List<string> Patterns { get; } = new List<string>();
		public bool Recursive { get; set; }

		private readonly List<Regex> _regexes = new List<Regex>();

		public static TextWriter Output = Console.Out;

		public void Execute()
		{
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
				Files.Add("*.*");
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
			Output.WriteLine();

			var fileName = Path.GetFileName(file);
			var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
			var ext = fileName.Substring(fileNameWithoutExt.Length);

			var path = file.Substring(0, file.Length - fileName.Length);

			using (Color(ConsoleColor.DarkCyan))
			{
				Output.Write(path);
				using (Color(ConsoleColor.Cyan))
				{
					Output.Write(fileNameWithoutExt);
				}
				Output.WriteLine(ext);
			}
		}

		IDisposable Color(ConsoleColor color)
		{
			return new ColorScope(color);
		}

		class ColorScope : IDisposable
		{
			private readonly ConsoleColor _original;

			public ColorScope(ConsoleColor color)
			{
				_original = Console.ForegroundColor;
				Console.ForegroundColor = color;
			}

			public void Dispose()
			{
				Console.ForegroundColor = _original;
			}
		}

		void Process(string file)
		{
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
								Output.Write(line.Substring(printPosition, match.Index - printPosition));
							}
						}
						using (Color(ConsoleColor.Red))
						{
							Output.Write(match.Value);
						}
						printPosition = match.Index + match.Length;
					}

					if (matches.Any())
					{
						using (Color(ConsoleColor.Gray))
						{
							Output.WriteLine(line.Substring(printPosition));
						}
					}
				}
			}
		}
	}
}

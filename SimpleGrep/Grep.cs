using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("SimpleGrep.Tests")]

namespace SimpleGrep
{
	using static Tools;

	public class Grep
	{
		static string Exe =>
			Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

		public static int Main(params string[] args)
		{
			try
			{
				return MainHandler(args);
			}
			catch (Exception ex)
			{
				using (Color(ConsoleColor.Red))
				{
					System.Console.Error.WriteLine($"Internal Error: {ex}");
					return 1;
				}
			}
		}

		public static int MainHandler(params string[] args)
		{
			var executor = new Executor();

			var listRemaining = new LinkedList<string>();
			var patternsToLoad = new List<string>();

			for (var i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				if (arg.StartsWith("-") || arg.StartsWith("/"))
				{
					switch (arg.Substring(1))
					{
						case "r":
						case "R":
							executor.Recursive = true;
							break;
						case "e":
							executor.Patterns.Add(args[++i]);
							break;
						case "f":
							patternsToLoad.Add(args[++i]);
							break;
						case "-debug":
							executor.Debug = true;
							using (Color(ConsoleColor.DarkGray))
							{
								Console.WriteLine(string.Join(" ", args));
								for (var index = 0; index < args.Length; index++)
								{
									var argx = args[index];
									Console.WriteLine($"{index}: {argx}");
								}
							}
							break;
						case "-save":
							executor.Save = true;
							break;
						case "-patterns-dir":
							_patternsDirs.Add(args[++i]);
							break;
						case "v":
							// invert
							break;
						case "-var":
							if (i + 2 >= args.Length)
							{
								using (Color(ConsoleColor.Red))
								{
									System.Console.Error.WriteLine(
										$"'--var' option requires 2 more arguments: name & value\r\nSee:\r\n    {Exe} --help var");
								}
								/*System.Console.Error.WriteLine(@"EXAMPLE:
    grepl -r *.csproj -f expression.rx --var Package Autofac --var ver 9.0.0
expression.rx:
((?<=(?'Package'),\sVersion=)(?'v'\d+\.\d+\.\d+)|(?<=packages\\(?'Package')\.)(?'v'\d+\.\d+\.\d+))
" +
								                               "");*/
									return 1;
							}
							var var = args[++i];
							var val = args[++i];
							Vars.Instance.Add(var, val);
							using (Color(ConsoleColor.Yellow))
							{
								// Console.WriteLine($"Variable '{var}' = '{val}'");
							}
							break;
						case "$":
							executor.ReplaceTo = args[++i];
							break;
						default:
							using (Color(ConsoleColor.Red))
							{
								Console.WriteLine($"Unknown option {arg}");
								return 1;
							}
					}
				}
				else
				{
					listRemaining.AddLast(arg);
				}
			}

			foreach (var patternFile in patternsToLoad)
			{
				var pattern = LoadPattern(patternFile);
				executor.Patterns.Add(pattern);
			}

			// capture implicit pattern
			if (listRemaining.Any() && !executor.Patterns.EmptyIfNull().Any())
			{
				executor.Patterns.Add(listRemaining.First.Value);
				listRemaining.RemoveFirst();
			}

			// capture files
			foreach (var arg in listRemaining)
			{
				executor.Files.Add(arg);
			}

			if (executor.Patterns.Count == 0)
			{
				using (Color(ConsoleColor.Red))
				{
					System.Console.Error.WriteLine("Pattern is not specified");
				}
				return 1;
			}

			executor.Dir = Directory.GetCurrentDirectory();
			executor.Execute();
			return 0;
		}

		private static readonly List<string> _patternsDirs = new List<string>();

		static string HydratePattern(string patternTemplate)
		{
			foreach (var varName in Vars.Instance.Names)
			{
				patternTemplate = patternTemplate.Replace($@"(?'{varName}')", Vars.Instance.Get(varName));
			}

			return patternTemplate;
		}

		static string LoadPattern(string fileName)
		{
			foreach (var dir in _patternsDirs.Concat(new[] {"", Path.GetDirectoryName(Assembly.GetCallingAssembly().Location)}))
			{
				var file = Path.Combine(dir, fileName);
				using (Color(ConsoleColor.DarkGray))
				{
					// Console.WriteLine("Try " + file);
				}
				if (File.Exists(file))
				{
					var pattern = File.ReadAllLines(file).FirstOrDefault();
					if (!string.IsNullOrEmpty(pattern))
					{
						return HydratePattern(pattern);
					}
				}
			}

			throw new Exception("File not found: " + fileName);
		}
	}
}

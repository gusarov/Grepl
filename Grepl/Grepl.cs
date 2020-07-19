using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Grepl.Tests")]

namespace Grepl
{
	using static Tools;

	public class Grepl
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
				if (arg.StartsWith("-") || (arg.StartsWith("/") && Path.DirectorySeparatorChar != '/'))
				{
					switch (arg.Substring(1))
					{
						case "r":
						case "R":
							executor.FilesSelectorOptions.Recursive = true;
							break;
						case "e":
							executor.Patterns.Add(args[++i]);
							break;
						case "f":
							patternsToLoad.Add(args[++i]);
							break;
						case "-debugger":
							if (!Debugger.IsAttached)
							{
								Debugger.Launch();
							}
							break;
						case "-debug":
							Shared.Instance.Debug = true;
							Trace.Listeners.Add(new ConsoleTraceListener());
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
						case "-files-with-matches":
						case "l":
							executor.OutputControlOptions.FilesWithMatches = true;
							break;
						case "-save":
							executor.Save = true;
							break;
						case "-group":
							executor.GroupMatchesByContext = true;
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
						case "-exclude-from":
							var lines = File.ReadAllLines(args[++i]);
							executor.FilesSelectorOptions.ExcludeGlobs.AddRange(lines);
							break;
						case "-exclude-dir":
							executor.FilesSelectorOptions.ExcludeGlobs.Add(args[++i].TrimEnd('/') + '/');
							break;
						case "-exclude":
							executor.FilesSelectorOptions.ExcludeGlobs.Add(args[++i]);
							break;
						case "-include":
							executor.FilesSelectorOptions.IncludeGlobs.Add(args[++i]);
							break;
						case "A":
							executor.ContextCaptureOptions.After = int.Parse(args[++i]);
							break;
						case "B":
							executor.ContextCaptureOptions.Before = int.Parse(args[++i]);
							break;
						case "C":
							executor.ContextCaptureOptions.ContextAround = int.Parse(args[++i]);
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

			if (executor.OutputControlOptions.FilesWithMatches)
			{
				if (executor.GroupMatchesByContext)
				{
					using (Color(ConsoleColor.Red))
					{
						Console.WriteLine("Grouping by context and not displaying file content does not make sense. Consider only one of those options: -l or --group");
						return 1;
					}
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

			for (var i = 0; i < executor.Patterns.Count; i++)
			{
				executor.Patterns[i] = HydratePattern(executor.Patterns[i]);
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
						return pattern;
					}
				}
			}

			throw new Exception("File not found: " + fileName);
		}
	}
}

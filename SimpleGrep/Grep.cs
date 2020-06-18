using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SimpleGrep
{
	public class Vars
	{
		public static Vars Instance = new Vars();

		private Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public void Add(string var, string val)
		{
			_values.Add(var, val);
		}

		public IEnumerable<string> Names
		{
			get { return _values.Keys; }
		}

		public string Get(string var)
		{
			_values.TryGetValue(var, out var val);
			return val;
		}
	}

	public class Grep
	{
		public static int Main(params string[] args)
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
							using (Executor.Color(ConsoleColor.DarkGray))
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
								using (Executor.Color(ConsoleColor.Red))
								{
									Console.WriteLine("VAR option requires 2 more arguments: name & value");
									return 1;
								}
							}
							var var = args[++i];
							var val = args[++i];
							Vars.Instance.Add(var, val);
							using (Executor.Color(ConsoleColor.Yellow))
							{
								// Console.WriteLine($"Variable '{var}' = '{val}'");
							}
							break;
						case "$":
							executor.ReplaceTo = args[++i];
							break;
						default:
							using (Executor.Color(ConsoleColor.Red))
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
				Console.WriteLine("Pattern is not specified");
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
				using (Executor.Color(ConsoleColor.DarkGray))
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

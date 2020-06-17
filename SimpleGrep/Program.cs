using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimpleGrep
{
	class Program
	{
		static int Main(string[] args)
		{
			var executor = new Executor();

			var listRemaining = new LinkedList<string>();

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
							executor.Patterns.Add(args[i++]);
							break;
						case "f":
							executor.Files.Add(args[i++]);
							break;
						case "v":
							// invert
							break;
					}
				}
				else
				{
					listRemaining.AddLast(arg);
				}
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
	}
}

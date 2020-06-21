using System;
using System.Collections.Generic;

namespace Grepl
{
	public class Vars
	{
		public static Vars Instance = new Vars();

		private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["q"] = "\"",
			["caret"] = "^",
			["lt"] = "<",
			["gt"] = ">",
			// ["p"] = "|",
			// ["pipe"] = "|",
			["or"] = "|",
			// ["orr"] = "||",
			// ["and"] = "&",
			// ["andd"] = "&&",
			["o/o"] = "%",
			["0/0"] = "%",
		};

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
}
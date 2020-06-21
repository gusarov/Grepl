using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Grepl.Tests
{
	public class ColorfulStream : TextWriter
	{
		public override Encoding Encoding { get; }

		private StringBuilder _sbColored = new StringBuilder();
		private StringBuilder _sbRaw = new StringBuilder();

		public string StringRaw
		{
			get { return _sbRaw.ToString(); }
		}

		public string StringColored
		{
			get { return _sbColored.ToString(); }
		}

		private ConsoleColor _color = Console.ForegroundColor;

		public override void Write(char value)
		{
			if (_color != Console.ForegroundColor)
			{
				_color = Console.ForegroundColor;
				_sbColored.Append($"[{_color}]");
			}
			_sbRaw.Append(value);
			_sbColored.Append(value);
		}
	}
}

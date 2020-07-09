using System;
using System.Collections.Generic;
using System.Text;

namespace Grepl.Model
{

	static class ColoredMessageExtensions
	{
		public static void Write(this ColoredMessage cm, string msg)
		{
			Write(cm, null, msg);
		}

		public static void Write(this ColoredMessage cm, ConsoleColor? color, string msg)
		{
			if (color != null)
			{
				cm.Parts.Add(new SetColorMessagePart(color.Value));
			}

			cm.Parts.Add(new WriteMessagePart(msg));

			if (color != null)
			{
				cm.Parts.Add(new ResetColorMessagePart());
			}

			/*
			if (cm != null)
			{
				cm.Append(msg);
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
			*/
		}

		public static void ToConsole(this ColoredMessage cm)
		{
			var consoleVisitor = new ConsoleMessagePartVisitor();
			foreach (var part in cm.Parts)
			{
				part.Accept(consoleVisitor);
			}
		}
	}

	class ConsoleMessagePartVisitor : IMessagePartVisitor
	{
		private readonly Stack<ConsoleColor> _colors = new Stack<ConsoleColor>();

		public void Visit(WriteMessagePart part)
		{
			Tools.Console.Write(part.Text);
		}

		public void Visit(SetColorMessagePart part)
		{
			_colors.Push(Console.ForegroundColor);
			Console.ForegroundColor = part.Color;
		}

		public void Visit(ResetColorMessagePart part)
		{
			Console.ForegroundColor = _colors.Pop();
		}
	}

	interface IMessagePartVisitor
	{
		void Visit(WriteMessagePart part);
		void Visit(SetColorMessagePart part);
		void Visit(ResetColorMessagePart part);
	}
}

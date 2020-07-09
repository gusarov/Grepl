using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Grepl.Model;

namespace Grepl.Tests
{
	[TestClass]
	public class ColoredMessageTests
	{
		[TestMethod]
		public void Should_have_custom_equality_eq()
		{
			var a = new ColoredMessage();
			a.Parts.Add(new SetColorMessagePart(ConsoleColor.Green));
			a.Parts.Add(new WriteMessagePart("abc"));
			a.Parts.Add(new ResetColorMessagePart());

			var b = new ColoredMessage();
			b.Parts.Add(new SetColorMessagePart(ConsoleColor.Green));
			b.Parts.Add(new WriteMessagePart("abc"));
			b.Parts.Add(new ResetColorMessagePart());

			Assert.AreEqual(a, b);
		}

		[TestMethod]
		public void Should_have_custom_equality_diff()
		{
			var a = new ColoredMessage();
			a.Parts.Add(new SetColorMessagePart(ConsoleColor.Green));
			a.Parts.Add(new WriteMessagePart("abc"));
			a.Parts.Add(new ResetColorMessagePart());

			var b = new ColoredMessage();
			b.Parts.Add(new SetColorMessagePart(ConsoleColor.Green));
			b.Parts.Add(new WriteMessagePart("xxx"));
			b.Parts.Add(new ResetColorMessagePart());

			Assert.AreNotEqual(a, b);
		}

		[TestMethod]
		public void Should_have_custom_equality_diff_color()
		{
			var a = new ColoredMessage();
			a.Parts.Add(new SetColorMessagePart(ConsoleColor.Green));
			a.Parts.Add(new WriteMessagePart("abc"));
			a.Parts.Add(new ResetColorMessagePart());

			var b = new ColoredMessage();
			b.Parts.Add(new SetColorMessagePart(ConsoleColor.Red));
			b.Parts.Add(new WriteMessagePart("abc"));
			b.Parts.Add(new ResetColorMessagePart());

			Assert.AreNotEqual(a, b);
		}
	}
}

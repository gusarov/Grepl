using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grepl.Tests
{
	[TestClass]
	public class TestEnvironment
	{
		[TestMethod]
		public void Should_read_any_line_break()
		{
			// this should pass on all agents: Windows, Linux, macOS
			File.WriteAllText("test", "abc\r\ndef\rqwe\nrty");
			var arr = File.ReadAllLines("test");
			Assert.AreEqual(4, arr.Length);
			Assert.AreEqual("abc", arr[0]);
			Assert.AreEqual("def", arr[1]);
			Assert.AreEqual("qwe", arr[2]);
			Assert.AreEqual("rty", arr[3]);
		}
	}
}

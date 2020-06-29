using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Grepl.Tests
{
	[TestClass]
	public class ReflectionByOpcodes
	{
		[TestMethod]
		public void Should_return_something()
		{
			var rx = new Regex("(a+)t");
			var qq = rx.Replace("daaatx", "_$1_");
			Assert.AreEqual("d_aaa_x", qq);

			var bf = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
			var wrr = rx.GetType().GetField("_replref", bf)?.GetValue(rx);
			var rr = wrr?.GetType().GetProperty("Target", bf)?.GetValue(wrr);
			var typeRr = rr.GetType();
			var typeVsb = typeof(Regex).Assembly.GetType("System.Text.ValueStringBuilder");
			var mi = typeRr.GetMethod("ReplacementImpl", bf);

			var usd = ReplacementBreakout.Call(mi, rx.Match("daaatx"), typeRr, rr, typeVsb);
			Assert.AreEqual("_aaa_", usd);
		}
	}
}

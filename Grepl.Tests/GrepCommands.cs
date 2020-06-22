using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grepl.Tests
{
	[TestClass]
	public class GrepCommands : TestBase
	{
		private static string _orig = Directory.GetCurrentDirectory();

		private string _dir;

		[TestInitialize]
		public void Init()
		{
			_dir = Path.Combine(_orig, "TestData");
			Directory.CreateDirectory(_dir);
			_dir = Path.Combine(_orig, "TestData", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_dir);

			Directory.SetCurrentDirectory(_dir);
			Console.WriteLine("CD /D " + _dir);
		}

		void CreateData()
		{
			File.WriteAllText("file1.txt", "some data1\r\ndef\r");
			File.WriteAllText("file2.txt", "some data2\r\nabc\r\n");

			Directory.CreateDirectory("dir1");
			Directory.CreateDirectory("dir2");
			Directory.CreateDirectory("dir1\\dir11");

			File.WriteAllText("dir1\\file.txt", "some data3\r\nqwe\n");
			File.WriteAllText("dir2\\file.txt", "some data4");
			File.WriteAllText("dir1\\dir11\\file.txt", "some data5");
		}

		[TestMethod]
		public void Should_10_SearchRecursively()
		{
			CreateData();
			var r = GreplEntry("data", "-r");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"
dir1\dir11\file.txt
some data5

dir1\file.txt
some data3

dir2\file.txt
some data4

file1.txt
some data1

file2.txt
some data2
";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);

		}

		[TestMethod]
		public void Should_10_SearchRecursivelyProc()
		{
			CreateData();
			var r = GreplProc("data", "-r");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"
dir1\dir11\file.txt
some data5

dir1\file.txt
some data3

dir2\file.txt
some data4

file1.txt
some data1

file2.txt
some data2
";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);

		}

		void CompareDetails(string a, string b)
		{
			var aa = Printt(a).ToArray();
			var bb = Printt(b).ToArray();
			for (int i = 0; i < aa.Length; i++)
			{
				if (aa[i] == bb[i])
				{
					Console.WriteLine($"{i} {aa[i] == bb[i]} {aa[i]}");
				}
				else
				{
					Console.WriteLine($"{i} {aa[i] == bb[i]} {aa[i]}");
					Console.WriteLine($"{i} {aa[i] == bb[i]} {bb[i]}");
				}
			}
		}

		IEnumerable<string> Printt(string str)
		{
			// Console.WriteLine("LINE A:");
			/*
			var lines1 = str.Split(new[] { '\r' });
			foreach (var line in lines1)
			{
				var abc = string.Join("", line.Select(x => ((int)x).ToString("X2")));
				// Console.WriteLine(str);
				yield return abc;
			}
			*/
			// Console.WriteLine("LINE b:");
			var lines2 = str.Split(new[] { '\n' });
			foreach (var line in lines2)
			{
				var abc = string.Join("", line.Select(x => ((int) x).ToString("X2")));
				// Console.WriteLine(abc);
				yield return abc;
			}
		}

		[TestMethod]
		[Ignore]
		public void Should_10_SearchRecursivelyUnix()
		{
			CreateData();
			var r = Grepl("data", "-r", "--unix");
			Assert.AreEqual(0, r.Code);

			Assert.AreEqual(
@"dir1/dir11/file.txt:some data5
dir1/file.txt:some data3
dir2/file.txt:some data4
file1.txt:some data1
file2.txt:some data2
", r.Output);
		}

		[TestMethod]
		public void Should_20_Replace()
		{
			CreateData();
			var r = Grepl("data", "-r", "-$", "cat");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"
dir1\dir11\file.txt
some datacat5

dir1\file.txt
some datacat3

dir2\file.txt
some datacat4

file1.txt
some datacat1

file2.txt
some datacat2
";

			CompareDetails(exp, raw);


			Assert.AreEqual(exp, raw);

			Assert.AreEqual("some data1", File.ReadAllText("file1.txt"));
			Assert.AreEqual("some data2", File.ReadAllText("file2.txt"));
			Assert.AreEqual("some data3", File.ReadAllText("dir1\\file.txt"));
		}

		[TestMethod]
		public void Should_30_ReplaceAndSave()
		{
			CreateData();
			var r = Grepl("data", "-r", "-$", "cat", "--save");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"
dir1\dir11\file.txt
some datacat5

dir1\file.txt
some datacat3

dir2\file.txt
some datacat4

file1.txt
some datacat1

file2.txt
some datacat2
";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);

			Assert.AreEqual("some cat1\r\ndef\r", File.ReadAllText("file1.txt"));
			Assert.AreEqual("some cat2\r\nanc\r\n", File.ReadAllText("file2.txt"));
			Assert.AreEqual("some cat3\r\nqwe\n", File.ReadAllText("dir1\\file.txt"));
		}
	}
}

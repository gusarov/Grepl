using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grepl.Tests
{
	[TestClass]
	public class GrepCommands : TestBase
	{
		private static string _orig = Directory.GetCurrentDirectory();
		private static char _s = Path.DirectorySeparatorChar;

		private string _dir;

		[AssemblyInitialize]
		public static void AsmInit(TestContext ctx)
		{
			var dir = Path.Combine(_orig, "Temp");
			if (Directory.Exists(dir))
			{
				Directory.Delete(dir, true);
			}

			Directory.CreateDirectory(dir);
		}

		[TestInitialize]
		public void Init()
		{
			_dir = Path.Combine(_orig, "Temp");
			_dir = Path.Combine(_dir, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_dir);

			Directory.SetCurrentDirectory(_dir);
			Console.WriteLine("CD /D " + _dir);
		}

		void CreateData(string sample = "1")
		{
			var sourcePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestData",
				sample);

			var destinationPath = Directory.GetCurrentDirectory();

			foreach (string dirPath in Directory.GetDirectories(sourcePath, "*",
				SearchOption.AllDirectories))
				Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));

			//Copy all the files & Replaces any files with the same name
			foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",
				SearchOption.AllDirectories))
				File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);

			return;
			File.WriteAllText("file1.txt", "some data1\r\ndef\r");
			File.WriteAllText("file2.txt", "some data2\r\nabc\r\n");

			Directory.CreateDirectory("dir1");
			Directory.CreateDirectory("dir2");
			Directory.CreateDirectory("dir1\\dir11");

			File.WriteAllText("dir1\\file.txt", "some data3\r\nqwe\n");
			File.WriteAllText("dir2\\file.txt", "some data4");
			File.WriteAllText("dir1\\dir11\\file.txt", "some data5");
		}

		string Data1
		{
			get
			{
				return @"
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
".Replace('\\', Path.DirectorySeparatorChar);
			}
		}

		[TestMethod]
		public void Should_10_SearchRecursively()
		{
			CreateData();
			var r = GreplEntry("data", "-r");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = Data1;

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);

		}

		[TestMethod]
		public void Should_10_search_multiple_entries_per_line()
		{
			CreateData("2");
			var r = GreplEntry("data", "file1.txt", "-r");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp =
@"//2 aaaa aaaa data aaaa
//4 cccc data bbbb data aaaa
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
			var exp = Data1;

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);

		}

		void CompareDetails(string act, string exp)
		{
			var aa = Printt(act).ToArray();
			var bb = Printt(exp).ToArray();
			for (int i = 0; i < Math.Max(aa.Length, bb.Length); i++)
			{
				if (aa.Length > i && bb.Length > i)
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
				else if (aa.Length <= i)
				{
					Console.WriteLine($"{i} Extra {bb[i]}");
				}
				else if (bb.Length <= i)
				{
					Console.WriteLine($"{i} Extra {aa[i]}");
				}
			}

			Assert.AreEqual(act, exp);
			CollectionAssert.AreEqual(act.ToArray(), exp.ToArray());
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
			var lines2 = str.Split(new[] {'\n'});
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
some data5
some cat5

dir1\file.txt
some data3
some cat3

dir2\file.txt
some data4
some cat4

file1.txt
some data1
some cat1

file2.txt
some data2
some cat2
".Replace('\\', Path.DirectorySeparatorChar);

			CompareDetails(exp, raw);


			Assert.AreEqual(exp, raw);

			Assert.AreEqual("some data1\r\ndef\r", File.ReadAllText("file1.txt"));
			Assert.AreEqual("some data2\r\nabc\r\n", File.ReadAllText("file2.txt"));
			Assert.AreEqual("some data3\r\nqwe\n", File.ReadAllText($"dir1{_s}file.txt"));
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
some data5
some cat5

dir1\file.txt
some data3
some cat3

dir2\file.txt
some data4
some cat4

file1.txt
some data1
some cat1

file2.txt
some data2
some cat2
".Replace('\\', Path.DirectorySeparatorChar);

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);

			Assert.AreEqual("some cat1\r\ndef\r", File.ReadAllText("file1.txt"));
			Assert.AreEqual("some cat2\r\nabc\r\n", File.ReadAllText("file2.txt"));
			Assert.AreEqual("some cat3\r\nqwe\n", File.ReadAllText($"dir1{_s}file.txt"));
		}

		void ShouldFindAll(params string[] args)
		{
			var r = GreplEntry(args);
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = Data1;

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);
		}

		void ShouldFindNone(params string[] args)
		{
			var r = GreplEntry(args);
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = "";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);
		}

		[TestMethod]
		public void Should_20_search_for_line_begin_end()
		{
			CreateData();

			ShouldFindAll(@"some data\d", "-r");
			ShouldFindAll(@"^some data\d$", "-r");
			ShouldFindAll(@"^some data\d", "-r");
			ShouldFindAll(@"some data\d$", "-r");

			ShouldFindNone(@"$some data\d", "-r");
			ShouldFindNone(@"some data\d^", "-r");
			ShouldFindNone(@"$some data\d^", "-r");
		}

		[TestMethod]
		public void Should_20_allow_dollar_in_pattern()
		{
			File.WriteAllText("file1.txt", "line1\r\nprice is 15$\r\nline2");

			var r = Grepl(@"\d+\$", "file1.txt");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"price is 15$
";

			CompareDetails(exp, raw);


			Assert.AreEqual(exp, raw);
		}

		[TestMethod]
		public void Should_20_match_utf_and_ignore_BOM()
		{
			CreateData("BOM");

			var r = Grepl("^data", "-r");
			Assert.AreEqual(0, r.Code);

			var act = r.Output;
			var exp = @"
utf16_be_bom.txt
data

utf16_le_bom.txt
data

utf32_bom.txt
data

utf8_bom.txt
data

utf8_nobom.txt
data
".Replace('\\', Path.DirectorySeparatorChar);

			CompareDetails(act, exp);
		}

		[TestMethod]
		public void Should_30_preserve_BOM_on_save()
		{
			CreateData("BOM");

			Assert.AreEqual("data", File.ReadAllText("utf16_be_bom.txt"));
			Assert.AreEqual("data", File.ReadAllText("utf16_le_bom.txt"));
			Assert.AreEqual("data", File.ReadAllText("utf8_bom.txt"));
			Assert.AreEqual("data", File.ReadAllText("utf8_nobom.txt"));

			var r = Grepl("^data", "-r", "-$", "cat", "--save");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"
utf16_be_bom.txt
data
cat

utf16_le_bom.txt
data
cat

utf32_bom.txt
data
cat

utf8_bom.txt
data
cat

utf8_nobom.txt
data
cat
".Replace('\\', Path.DirectorySeparatorChar);

			CompareDetails(exp, raw);

			Assert.AreEqual("cat", File.ReadAllText("utf8_nobom.txt"));
			Assert.AreEqual("cat", File.ReadAllText("utf8_bom.txt"));
			Assert.AreEqual("cat", File.ReadAllText("utf16_be_bom.txt"));
			Assert.AreEqual("cat", File.ReadAllText("utf16_le_bom.txt"));
			Assert.AreEqual("cat", File.ReadAllText("utf32_bom.txt"));

			CollectionAssert.AreEqual(new byte[] { 0x63, 0x61, 0x74 },
				File.ReadAllBytes("utf8_nobom.txt"));

			CollectionAssert.AreEqual(new byte[] { 0xEF, 0xBB, 0xBF, 0x63, 0x61, 0x74 },
				File.ReadAllBytes("utf8_bom.txt"));

			CollectionAssert.AreEqual(new byte[] { 0xFE, 0xFF, 0x00, 0x63, 0x00, 0x61, 0x00, 0x74 },
				File.ReadAllBytes("utf16_be_bom.txt"));

			CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFE, 0x63, 0x00, 0x61, 0x00, 0x74, 0x00 },
				File.ReadAllBytes("utf16_le_bom.txt"));

			CollectionAssert.AreEqual(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x63, 0x00, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00, 0x74, 0x00, 0x00, 0x00 },
				File.ReadAllBytes("utf32_bom.txt"));

		}

	}
}

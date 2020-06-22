using System;
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

		private ColorfulStream _colorfulStream = new ColorfulStream();

		string OutColored
		{
			get { return _colorfulStream.StringColored; }
		}

		string OutRaw
		{
			get { return _colorfulStream.StringRaw; }
		}

		[TestInitialize]
		public void Init()
		{
			Tools.Console = _colorfulStream;

			_dir = Path.Combine(_orig, "TestData");
			Directory.CreateDirectory(_dir);
			_dir = Path.Combine(_orig, "TestData", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_dir);

			Directory.SetCurrentDirectory(_dir);
			Console.WriteLine("CD /D " + _dir);
		}

		void CreateData()
		{
			File.WriteAllText("file1.txt", "some data1");
			File.WriteAllText("file2.txt", "some data2");

			Directory.CreateDirectory("dir1");
			Directory.CreateDirectory("dir2");
			Directory.CreateDirectory("dir1\\dir11");

			File.WriteAllText("dir1\\file.txt", "some data3");
			File.WriteAllText("dir2\\file.txt", "some data4");
			File.WriteAllText("dir1\\dir11\\file.txt", "some data5");
		}

		[TestMethod]
		public void ShouldSearchRecursively()
		{
			CreateData();
			var r = Grep.Main("data", "-r");
			Assert.AreEqual(0, r);

			var raw = OutRaw;
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

			Print(raw);
			Print(exp);

			Assert.AreEqual(exp, raw);

		}

		[TestMethod]
		public void ShouldSearchRecursivelyProc()
		{
			CreateData();
			var r = Grepl("data", "-r");
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

			Print(raw);
			Print(exp);

			Assert.AreEqual(exp, raw);

		}

		void Print(string str)
		{
			Console.WriteLine("LINE A:");
			var lines1 = str.Split(new[] { '\r' });
			foreach (var line in lines1)
			{
				Console.WriteLine(string.Join("", line.Select(x => ((int)x).ToString("X2"))));
			}
			Console.WriteLine("LINE b:");
			var lines2 = str.Split(new[] { '\n' });
			foreach (var line in lines2)
			{
				Console.WriteLine(string.Join("", line.Select(x => ((int)x).ToString("X2"))));
			}
		}

		[TestMethod]
		[Ignore]
		public void ShouldSearchRecursivelyUnix()
		{
			CreateData();
			var r = Grep.Main("data", "-r", "--unix");
			Assert.AreEqual(0, r);

			Assert.AreEqual(
@"dir1/dir11/file.txt:some data5
dir1/file.txt:some data3
dir2/file.txt:some data4
file1.txt:some data1
file2.txt:some data2
", OutRaw);
		}
	}
}

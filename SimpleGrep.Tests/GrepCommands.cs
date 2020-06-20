using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SimpleGrep.Tests
{
	[TestClass]
	public class GrepCommands
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
			Console.WriteLine(_dir);
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
			/*
			var si = new ProcessStartInfo(typeof(Grep).Assembly.Location, "data -r")
			{
				UseShellExecute = false,
				RedirectStandardOutput = true,
			};
			var p = Process.Start(si);
			p.OutputDataReceived += (s, e) =>
			{

			};
			p.WaitForExit();

			Console.ForegroundColor = ConsoleColor.Red;
			Assert.AreEqual(ConsoleColor.Red, Console.ForegroundColor);
			*/

			CreateData();
			var r = Grep.Main("data", "-r");
			Assert.AreEqual(0, r);

			Assert.AreEqual(@"
file1.txt
some data1

file2.txt
some data2

dir1\file.txt
some data3

dir2\file.txt
some data4

dir1\dir11\file.txt
some data5
", OutRaw);

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

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
	public abstract class GrepCommands : TestBase
	{
		private static string _orig = Directory.GetCurrentDirectory();
		protected static char _s = Path.DirectorySeparatorChar;

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

		protected void CreateData(string sample)
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



		protected void CompareDetails(string act, string exp)
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






	}
}

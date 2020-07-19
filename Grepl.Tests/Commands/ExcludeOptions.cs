using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grepl.Tests.Commands
{
	[TestClass]
	public class ExcludeOptions : GrepCommands
	{
		[TestMethod]
		public void Should_10_have_3_sample_files()
		{
			CreateData("Excludes");

			var (r, output, error) = Grepl("aaaa", "*.*", "-r");
			Assert.AreEqual(0, r);

			CompareDetails(@"
file1.txt
//1 aaaa

file2.txt
//1 aaaa

file3.txt
//1 aaaa
", output);
		}


		[TestMethod]
		public void Should_11_exclude_basic()
		{
			CreateData("Excludes");

			var (r, output, error) = Grepl("aaaa", "*.*", "-r", "--exclude", "*2.txt");
			Assert.AreEqual(0, r);

			CompareDetails(@"
file1.txt
//1 aaaa

file3.txt
//1 aaaa
", output);
		}

		[TestMethod]
		public void Should_11_include_all_plus_r()
		{
			CreateData("1");

			var (r, output, error) = Grepl("some", "*.*", "-r");
			Assert.AreEqual(0, r);

			CompareDetails(@"
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
".Replace('\\', Path.DirectorySeparatorChar), output);
		}

		[TestMethod]
		public void Should_11_include_all_no_r()
		{
			CreateData("1");

			var (r, output, error) = Grepl("some", "*.*");
			Assert.AreEqual(0, r);

			CompareDetails(@"
file1.txt
some data1

file2.txt
some data2
".Replace('\\', Path.DirectorySeparatorChar), output);
		}

		[TestMethod]
		public void Should_20_exclude_full_path_and_include_full_path()
		{
			CreateData("1");

			var fullFile1 = Path.GetFullPath("file1.txt");
			var (r, output, error) = Grepl("some", "**/*.*", "--exclude", fullFile1, fullFile1);
			Assert.AreEqual(0, r);

			CompareDetails(@"
dir1\dir11\file.txt
some data5

dir1\file.txt
some data3

dir2\file.txt
some data4

file2.txt
some data2
".Replace('\\', Path.DirectorySeparatorChar), output);
		}

		[TestMethod]
		public void Should_20_exclude_full_path()
		{
			CreateData("1");

			var fullFile1 = Path.GetFullPath("file1.txt");
			var (r, output, error) = Grepl("some", "**/*.*", "--exclude", fullFile1);
			Assert.AreEqual(0, r);

			CompareDetails(@"
dir1\dir11\file.txt
some data5

dir1\file.txt
some data3

dir2\file.txt
some data4

file2.txt
some data2
".Replace('\\', Path.DirectorySeparatorChar), output);
		}

		[TestMethod]
		public void Should_20_include_full_path()
		{
			CreateData("1");

			var fullFile1 = Path.GetFullPath("file1.txt");
			var fullFile22 = Path.GetFullPath($"dir2{_s}file.txt");
			var (r, output, error) = Grepl("some", fullFile1, "--include", fullFile22);
			Assert.AreEqual(0, r);

			CompareDetails(@"
dir2\file.txt
some data4

file1.txt
some data1
".Replace('\\', Path.DirectorySeparatorChar), output);
		}
	}
}

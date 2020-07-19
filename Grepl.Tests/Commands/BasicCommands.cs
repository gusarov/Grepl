using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Grepl.Tests.Commands
{
	[TestClass]
	public class BasicCommands : GrepCommands
	{
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

		void ShouldFindAllData1(params string[] args)
		{
			var r = GreplEntry(args);
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = Data1;

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);
		}

		void ShouldFindNoneData1(params string[] args)
		{
			var r = GreplEntry(args);
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = "";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);
		}

		[TestMethod]
		public void Should_10_SearchRecursively()
		{
			CreateData("1");
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
			var r = GreplEntry("data", "file1.txt");
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
			CreateData("1");
			var r = GreplProc("data", "-r");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = Data1;

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);

		}


		[TestMethod]
		[Ignore]
		public void Should_10_SearchRecursivelyUnix()
		{
			CreateData("1");
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
			CreateData("1");
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
		public void Should_20_replace_to_empty()
		{
			CreateData("1");
			var r = Grepl("data", "-r", "-$", "");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"
dir1\dir11\file.txt
some data5
some 5

dir1\file.txt
some data3
some 3

dir2\file.txt
some data4
some 4

file1.txt
some data1
some 1

file2.txt
some data2
some 2
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
			CreateData("1");
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

		[TestMethod]
		public void Should_30_group_file_sets_on_search()
		{
			CreateData("3");
			var r = Grepl("bbb", "-r", "--group");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"
2 files:
file1.txt
file2.txt
//2 bbbb

file3.txt
//2 xbbb
";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);
		}

		[TestMethod]
		public void Should_30_replace_and_save_when_groupping_enabled()
		{
			CreateData("3");
			var r = Grepl("bbb", "-r", "-$", "ttt", "--save", "--group");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"
2 files:
file1.txt
file2.txt
//2 bbbb
//2 tttb

file3.txt
//2 xbbb
//2 xttt
";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);

			Assert.AreEqual("//1 aaaa\n//2 tttb\n//3 cccc\n", File.ReadAllText("file1.txt").Replace("\r", ""), "file1");
			Assert.AreEqual("//1 aaaa\n//2 tttb\n//3 cccc\n", File.ReadAllText("file2.txt").Replace("\r", ""), "file2");
			Assert.AreEqual("//1 aaaa\n//2 xttt\n//3 cccc\n", File.ReadAllText("file3.txt").Replace("\r", ""), "file3");
		}

		[TestMethod]
		public void Should_20_search_for_line_begin_end()
		{
			CreateData("1");

			ShouldFindAllData1(@"some data\d", "-r");
			ShouldFindAllData1(@"^some data\d$", "-r");
			ShouldFindAllData1(@"^some data\d", "-r");
			ShouldFindAllData1(@"some data\d$", "-r");

			ShouldFindNoneData1(@"$some data\d", "-r");
			ShouldFindNoneData1(@"some data\d^", "-r");
			ShouldFindNoneData1(@"$some data\d^", "-r");
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

		[TestMethod]
		public void Should_30_replace_with_group_match()
		{
			CreateData("1");
			var r = Grepl(@"dat(a\d)", "-r", "-$", "cat$1", "--save");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"
dir1\dir11\file.txt
some data5
some cata5

dir1\file.txt
some data3
some cata3

dir2\file.txt
some data4
some cata4

file1.txt
some data1
some cata1

file2.txt
some data2
some cata2
".Replace('\\', Path.DirectorySeparatorChar);

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);

			Assert.AreEqual("some cata1\r\ndef\r", File.ReadAllText("file1.txt"));
			Assert.AreEqual("some cata2\r\nabc\r\n", File.ReadAllText("file2.txt"));
			Assert.AreEqual("some cata3\r\nqwe\n", File.ReadAllText($"dir1{_s}file.txt"));
		}

		[TestMethod]
		public void Should_30_read_from_console()
		{
			CreateData("1");
			var r = GreplProc(stdIn =>
			{
				stdIn.WriteLine("abc\\abc.csproj");
				stdIn.WriteLine("abc\\abc.xxx");
				stdIn.WriteLine("def\\def.csproj");
				stdIn.Close();
			}, @".*\.csproj");
			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"abc\abc.csproj
def\def.csproj
";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);
		}

		[TestMethod]
		public void Should_30_show_only_file_name()
		{
			CreateData("3");

			var r = Grepl("bbbb", "-l", "*");

			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"file1.txt
file2.txt
";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);
		}

		[TestMethod]
		public void Should_30_show_only_file_name_replace()
		{
			CreateData("3");

			var r = Grepl("bbbb", "-l", "*", "-$", "xx");

			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"file1.txt
file2.txt
";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);
		}


		[TestMethod]
		public void Should_30_show_only_file_name_replace_save()
		{
			CreateData("3");

			var r = Grepl("bbbb", "-l", "*", "-$", "xx", "--save");

			Assert.AreEqual(0, r.Code);

			var raw = r.Output;
			var exp = @"file1.txt
file2.txt
";

			CompareDetails(exp, raw);

			Assert.AreEqual(exp, raw);

			Assert.AreEqual("//1 aaaa\n//2 xx\n//3 cccc\n", File.ReadAllText("file1.txt").Replace("\r", ""), "file1");
			Assert.AreEqual("//1 aaaa\n//2 xx\n//3 cccc\n", File.ReadAllText("file2.txt").Replace("\r", ""), "file2");
			Assert.AreEqual("//1 aaaa\n//2 xbbb\n//3 cccc\n", File.ReadAllText("file3.txt").Replace("\r", ""), "file3");

		}

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

namespace Grepl
{
	using Signature = Func<Match, object, string>;

	static class ReplacementBreakout
	{
		const BindingFlags _bf = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
		static MethodInfo _toString = typeof(object).GetMethod("ToString");

		static Signature GenerateMethod(Type typeVsb, MethodInfo rrReplacementImpl)
		{
			var d = new DynamicMethod("Capture", typeof(string), new[] { typeof(Match), typeof(object) }, true);
			var g = d.GetILGenerator();

			// ==== C# ====
			/*

			static string Capture(Match match, RegexReplacement rr) {
				ValueStringBuilder vsb = new ValueStringBuilder(256);
				rr.ReplacementImpl(ref vsb, match);
				return vsb.ToString();
			}

			*/

			g.DeclareLocal(typeVsb); // ValueStringBuilder #0_vsb;

			// ValueStringBuilder vsb = new ValueStringBuilder(256);
			g.Emit(OpCodes.Ldc_I4, 256); // ValueStringBuilder initialBuffer size
			g.Emit(OpCodes.Newobj, typeVsb.GetConstructor(new[] { typeof(int) }));
			g.Emit(OpCodes.Stloc_0); // save new span ref to local variable #0

			// rr.ReplacementImpl(ref vsb, match);
			g.Emit(OpCodes.Ldarg_1); // rr
			g.Emit(OpCodes.Ldloca_S, 0); // vsb
			g.Emit(OpCodes.Ldarg_0); // match
			g.Emit(OpCodes.Call, rrReplacementImpl);

			// return vsb.ToString();
			g.Emit(OpCodes.Ldloca_S, 0); // vsb
			g.Emit(OpCodes.Constrained, typeVsb);
			g.Emit(OpCodes.Callvirt, _toString);
			g.Emit(OpCodes.Ret);

			// g.Emit(OpCodes.Ldstr, "a");
			// g.Emit(OpCodes.Ldstr, "a");
			// g.Emit(OpCodes.Ret);

			var act = (Signature)d.CreateDelegate(typeof(Signature));
			return act;

			/*

			var args = methodInfo.GetParameters();
			var argTypes = args.Select(x => x.ParameterType).ToArray();

			var vsbPar = Expression.Parameter(argTypes[0], "vsb");
			var matchPar = Expression.Parameter(argTypes[1], "match");

			var call = Expression.Call(methodInfo, vsbPar, matchPar);
			var lambda = Expression.Lambda<Action<MyType>>(call, p);
			var action = lambda.Compile();

			*/
		}

		static Signature _capture;

		internal static string Call(MethodInfo methodInfo, Match match, Type typeRr, object rr, Type typeVsb)
		{
			if (methodInfo == null)
			{
				throw new ArgumentNullException(nameof(methodInfo));
			}
			if (match == null)
			{
				throw new ArgumentNullException(nameof(match));
			}
			if (typeRr == null)
			{
				throw new ArgumentNullException(nameof(typeRr));
			}
			if (rr == null)
			{
				throw new ArgumentNullException(nameof(rr));
			}
			if (typeVsb == null)
			{
				throw new ArgumentNullException(nameof(typeVsb));
			}
			if (_capture == null)
			{
				_capture = GenerateMethod(typeVsb, methodInfo);
			}
			return _capture(match, rr);
		}

		public static IEnumerable<string> ReplaceBreakout(this Regex rx, string input, string replacement)
		{
			var list = new List<string>();
			foreach (Match match in rx.Matches(input))
			{
				list.Add(ReplaceBreakout(rx, match, input, replacement));
			}
			return list;
		}

		public static string ReplaceBreakout(this Regex rx, Match match, string input, string replacement)
		{
			for (int i = 0; i < 5; i++)
			{
				var wrr = rx.GetType().GetField("_replref", _bf)?.GetValue(rx);
				var rr = wrr?.GetType().GetProperty("Target", _bf)?.GetValue(wrr);

				if (rr == null)
				{
					// update wr cache
					rx.Replace(input, replacement);
					continue;
				}

				var typeVsb = typeof(Regex).Assembly.GetType("System.Text.ValueStringBuilder");

				var mi = rr?.GetType().GetMethod("ReplacementImpl", _bf);
				var rep = Call(mi, match, rr.GetType(), rr, typeVsb);
				return rep;
			}
			throw new Exception("Unable to get RegexReplacement instance");
		}

		/*
		public static Breakout ReplaceBreakout(this Regex rx, string input, string replacement)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			return ReplaceBreakout(rx, input, replacement, -1, UseOptionR(rx) ? input.Length : 0);
		}

		public static Breakout ReplaceBreakout(this Regex rx, string input, string replacement, int count, int startAt)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			return ReplaceBreakout(rx, input, replacement, -1, UseOptionR(rx) ? input.Length : 0);
		}

		public static Breakout Replace(this Regex rx, string input, string replacement, int count, int startAt)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			if (replacement == null)
				throw new ArgumentNullException(nameof(replacement));

			// Gets the weakly cached replacement helper or creates one if there isn't one already.
			var replref = new WeakReference<RegexReplacement>(null);
			var repl = RegexReplacement.GetOrCreate(replref, replacement, caps, capsize, capnames, roptions);

			return repl.Replace(this, input, count, startat);
		}

		static bool UseOptionR(this Regex rx) => (rx.Options & RegexOptions.RightToLeft) != 0;
		*/
	}
	/*
	internal sealed class RegexReplacement_
	{
		// Constants for special insertion patterns
		private const int Specials = 4;
		public const int LeftPortion = -1;
		public const int RightPortion = -2;
		public const int LastGroup = -3;
		public const int WholeString = -4;

		private readonly List<string> _strings; // table of string constants
		private readonly List<int> _rules;      // negative -> group #, positive -> string #

		/// <summary>
		/// Since RegexReplacement shares the same parser as Regex,
		/// the constructor takes a RegexNode which is a concatenation
		/// of constant strings and backreferences.
		/// </summary>
		public RegexReplacement(string rep, RegexNode concat, Hashtable _caps)
		{
			if (concat.Type() != RegexNode.Concatenate)
				throw new ArgumentException(SR.ReplacementError);

			Span<char> buffer = stackalloc char[256];
			ValueStringBuilder vsb = new ValueStringBuilder(buffer);
			List<string> strings = new List<string>();
			List<int> rules = new List<int>();

			for (int i = 0; i < concat.ChildCount(); i++)
			{
				RegexNode child = concat.Child(i);

				switch (child.Type())
				{
					case RegexNode.Multi:
						vsb.Append(child.Str);
						break;

					case RegexNode.One:
						vsb.Append(child.Ch);
						break;

					case RegexNode.Ref:
						if (vsb.Length > 0)
						{
							rules.Add(strings.Count);
							strings.Add(vsb.ToString());
							vsb.Length = 0;
						}
						int slot = child.M;

						if (_caps != null && slot >= 0)
							slot = (int)_caps[slot];

						rules.Add(-Specials - 1 - slot);
						break;

					default:
						throw new ArgumentException(SR.ReplacementError);
				}
			}

			if (vsb.Length > 0)
			{
				rules.Add(strings.Count);
				strings.Add(vsb.ToString());
			}

			Pattern = rep;
			_strings = strings;
			_rules = rules;
		}

		/// <summary>
		/// Either returns a weakly cached RegexReplacement helper or creates one and caches it.
		/// </summary>
		/// <returns></returns>
		public static RegexReplacement GetOrCreate(WeakReference<RegexReplacement> replRef, string replacement, Hashtable caps,
			int capsize, Hashtable capnames, RegexOptions roptions)
		{
			RegexReplacement repl;

			if (!replRef.TryGetTarget(out repl) || !repl.Pattern.Equals(replacement))
			{
				repl = RegexParser.ParseReplacement(replacement, roptions, caps, capsize, capnames);
				replRef.SetTarget(repl);
			}

			return repl;
		}

		/// <summary>
		/// The original pattern string
		/// </summary>
		public string Pattern { get; }

		/// <summary>
		/// Given a Match, emits into the StringBuilder the evaluated
		/// substitution pattern.
		/// </summary>
		public void ReplacementImpl(ref ValueStringBuilder vsb, Match match)
		{
			for (int i = 0; i < _rules.Count; i++)
			{
				int r = _rules[i];
				if (r >= 0)   // string lookup
					vsb.Append(_strings[r]);
				else if (r < -Specials) // group lookup
					vsb.Append(match.GroupToStringImpl(-Specials - 1 - r));
				else
				{
					switch (-Specials - 1 - r)
					{ // special insertion patterns
						case LeftPortion:
							vsb.Append(match.GetLeftSubstring());
							break;
						case RightPortion:
							vsb.Append(match.GetRightSubstring());
							break;
						case LastGroup:
							vsb.Append(match.LastGroupToStringImpl());
							break;
						case WholeString:
							vsb.Append(match.Text);
							break;
					}
				}
			}
		}

		/// <summary>
		/// Given a Match, emits into the ValueStringBuilder the evaluated
		/// Right-to-Left substitution pattern.
		/// </summary>
		public void ReplacementImplRTL(ref ValueStringBuilder vsb, Match match)
		{
			for (int i = _rules.Count - 1; i >= 0; i--)
			{
				int r = _rules[i];
				if (r >= 0)  // string lookup
					vsb.AppendReversed(_strings[r]);
				else if (r < -Specials) // group lookup
					vsb.AppendReversed(match.GroupToStringImpl(-Specials - 1 - r));
				else
				{
					switch (-Specials - 1 - r)
					{ // special insertion patterns
						case LeftPortion:
							vsb.AppendReversed(match.GetLeftSubstring());
							break;
						case RightPortion:
							vsb.AppendReversed(match.GetRightSubstring());
							break;
						case LastGroup:
							vsb.AppendReversed(match.LastGroupToStringImpl());
							break;
						case WholeString:
							vsb.AppendReversed(match.Text);
							break;
					}
				}
			}
		}

		// Three very similar algorithms appear below: replace (pattern),
		// replace (evaluator), and split.

		/// <summary>
		/// Replaces all occurrences of the regex in the string with the
		/// replacement pattern.
		///
		/// Note that the special case of no matches is handled on its own:
		/// with no matches, the input string is returned unchanged.
		/// The right-to-left case is split out because StringBuilder
		/// doesn't handle right-to-left string building directly very well.
		/// </summary>
		public string Replace(Regex regex, string input, int count, int startat)
		{
			if (count < -1)
				throw new ArgumentOutOfRangeException(nameof(count), SR.CountTooSmall);
			if (startat < 0 || startat > input.Length)
				throw new ArgumentOutOfRangeException(nameof(startat), SR.BeginIndexNotNegative);

			if (count == 0)
				return input;

			Match match = regex.Match(input, startat);
			if (!match.Success)
			{
				return input;
			}
			else
			{
				Span<char> charInitSpan = stackalloc char[256];
				var vsb = new ValueStringBuilder(charInitSpan);

				if (!regex.RightToLeft)
				{
					int prevat = 0;

					do
					{
						if (match.Index != prevat)
							vsb.Append(input.AsSpan(prevat, match.Index - prevat));

						prevat = match.Index + match.Length;
						ReplacementImpl(ref vsb, match);
						if (--count == 0)
							break;

						match = match.NextMatch();
					} while (match.Success);

					if (prevat < input.Length)
						vsb.Append(input.AsSpan(prevat, input.Length - prevat));
				}
				else
				{
					// In right to left mode append all the inputs in reversed order to avoid an extra dynamic data structure
					// and to be able to work with Spans. A final reverse of the transformed reversed input string generates
					// the desired output. Similar to Tower of Hanoi.

					int prevat = input.Length;

					do
					{
						if (match.Index + match.Length != prevat)
							vsb.AppendReversed(input.AsSpan(match.Index + match.Length, prevat - match.Index - match.Length));

						prevat = match.Index;
						ReplacementImplRTL(ref vsb, match);
						if (--count == 0)
							break;

						match = match.NextMatch();
					} while (match.Success);

					if (prevat > 0)
						vsb.AppendReversed(input.AsSpan(0, prevat));

					vsb.Reverse();
				}

				return vsb.ToString();
			}
		}
	}
	*/
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleGrep
{
	static class EnumerableExtensions
	{
		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
		{
			return source ?? Enumerable.Empty<T>();
		}
	}
}

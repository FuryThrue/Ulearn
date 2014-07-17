﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

namespace uLearn
{
	public class SlideTestsUtils
	{
		public static void TestExercise(MethodInfo methodInfo)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			var newOut = new StringWriter();
			Console.SetOut(newOut);
			methodInfo.Invoke(null, null);
			var declaringType = methodInfo.DeclaringType;
			if (declaringType == null) throw new Exception("should be!");
			var expectedOutput = string.Join("\n", GetExpectedOutputAttributes(declaringType).Select(a => a.Output));
			Assert.AreEqual(PrepareOutput(expectedOutput), PrepareOutput(newOut.ToString()));
		}

		private static string PrepareOutput(string output)
		{
			return string.Join("\n", output.Trim().SplitToLines());
		}

		public static IEnumerable<ExpectedOutputAttribute> GetExpectedOutputAttributes(Type declaringType)
		{
			return declaringType.GetMethods()
				.SelectMany(m => m.GetCustomAttributes(typeof (ExpectedOutputAttribute)))
				.Cast<ExpectedOutputAttribute>();
		}
	}
}
﻿//region all

using System;
using System.IO;

namespace uLearn.tests
{
	internal class OverloadedMethods : IRunnable
	{
		//region methods_1_and_2
		public string Method()
		{
			return "a";
		}

		//region methods_2_and_3

		public string Method(int a)
		{
			return a.ToString("0");
		}

		//endregion methods_1_and_2

		private string Method(string a)
		{
			return a;
		}

		//endregion methods_2_and_3

		public void Main()
		{
			return;
		}

		private static void Region()
		{
		}
	}
}

/*
region Region
Not method
endregion Region 
 */

//endregion all
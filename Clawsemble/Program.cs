using System;
using System.IO;
using System.Collections.Generic;

namespace Clawsemble
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var preproc = new Preprocessor();
			preproc.DoFile("Sample01.csm");
		}
	}
}

using System;
using System.IO;

namespace Clawsemble
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Token[] tokens = Tokenizer.Tokenize(File.OpenRead("./Sample01.csm"));
		}
	}
}

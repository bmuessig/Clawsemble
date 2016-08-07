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
            try {
                preproc.DoFile(args[0]);
            } catch (CodeError error) {
                Console.WriteLine(error.Message);
            }
        }
    }
}

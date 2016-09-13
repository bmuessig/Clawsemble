using System;
using System.IO;
using System.Collections.Generic;

namespace Clawsemble
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            string infile, outfile;

            if (args.Length == 1) {
                infile = args[0];
                outfile = Path.Combine(new FileInfo(infile).Directory.FullName,
                    string.Format("{0}.cxe", Path.GetFileNameWithoutExtension(infile)));
            } else if (args.Length == 2) {
                infile = args[0];
                outfile = args[1];
            } else {
                Console.WriteLine("Invalid # of arguments!");
                Console.WriteLine("Usage: csm <infile> [outfile]");
                return;
            }

            var preproc = new Preprocessor();
            Console.Write("Preprocessing...");
            try {
                preproc.DoFile(infile);
            } catch (CodeError error) {
                Console.WriteLine(error.Message);
                return;
            }
            Console.WriteLine(" done!");

            var comp = new Compiler();
            Console.Write("Precompiling...");
            try {
                comp.Precompile(preproc.Tokens, preproc.Files);
            } catch (CodeError error) {
                Console.WriteLine(error.Message);
                return;
            }
            Console.WriteLine(" done!");

            Console.Write("Compiling...");
            try {
                var binary = comp.Compile();
                File.WriteAllBytes(outfile, binary.Bake());
            } catch (CodeError error) {
                Console.WriteLine(error.Message);
                return;
            }
            Console.WriteLine(" done!");
        }
    }
}

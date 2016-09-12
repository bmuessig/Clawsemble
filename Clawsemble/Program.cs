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
                return;
            }
            Console.WriteLine("Tokenizing and preprocessing successful!");

            var comp = new Precompiler();
            try {
                comp.Precompile(preproc.Tokens, preproc.Files);
            } catch (CodeError error) {
                Console.WriteLine(error.Message);
                return;
            } //catch (Exception ex) {
            //    Console.WriteLine(ex.Message);
            //   return;
            // }
            Console.WriteLine("Precompiling successful!");
        }
    }
}

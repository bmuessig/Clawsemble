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
            LogPriority loglevel = LogPriority.ExtendedInformation;

            if (args.Length == 1) {
                infile = args[0];
                outfile = Path.Combine(new FileInfo(infile).Directory.FullName,
                    string.Format("{0}.cxe", Path.GetFileNameWithoutExtension(infile)));
            } else if (args.Length == 2) {
                infile = args[0];
                outfile = args[1];
            } else if (args.Length == 3) {
                infile = args[0];
                outfile = args[1];
                if (!Enum.TryParse(args[2], true, out loglevel)) {
                    Logger.Warn("Invalid loglevel given!");
                    Logger.Warn("Switching to ExtInfo Loglevel!", true);
                    loglevel = LogPriority.ExtendedInformation;
                }
            } else {
                Logger.Error("Invalid # of arguments!");
                Logger.Error("Usage: csm <infile> [outfile]", true);
                return;
            }

            Logger.Priority = loglevel;

            var preproc = new Preprocessor();
            Logger.ExtInfo("Preprocessing...");
            try {
                preproc.DoFile(infile);
            } catch (CodeError error) {
                Logger.Error("Preprocessing failed:");
                Logger.Error(error.Message, true);
                return;
            }
            Logger.ExtInfo(" done!", true);

            var comp = new Compiler();
            Logger.ExtInfo("Precompiling...");
            try {
                comp.Precompile(preproc.Tokens, preproc.Files);
            } catch (CodeError error) {
                Logger.Error("Precompiling failed:");
                Logger.Error(error.Message, true);
                return;
            }
            Logger.ExtInfo(" done!", true);

            Logger.ExtInfo("Compiling...");
            try {
                var binary = comp.Compile();
                File.WriteAllBytes(outfile, binary.Bake());
            } catch (CodeError error) {
                Logger.Error("Compiling failed:");
                Logger.Error(error.Message, true);
                return;
            }
            Logger.ExtInfo(" done!", true);
        }
    }
}

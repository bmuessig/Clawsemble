using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public static class DefaultDefinitions
    {
        public static Dictionary<string, Constant> CompileList()
        {
            var defs = new Dictionary<string, Constant>();

            // Misc values
            defs.Add("NULL", new Constant(0));

            // Argument types
            defs.Add("ARGTYPE_NUMBER", new Constant("Number"));
            defs.Add("ARGTYPE_LABEL", new Constant("Label"));
            defs.Add("ARGTYPE_BYTE", new Constant("Byte"));
            defs.Add("ARGTYPE_SHORTLABELFW", new Constant("ShortLabelFW"));
            defs.Add("ARGTYPE_SHORTLABELBW", new Constant("ShortLabelBW"));
            defs.Add("ARGTYPE_DATA", new Constant("Data"));
            defs.Add("ARGTYPE_VALUES", new Constant("Values"));
            defs.Add("ARGTYPE_STRING", new Constant("String"));
            defs.Add("ARGTYPE_FUNCTION", new Constant("Function"));
            defs.Add("ARGTYPE_ARRAY", new Constant("Array"));

            return defs;
        }
    }
}


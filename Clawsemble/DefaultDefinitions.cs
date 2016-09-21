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

            defs.Add("ARGTYPE_ANYTHING", new Constant("Anything"));
            defs.Add("ARGTYPE_NUMBER", new Constant("Number"));
            defs.Add("ARGTYPE_UNUMBER", new Constant("UnsignedNumber"));
            defs.Add("ARGTYPE_BYTE", new Constant("Byte"));
            defs.Add("ARGTYPE_LABEL", new Constant("Label"));
            defs.Add("ARGTYPE_SHORTLABELFW", new Constant("ShortLabelFw"));
            defs.Add("ARGTYPE_SHORTLABELBW", new Constant("ShortLabelBw"));
            defs.Add("ARGTYPE_INTSYMBOL", new Constant("InternSymbol"));
            defs.Add("ARGTYPE_EXTSYMBOL", new Constant("ExternSymbol"));
            defs.Add("ARGTYPE_MODULE", new Constant("Module"));
            defs.Add("ARGTYPE_DATA", new Constant("Data"));
            defs.Add("ARGTYPE_VALUES", new Constant("Values"));
            defs.Add("ARGTYPE_STRING", new Constant("String"));
            defs.Add("ARGTYPE_ARRAY", new Constant("Array"));
            defs.Add("ARGTYPE_BYTEARRAY", new Constant("ByteArray"));

            return defs;
        }
    }
}


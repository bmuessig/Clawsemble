using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public static class DefaultInstructions
    {
        public static InstructionSignature[] CompileList()
        {
            var instrs = new List<InstructionSignature>();

            instrs.Add(new InstructionSignature("nop", 0x0));
            instrs.Add(new InstructionSignature("hcf", 0x1, InstructionArgumentType.Byte & InstructionArgumentType.String));
            instrs.Add(new InstructionSignature("eop", 0x2));
            instrs.Add(new InstructionSignature("stsz", 0x3));
            instrs.Add(new InstructionSignature("plsz", 0x4));
            instrs.Add(new InstructionSignature("rick", 0x5));
            instrs.Add(new InstructionSignature("vnfo", 0x6));
            instrs.Add(new InstructionSignature("brk", 0x7, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("dbps", 0x8, InstructionArgumentType.String));
            instrs.Add(new InstructionSignature("dbda", 0x9));
            instrs.Add(new InstructionSignature("dbdv", 0xa));
            instrs.Add(new InstructionSignature("mdp", 0xc, InstructionArgumentType.String));
            instrs.Add(new InstructionSignature("mdl", 0xd, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("mdv", 0xe, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("lrl", 0xf, InstructionArgumentType.String));
            instrs.Add(new InstructionSignature("lrd", 0x10));
            instrs.Add(new InstructionSignature("lru", 0x11));
            instrs.Add(new InstructionSignature("lc", 0x14, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("ld", 0x15, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("ldd", 0x16));
            instrs.Add(new InstructionSignature("dv", 0x17));
            instrs.Add(new InstructionSignature("dp", 0x18));
            instrs.Add(new InstructionSignature("sw", 0x19));
            instrs.Add(new InstructionSignature("ldx", 0x1a, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("ldxd", 0x1b));
            instrs.Add(new InstructionSignature("psx", 0x1c, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("psxd", 0x1d));
            instrs.Add(new InstructionSignature("psl", 0x1e));
            instrs.Add(new InstructionSignature("ldl", 0x1f));
            instrs.Add(new InstructionSignature("xc", 0x20, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("xcd", 0x21));
            instrs.Add(new InstructionSignature("xci", 0x22, InstructionArgumentType.Array));
            instrs.Add(new InstructionSignature("xcid", 0x23));
            instrs.Add(new InstructionSignature("xs", 0x24, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("xsd", 0x25));
            instrs.Add(new InstructionSignature("xr", 0x26));
            instrs.Add(new InstructionSignature("xd", 0x27));
            instrs.Add(new InstructionSignature("xpi", 0x28, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("xps", 0x29));
            instrs.Add(new InstructionSignature("xf", 0x2a, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("xfd", 0x2b));
            instrs.Add(new InstructionSignature("xcp", 0x2c, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("xcpd", 0x2d));
            instrs.Add(new InstructionSignature("xcpc", 0x2e, InstructionArgumentType.Array));
            instrs.Add(new InstructionSignature("xcpcd", 0x2f));
            instrs.Add(new InstructionSignature("cts", 0x30));
            instrs.Add(new InstructionSignature("jp", 0x33, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("jpd", 0x34));
            instrs.Add(new InstructionSignature("jpz", 0x35));
            instrs.Add(new InstructionSignature("jpp", 0x36));
            instrs.Add(new InstructionSignature("js", 0x37, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("jsb", 0x38, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("ca", 0x39, InstructionArgumentType.Callback));
            instrs.Add(new InstructionSignature("cad", 0x3a));
            instrs.Add(new InstructionSignature("cl", 0x3b, InstructionArgumentType.Callback));
            instrs.Add(new InstructionSignature("cld", 0x3c));
            instrs.Add(new InstructionSignature("ret", 0x3d));
            instrs.Add(new InstructionSignature("add", 0x40));
            instrs.Add(new InstructionSignature("sub", 0x41));
            instrs.Add(new InstructionSignature("mul", 0x42));
            instrs.Add(new InstructionSignature("div", 0x43));
            instrs.Add(new InstructionSignature("mod", 0x44));
            instrs.Add(new InstructionSignature("pow", 0x45));
            instrs.Add(new InstructionSignature("max", 0x46));
            instrs.Add(new InstructionSignature("min", 0x47));
            instrs.Add(new InstructionSignature("and", 0x48));
            instrs.Add(new InstructionSignature("or", 0x49));
            instrs.Add(new InstructionSignature("xor", 0x4a));
            instrs.Add(new InstructionSignature("bsl", 0x4b));
            instrs.Add(new InstructionSignature("bsr", 0x4c));
            instrs.Add(new InstructionSignature("itl", 0x4d));
            instrs.Add(new InstructionSignature("addc", 0x4f, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("subc", 0x50, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("mulc", 0x51, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("divc", 0x52, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("modc", 0x53, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("powc", 0x54, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("andc", 0x55, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("orc", 0x56, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("xorc", 0x57, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("bslc", 0x58, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("bsrc", 0x59, InstructionArgumentType.Byte));
            instrs.Add(new InstructionSignature("itlc", 0x5a, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("icr", 0x5c));
            instrs.Add(new InstructionSignature("dcr", 0x5d));
            instrs.Add(new InstructionSignature("abs", 0x5e));
            instrs.Add(new InstructionSignature("rand", 0x5f));
            instrs.Add(new InstructionSignature("sqrt", 0x60));
            instrs.Add(new InstructionSignature("log2", 0x61));
            instrs.Add(new InstructionSignature("sin", 0x62));
            instrs.Add(new InstructionSignature("cos", 0x63));
            instrs.Add(new InstructionSignature("neg", 0x64));
            instrs.Add(new InstructionSignature("not", 0x65));
            instrs.Add(new InstructionSignature("rev", 0x66));
            instrs.Add(new InstructionSignature("cbs", 0x67));
            instrs.Add(new InstructionSignature("cbz", 0x68));
            instrs.Add(new InstructionSignature("land", 0x6a));
            instrs.Add(new InstructionSignature("lor", 0x6b));
            instrs.Add(new InstructionSignature("eq", 0x6c));
            instrs.Add(new InstructionSignature("neq", 0x6d));
            instrs.Add(new InstructionSignature("lt", 0x6e));
            instrs.Add(new InstructionSignature("lteq", 0x6f));
            instrs.Add(new InstructionSignature("gt", 0x70));
            instrs.Add(new InstructionSignature("gteq", 0x71));
            instrs.Add(new InstructionSignature("lnot", 0x73));
            instrs.Add(new InstructionSignature("ipow2", 0x74));
            instrs.Add(new InstructionSignature("eqc", 0x76, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("neqc", 0x77, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("ltc", 0x78, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("lteqc", 0x79, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("gtc", 0x7a, InstructionArgumentType.Number));
            instrs.Add(new InstructionSignature("gteqc", 0x7b, InstructionArgumentType.Number));

            return instrs.ToArray();
        }
    }
}
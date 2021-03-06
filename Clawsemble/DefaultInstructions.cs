﻿using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public static class DefaultInstructions
    {
        public static List<InstructionSignature> CompileList()
        {
            var instrs = new List<InstructionSignature>();

            instrs.Add(new InstructionSignature("nop", 0x0));
            instrs.Add(new InstructionSignature("vnfo", 0x1));
            instrs.Add(new InstructionSignature("stsz", 0x2));
            instrs.Add(new InstructionSignature("casz", 0x3));
            instrs.Add(new InstructionSignature("plsz", 0x4));
            instrs.Add(new InstructionSignature("rick", 0x5));
            instrs.Add(new InstructionSignature("hcf", 0x6, new InstructionArgument(InstructionArgumentType.String)));
            instrs.Add(new InstructionSignature("eop", 0x7));
            instrs.Add(new InstructionSignature("brk", 0x8, new InstructionArgument(InstructionArgumentType.Byte)));
            instrs.Add(new InstructionSignature("dbps", 0x9, new InstructionArgument(InstructionArgumentType.String)));
            instrs.Add(new InstructionSignature("dbda", 0xa));
            instrs.Add(new InstructionSignature("dbdv", 0xb));
            instrs.Add(new InstructionSignature("dbrn", 0xc));
            instrs.Add(new InstructionSignature("mdp", 0xd, new InstructionArgument(InstructionArgumentType.Module)));
            instrs.Add(new InstructionSignature("mdl", 0xe, new InstructionArgument(InstructionArgumentType.Module)));
            instrs.Add(new InstructionSignature("mdv", 0xf, new InstructionArgument(InstructionArgumentType.Module)));
            instrs.Add(new InstructionSignature("lrl", 0x10, new InstructionArgument(InstructionArgumentType.String)));
            instrs.Add(new InstructionSignature("lrd", 0x11));
            instrs.Add(new InstructionSignature("lru", 0x12));
            instrs.Add(new InstructionSignature("fex", 0x13));
            instrs.Add(new InstructionSignature("lc", 0x14, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("ld", 0x15, new InstructionArgument(InstructionArgumentType.Byte)));
            instrs.Add(new InstructionSignature("ldd", 0x16));
            instrs.Add(new InstructionSignature("dv", 0x17));
            instrs.Add(new InstructionSignature("dp", 0x18));
            instrs.Add(new InstructionSignature("sw", 0x19));
            instrs.Add(new InstructionSignature("swf", 0x1a, new InstructionArgument(InstructionArgumentType.Byte)));
            instrs.Add(new InstructionSignature("lx", 0x1b, new InstructionArgument(InstructionArgumentType.UnsignedNumber)));
            instrs.Add(new InstructionSignature("lxd", 0x1c));
            instrs.Add(new InstructionSignature("lxb", 0x1d, new InstructionArgument(InstructionArgumentType.UnsignedNumber)));
            instrs.Add(new InstructionSignature("lxbd", 0x1e));
            instrs.Add(new InstructionSignature("px", 0x1f, new InstructionArgument(InstructionArgumentType.UnsignedNumber)));
            instrs.Add(new InstructionSignature("pxd", 0x20));
            instrs.Add(new InstructionSignature("pxb", 0x21, new InstructionArgument(InstructionArgumentType.UnsignedNumber)));
            instrs.Add(new InstructionSignature("pxbd", 0x22));
            instrs.Add(new InstructionSignature("xc", 0x23, new InstructionArgument(InstructionArgumentType.UnsignedNumber)));
            instrs.Add(new InstructionSignature("xcb", 0x24, new InstructionArgument(InstructionArgumentType.UnsignedNumber)));
            instrs.Add(new InstructionSignature("xcd", 0x25));
            instrs.Add(new InstructionSignature("xci", 0x26, new InstructionArgument(InstructionArgumentType.Array)));
            instrs.Add(new InstructionSignature("xs", 0x27, new InstructionArgument(InstructionArgumentType.Byte)));
            instrs.Add(new InstructionSignature("xsd", 0x28));
            instrs.Add(new InstructionSignature("xsr", 0x29));
            instrs.Add(new InstructionSignature("xar", 0x2a));
            instrs.Add(new InstructionSignature("xr", 0x2b));
            instrs.Add(new InstructionSignature("xrb", 0x2c));
            instrs.Add(new InstructionSignature("xrl", 0x2d));
            instrs.Add(new InstructionSignature("xrlb", 0x2e));
            instrs.Add(new InstructionSignature("xd", 0x2f));
            instrs.Add(new InstructionSignature("xdl", 0x30));
            instrs.Add(new InstructionSignature("xpo", 0x31));
            instrs.Add(new InstructionSignature("xpi", 0x32, new InstructionArgument(InstructionArgumentType.Byte)));
            instrs.Add(new InstructionSignature("xps", 0x33));
            instrs.Add(new InstructionSignature("xpsb", 0x34));
            instrs.Add(new InstructionSignature("xcs", 0x35));
            instrs.Add(new InstructionSignature("xfn", 0x36));
            instrs.Add(new InstructionSignature("xfc", 0x37, new InstructionArgument(InstructionArgumentType.Byte)));
            instrs.Add(new InstructionSignature("xmn", 0x38, new InstructionArgument(InstructionArgumentType.Byte)));
            instrs.Add(new InstructionSignature("xmb", 0x39, new InstructionArgument(InstructionArgumentType.Byte)));
            instrs.Add(new InstructionSignature("xmd", 0x3a));
            instrs.Add(new InstructionSignature("xmcn", 0x3b, new InstructionArgument(InstructionArgumentType.Array)));
            instrs.Add(new InstructionSignature("xmcb", 0x3c, new InstructionArgument(InstructionArgumentType.ByteArray)));
            instrs.Add(new InstructionSignature("xmcd", 0x3d));
            instrs.Add(new InstructionSignature("cnb", 0x3e));
            instrs.Add(new InstructionSignature("cnc", 0x3f, new InstructionArgument(InstructionArgumentType.UnsignedNumber)));
            instrs.Add(new InstructionSignature("cbn", 0x40));
            instrs.Add(new InstructionSignature("jp", 0x41, new InstructionArgument(InstructionArgumentType.Label)));
            instrs.Add(new InstructionSignature("jpd", 0x42));
            instrs.Add(new InstructionSignature("jpn", 0x43, new InstructionArgument(InstructionArgumentType.Label)));
            instrs.Add(new InstructionSignature("jpp", 0x44, new InstructionArgument(InstructionArgumentType.Label)));
            instrs.Add(new InstructionSignature("jppd", 0x45));
            instrs.Add(new InstructionSignature("js", 0x46, new InstructionArgument(InstructionArgumentType.ShortLabelFw)));
            instrs.Add(new InstructionSignature("jsb", 0x47, new InstructionArgument(InstructionArgumentType.ShortLabelBw)));
            instrs.Add(new InstructionSignature("ca", 0x48, new InstructionArgument(InstructionArgumentType.InternSymbol)));
            instrs.Add(new InstructionSignature("cad", 0x49));
            instrs.Add(new InstructionSignature("cl", 0x4a, new InstructionArgument(InstructionArgumentType.ExternSymbol)));
            instrs.Add(new InstructionSignature("cld", 0x4b));
            instrs.Add(new InstructionSignature("ret", 0x4c));
            instrs.Add(new InstructionSignature("add", 0x4e));
            instrs.Add(new InstructionSignature("sub", 0x4f));
            instrs.Add(new InstructionSignature("mul", 0x50));
            instrs.Add(new InstructionSignature("div", 0x51));
            instrs.Add(new InstructionSignature("mod", 0x52));
            instrs.Add(new InstructionSignature("pow", 0x53));
            instrs.Add(new InstructionSignature("max", 0x54));
            instrs.Add(new InstructionSignature("min", 0x55));
            instrs.Add(new InstructionSignature("and", 0x56));
            instrs.Add(new InstructionSignature("or", 0x57));
            instrs.Add(new InstructionSignature("xor", 0x58));
            instrs.Add(new InstructionSignature("bsl", 0x59));
            instrs.Add(new InstructionSignature("bsr", 0x5a));
            instrs.Add(new InstructionSignature("addc", 0x5b, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("subc", 0x5c, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("mulc", 0x5d, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("divc", 0x5e, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("modc", 0x5f, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("andc", 0x60, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("orc", 0x61, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("xorc", 0x62, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("bslc", 0x63, new InstructionArgument(InstructionArgumentType.Byte)));
            instrs.Add(new InstructionSignature("bsrc", 0x64, new InstructionArgument(InstructionArgumentType.Byte)));
            instrs.Add(new InstructionSignature("icr", 0x65));
            instrs.Add(new InstructionSignature("dcr", 0x66));
            instrs.Add(new InstructionSignature("abs", 0x67));
            instrs.Add(new InstructionSignature("rnd", 0x68));
            instrs.Add(new InstructionSignature("sqrt", 0x69));
            instrs.Add(new InstructionSignature("log2", 0x6a));
            instrs.Add(new InstructionSignature("ipw2", 0x6b));
            instrs.Add(new InstructionSignature("neg", 0x6c));
            instrs.Add(new InstructionSignature("not", 0x6d));
            instrs.Add(new InstructionSignature("rev", 0x6e));
            instrs.Add(new InstructionSignature("cbs", 0x6f));
            instrs.Add(new InstructionSignature("cbz", 0x70));
            instrs.Add(new InstructionSignature("lnot", 0x71));
            instrs.Add(new InstructionSignature("land", 0x72));
            instrs.Add(new InstructionSignature("lor", 0x73));
            instrs.Add(new InstructionSignature("eq", 0x74));
            instrs.Add(new InstructionSignature("neq", 0x75));
            instrs.Add(new InstructionSignature("lt", 0x76));
            instrs.Add(new InstructionSignature("lteq", 0x77));
            instrs.Add(new InstructionSignature("gt", 0x78));
            instrs.Add(new InstructionSignature("gteq", 0x79));
            instrs.Add(new InstructionSignature("eqc", 0x7a, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("neqc", 0x7b, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("ltc", 0x7c, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("lteqc", 0x7d, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("gtc", 0x7e, new InstructionArgument(InstructionArgumentType.Number)));
            instrs.Add(new InstructionSignature("gteqc", 0x7f, new InstructionArgument(InstructionArgumentType.Number)));

            return instrs;
        }
    }
}
using System;

namespace Clawsemble
{
    public class Compiler
    {
        public Compiler()
        {
        }


        /*
         } else if (Tokens[ptr].Type == TokenType.Word) {
                    if (string.IsNullOrWhiteSpace(Tokens[ptr].Content))
                        throw new CodeError(CodeErrorType.WordInvalid, "Empty instruction!", Tokens[ptr], GetFilename(Tokens[ptr]));

                    InstructionSignature instr;
                    if (FindSignature(Tokens[ptr].Content, out instr)) {
                        DoInstruction(instr, ref ptr, Binary);
                    } else
                        throw new CodeError(CodeErrorType.WordUnknown, "Unknown instruction!", Tokens[ptr], GetFilename(Tokens[ptr]));
                } else if (Tokens[ptr].Type == TokenType.Number) {
                    for (; Tokens[ptr].Type != TokenType.Break && Tokens[ptr].Type != TokenType.Seperator; ptr++) {
                        if (Tokens[ptr].Type == TokenType.String) {
                            
                        } else if (Tokens[ptr].Type == TokenType.Number) {

                        }
                    }
                }
           */

        /*
        private void DoInstruction(InstructionSignature Instruction, ref int Pointer, List<byte> Bytes)
        {
            int argnum = 0;
            Bytes.Add(Instruction.Code);

            foreach (InstructionArgumentType arg in Instruction.Arguments) {
                if (!IsBeforeEOF(Pointer++, InputTokens.Count))
                    throw new CodeError(CodeErrorType.UnexpectedEOF, InputTokens[Pointer - 1].Line, GetFilename(InputTokens[Pointer].File));
                if (string.IsNullOrWhiteSpace(InputTokens[Pointer].Content))
                    throw new CodeError(CodeErrorType.ConstantInvalid, "Constant is empty!", InputTokens[Pointer], GetFilename(InputTokens[Pointer].File));
                argnum++;

                if (arg == InstructionArgumentType.Number) {
                    if (InputTokens[Pointer].Type != TokenType.Number)
                        throw new CodeError(CodeErrorType.ExpectedNumber, InputTokens[Pointer], GetFilename(InputTokens[Pointer].File));

                    long val;
                    if (!long.TryParse(InputTokens[Pointer].Content, out val))
                        throw new CodeError(CodeErrorType.ConstantInvalid, "Can't parse numeric constant!", InputTokens[Pointer], GetFilename(InputTokens[Pointer].File));
                    Bytes.AddRange(BitConverter.GetBytes(val));

                    continue;
                }

                if ((arg & InstructionArgumentType.Label) > 0) {

                }

                if ((arg & InstructionArgumentType.String) > 0) {

                }

                if ((arg & InstructionArgumentType.Byte) > 0) {
                    if (InputTokens[Pointer].Type != TokenType.Number)
                        throw new CodeError(CodeErrorType.ExpectedNumber, InputTokens[Pointer], GetFilename(InputTokens[Pointer].File));

                    byte val;
                    if (!byte.TryParse(InputTokens[Pointer].Content, out val))
                        throw new CodeError(CodeErrorType.ConstantInvalid, "Can't parse numeric constant!", InputTokens[Pointer], GetFilename(InputTokens[Pointer].File));
                    Bytes.Add(val);

                    continue;
                }

                throw new CodeError(CodeErrorType.SignatureMissmatch,
                    string.Format("The constant does not match the signature ({1}) of argument #{0} of the instruction \"{2}\"!",
                        argnum, arg.ToString(), Instruction.Mnemonic),
                    InputTokens[Pointer], GetFilename(InputTokens[Pointer].File));
            }
        }
        */
    }
}


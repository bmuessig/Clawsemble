using System;

namespace Clawsemble
{
    public enum TokenType
    {
        Empty,
        Invalid,
        Unexpected,
        Comment,
        PreprocessorDirective,
        Not,
        Assign,
        LessThan,
        LessEqual,
        Equal,
        NotEqual,
        GreaterThan,
        GreaterEqual,
        BitwiseAnd,
        LogicalAnd,
        BitwiseOr,
        LogicalOr,
        BitwiseXOr,
        BitwiseNot,
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        BitshiftLeft,
        BitshiftRight,
        ParanthesisOpen,
        ParanthesisClose,
        CompilerDirective,
        String,
        Character,
        CharacterRemove,
        HexadecimalEscape,
        Word,
        Number,
        Seperator,
        Break
    }
}


using System;

namespace Clawsemble
{
    public enum CodeErrorType
    {
        UnknownError,
        DirectiveInvalid,
        DirectiveUnknown,
        InstructionUnknown,
        TokenError,
        IntentionalError,
        DivisionByZero,
        ExpectedWord,
        ExpectedNumber,
        ExpectedString,
        ExpectedOperator,
        ExpectedExpression,
        ExpectedSeperator,
        ExpectedBreak,
        ExpectedHeader,
        ExpectedSymbol,
        ExpectedInstruction,
        ArgumentInvalid,
        ArgumentRange,
        WordInvalid,
        WordUnknown,
        WordCollision,
        UnexpectedEOF,
        UnexpectedToken,
        UnexpectedOperator,
        UnexpectedDirective,
        UnexpectedBreak,
        ExpressionEmpty,
        ExpressionInvalid,
        ConstantInvalid,
        ConstantRange,
        ConstantEmpty,
        ConstantNotFound,
        MissmatchedParantheses,
        StackUnderflow,
        StackOverflow,
        TypeMissmatch,
        SignatureMissmatch,
        OperationInvalid,
        IfMissmatched
    }
}


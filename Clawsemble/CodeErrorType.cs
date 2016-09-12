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
        ArgumentInvalid,
        ArgumentRange,
        WordInvalid,
        WordUnknown,
        WordCollision,
        UnexpectedEOF,
        UnexpectedToken,
        UnexpectedOperator,
        UnexpectedDirective,
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


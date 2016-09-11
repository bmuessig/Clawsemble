using System;

namespace Clawsemble
{
    public enum CodeErrorType
    {
        UnknownError,
        UnknownDirective,
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


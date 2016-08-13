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
        ExpectedEOL,
        ExpectedHeader,
        ArgumentOutOfBounds,
        WordInvalid,
        WordUnknown,
        UnexpectedEOF,
        UnexpectedToken,
        ExpressionEmpty,
        ExpressionInvalid,
        ExpressionUncontained,
        ConstantInvalid,
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


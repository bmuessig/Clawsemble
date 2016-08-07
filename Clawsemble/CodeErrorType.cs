using System;

namespace Clawsemble
{
    public enum CodeErrorType
    {
        UnknownError,
        UnknownPreprocDir,
        IntentionalError,
        DivisionByZero,
        ExpectedWord,
        ExpectedNumber,
        ExpectedString,
        ExpectedOperator,
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
        OperationInvalid
    }
}


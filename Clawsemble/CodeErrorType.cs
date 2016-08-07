using System;

namespace Clawsemble
{
    public enum CodeErrorType
    {
        UnknownError,
        UnknownPreprocDir,
        IntentionalError,
        ExpectedWord,
        ExpectedNumber,
        ExpectedString,
        UnexpectedEOF,
        UnexpectedToken,
        ExpressionEmpty,
        ExpressionInvalid,
        ConstantInvalid,
        ConstantEmpty,
        ConstantNotFound
    }
}


using System;

namespace Clawsemble
{
    public enum CodeErrorType
    {
        UnknownError,
        UnknownPreprocDir,
        ExpectedWord,
        ExpectedNumber,
        ExpectedString,
        UnexpectedEOF,
        UnexpectedToken,
        EmptyExpression,
        ConstantInvalid,
        ConstantEmpty,
        ConstantNotFound
    }
}


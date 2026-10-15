namespace Grit.SharedKernel;

public sealed record Error(string Code, string Message, ErrorType Type);

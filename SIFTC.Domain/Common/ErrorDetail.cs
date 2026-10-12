namespace SIFTC.Domain.Common;

public record ErrorDetail(
    string Field,
    string Code,
    string Message
);
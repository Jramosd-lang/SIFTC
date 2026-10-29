namespace SIFTC.Application.Common;

public record Pagina<T>(
    IReadOnlyList<T> Items,
    int Total
);

using SIFTC.Domain.Entities;

namespace SIFTC.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    int ExpirySeconds { get; }
    string GenerateToken(Usuario usuario);
}

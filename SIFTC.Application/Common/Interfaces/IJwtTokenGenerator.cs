namespace SIFTC.Application.Common.Interfaces;

using SIFTC.Domain.Entities;

public interface IJwtTokenGenerator
{
    string GenerateToken(Usuario usuario);
}
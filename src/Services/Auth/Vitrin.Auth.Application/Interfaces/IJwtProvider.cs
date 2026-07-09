using Vitrin.Auth.Domain.Entities;

namespace Vitrin.Auth.Application.Interfaces;

public interface IJwtProvider
{
    string Generate(User user);
}

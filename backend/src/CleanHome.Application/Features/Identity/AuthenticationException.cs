namespace CleanHome.Application.Features.Identity;

public sealed class AuthenticationException : Exception
{
    public AuthenticationException()
        : base("Las credenciales o la sesión no son válidas.")
    {
    }
}

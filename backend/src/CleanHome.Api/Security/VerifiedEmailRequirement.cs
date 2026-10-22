using Microsoft.AspNetCore.Authorization;

namespace CleanHome.Api.Security;

public sealed class VerifiedEmailRequirement : IAuthorizationRequirement
{
}

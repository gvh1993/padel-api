namespace Padel.Application.Common;

/// <summary>
/// The current user is authenticated but not allowed to access the resource (HTTP 403).
/// </summary>
public class ForbiddenException(string message = "You are not allowed to access this resource.") : Exception(message);

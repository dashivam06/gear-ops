using System;

namespace gearOps.Application.Exceptions;

/// <summary>
/// Exception thrown when an admin user is not found.
/// </summary>
public class AdminNotFoundException : NotFoundException
{
    public AdminNotFoundException(int adminId) 
        : base($"Administrator with ID {adminId} not found.") { }

    public AdminNotFoundException(string message) 
        : base(message) { }
}

/// <summary>
/// Exception thrown when password verification fails during an administrative action.
/// </summary>
public class AdminPasswordVerificationFailedException : UnauthorizedException
{
    public AdminPasswordVerificationFailedException(string message = "Administrator password verification failed.") 
        : base(message) { }
}

/// <summary>
/// Exception thrown when an admin tries to perform an invalid action on scheduled notifications.
/// </summary>
public class ScheduledNotificationException : BadRequestException
{
    public ScheduledNotificationException(string message) 
        : base(message) { }
}

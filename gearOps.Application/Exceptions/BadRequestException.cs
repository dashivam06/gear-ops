using System;
namespace gearOps.Application.Exceptions;
public class BadRequestException : Exception { public BadRequestException(string message) : base(message) {} }

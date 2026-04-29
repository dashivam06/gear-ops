using System;
namespace gearOps.Application.Exceptions;
public class ConflictException : Exception { public ConflictException(string message) : base(message) {} }

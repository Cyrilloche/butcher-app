namespace Butcher.Api.Common.Exceptions;

public class TooManyRequestsException(string message) : Exception(message);

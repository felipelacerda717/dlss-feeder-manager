namespace DLSSFeederManager.Models;

public sealed class OperationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public IReadOnlyList<string> Details { get; init; } = [];

    public static OperationResult Ok(string message, params string[] details) => new()
    {
        Success = true,
        Message = message,
        Details = details
    };

    public static OperationResult Fail(string message, params string[] details) => new()
    {
        Success = false,
        Message = message,
        Details = details
    };
}

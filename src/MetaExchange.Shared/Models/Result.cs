namespace MetaExchange.Shared.Models;

public class Result
{
    public bool Successful { get; private set; } = true;
    public string ErrorMessage { get; private set; } = string.Empty;

    private readonly List<string> _warnings = [];
    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

    public void AddError(string errorMessage)
    {
        Successful = false;
        ErrorMessage = errorMessage;
    }

    public void AddWarning(string warning)
    {
        _warnings.Add(warning);
    }
}

public class Result<T> : Result where T : notnull
{
    public Result()
    {
    }

    public Result(T value)
    {
        SetValue(value);
    }

    public T? Value { get; private set; }

    private void SetValue(T value)
    {
        Value = value;
    }
}

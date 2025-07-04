namespace MetaExchange.Shared.Models;

public class ExecuteOrderPlanResult
{
    public bool Successful { get; internal set; } = true;
    public string ErrorMessage { get; private set; } = string.Empty;

    internal void AddError(string errorMessage)
    {
        Successful = false;
        ErrorMessage = errorMessage;
    }
}

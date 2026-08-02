namespace ACCHCO.EUSMS.App.Helpers;

public class TicketSavedMessage
{
}

public class EquipmentSavedMessage
{
}

public class DataChangedMessage
{
}

public class NavigateMessage
{
    public string Target { get; }
    public object? Data { get; }

    public NavigateMessage(string target, object? data = null)
    {
        Target = target;
        Data = data;
    }
}

public class ShowSnackbarMessage
{
    public string Message { get; }

    public ShowSnackbarMessage(string message)
    {
        Message = message;
    }
}

public class ShowLoadingMessage
{
    public bool IsLoading { get; }

    public ShowLoadingMessage(bool isLoading)
    {
        IsLoading = isLoading;
    }
}

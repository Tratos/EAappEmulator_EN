namespace EAappEmulater.Helper;

public static class MsgBoxHelper
{
    /// <summary>
    /// General information pop-up window, Information
    /// </summary>
    public static void Information(string content, string title = "info")
    {
        MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// General warning pop-up window, Warning
    /// </summary>
    public static void Warning(string content, string title = "warn")
    {
        MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// General error pop-up window, Error
    /// </summary>
    public static void Error(string content, string title = "error")
    {
        MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// General exception pop-up window, Exception
    /// </summary>
    public static void Exception(Exception ex, string title = "abnormal")
    {
        MessageBox.Show("An unknown exception occurred, check the exception prompt for more information\n\nException information: \n" + ex.Message,
            title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

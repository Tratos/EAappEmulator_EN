namespace EAappEmulater.Helper;

public static class MsgBoxHelper
{
    /// <summary>
    /// 通用信息弹窗，Information
    /// </summary>
    public static void Information(string content, string title = "hint")
    {
        MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 通用警告弹窗，Warning
    /// </summary>
    public static void Warning(string content, string title = "warn")
    {
        MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// 通用错误弹窗，Error
    /// </summary>
    public static void Error(string content, string title = "mistake")
    {
        MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// 通用异常弹窗，Exception
    /// </summary>
    public static void Exception(Exception ex, string title = "abnormal")
    {
        MessageBox.Show("An unknown exception occurred, check the exception prompt for more information\n\nException information : \n" + ex.Message,
            title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

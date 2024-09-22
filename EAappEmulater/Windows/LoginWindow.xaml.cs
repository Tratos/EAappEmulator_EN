using CommunityToolkit.Mvvm.Input;
using EAappEmulater.Helper;
using Microsoft.Web.WebView2.Core;

namespace EAappEmulater.Windows;

/// <summary>
/// Interaction logic of LoginWindow.xaml
/// </summary>
public partial class LoginWindow
{
    private const string _host = "https://accounts.ea.com/connect/auth?response_type=code&locale=en_US&client_id=EADOTCOM-WEB-SERVER";

    /**
     * 2024/04/29
     * Regarding the first loading setting of WebView2, if the Visibility is not visible, it will cause a brief white screen.
     * https://github.com/MicrosoftEdge/WebView2Feedback/issues/3707#issuecomment-1679440957
     */

    /// <summary>
    /// Whether to clear the cache (used to switch to a new account)
    /// </summary>
    private readonly bool _isClear;

    public LoginWindow(bool isClear)
    {
        InitializeComponent();
        this._isClear = isClear;
    }

    /// <summary>
    /// Window loading completion event
    /// </summary>
    private void Window_Login_Loaded(object sender, RoutedEventArgs e)
    {
    }

    /// <summary>
    /// Event after window content is rendered
    /// </summary>
    private async void Window_Login_ContentRendered(object sender, EventArgs e)
    {
        //Initialize WebView2
        await InitWebView2();
    }

    /// <summary>
    /// Event when the window is closed
    /// </summary>
    private void Window_Login_Closing(object sender, CancelEventArgs e)
    {
        WebView2_Main?.Dispose();

        ////////////////////////////////

        var accountWindow = new AccountWindow();

        //Transfer control of main program
        Application.Current.MainWindow = accountWindow;

        //Display the switch account window
        accountWindow.Show();
    }

    /// <summary>
    /// Initialize WebView2 login information
    /// </summary>
    private async Task InitWebView2()
    {
        try
        {
            LoggerHelper.Info("Start initializing WebView2...");

            var options = new CoreWebView2EnvironmentOptions();

            //Initialize WebView2 environment
            var env = await CoreWebView2Environment.CreateAsync(null, Globals.GetAccountCacheDir(), options);
            await WebView2_Main.EnsureCoreWebView2Async(env);

            LoggerHelper.Info("Initializing WebView2 completed...");

            // Disable Dev development tools
            WebView2_Main.CoreWebView2.Settings.AreDevToolsEnabled = false;
            // Disable right-click menu
            WebView2_Main.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            // Disable browser zoom
            WebView2_Main.CoreWebView2.Settings.IsZoomControlEnabled = false;
            // Disable display of the status bar (no url address is displayed in the lower right corner when the mouse is hovering over the link)
            WebView2_Main.CoreWebView2.Settings.IsStatusBarEnabled = false;

            // Processing of pages opened in new windows
            WebView2_Main.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            //Handling of Url changes
            WebView2_Main.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;

            // Navigation start event
            WebView2_Main.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            // Navigation completion event
            WebView2_Main.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

            // Used to change new account
            if (_isClear)
            {
                LoggerHelper.Info("Start clearing the cache of the current login account...");
                await ClearWebView2Cache();
            }
            else
            {
                LoggerHelper.Info("Starting to load the WebView2 login interface...");

                // Navigate to the specified Url
                WebView2_Main.CoreWebView2.Navigate(_host);
            }
        }
        catch (Exception ex)
        {
            LoggerHelper.Error($"WebView2 initialization exception", ex);
        }
    }

    private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        var deferral = e.GetDeferral();
        e.NewWindow = WebView2_Main.CoreWebView2;
        deferral.Complete();
    }

    private async void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
    {
        LoggerHelper.Trace("SourceChanged");

        var source = WebView2_Main.Source.ToString();
        LoggerHelper.Info($"Current WebView2 address: {source}");
        if (!source.Contains("test.pulse.ea.com"))
            return;

        LoggerHelper.Info("The player logged in successfully and started to obtain cookies...");
        var cookies = await WebView2_Main.CoreWebView2.CookieManager.GetCookiesAsync(null);
        if (cookies == null)
        {
            LoggerHelper.Warn("Successfully logged in to the account, but failed to obtain the cookie. Please try clearing the WebView2 cache.");
            return;
        }

        LoggerHelper.Info("Found cookies file, starting to traverse...");
        LoggerHelper.Info($"The number of cookies is {cookies.Count}");

        foreach (var item in cookies)
        {
            if (item.Name.Equals("remid", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    IniHelper.WriteString("Cookie", "Remid", item.Value, Globals.GetAccountIniPath());
                    LoggerHelper.Info($"Get Remid successfully: {item.Value}");
                    continue;
                }
            }

            if (item.Name.Equals("sid", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    IniHelper.WriteString("Cookie", "Sid", item.Value, Globals.GetAccountIniPath());
                    LoggerHelper.Info($"Get Sid successfully: {item.Value}");
                    continue;
                }
            }
        }

        ////////////////////////////////

        this.Close();
    }

    private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        WebView2_Main.Visibility = Visibility.Hidden;
        WebView2_Loading.Visibility = Visibility.Visible;

        LoggerHelper.Trace("NavigationStarting");
    }

    private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        WebView2_Main.Visibility = Visibility.Visible;
        WebView2_Loading.Visibility = Visibility.Hidden;

        LoggerHelper.Trace("NavigationCompleted");
    }

    /// <summary>
    /// Clear WebView2 cache
    /// </summary>
    /// <returns></returns>
    private async Task ClearWebView2Cache()
    {
        await WebView2_Main.CoreWebView2.ExecuteScriptAsync("localStorage.clear()");
        WebView2_Main.CoreWebView2.CookieManager.DeleteAllCookies();
        WebView2_Main.CoreWebView2.Navigate(_host);

        LoggerHelper.Info("Cleared WebView2 cache successfully");
    }

    /// <summary>
    /// Reload the login page
    /// </summary>
    [RelayCommand]
    private void ReloadLoginPage()
    {
        WebView2_Main.CoreWebView2.Navigate(_host);

        LoggerHelper.Info("Login page reloaded successfully");
    }
}

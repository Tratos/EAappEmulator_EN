using EAappEmulater.Extend;
using EAappEmulater.Models;
using NLog;
using NLog.Common;

namespace EAappEmulater.Views;

/// <summary>
/// Interaction logic of LogView.xaml
/// </summary>
public partial class LogView : UserControl
{
    public ObservableCollection<LogInfo> ObsCol_LogInfos { get; set; } = new();

    private const int _maxRowCount = 100;

    public LogView()
    {
        InitializeComponent();

        ToDoList();

        // Each time the control is loaded, the log scrolls to the last line
        this.Loaded += (s, e) => { ScrollToLast(); };
    }

    private void ToDoList()
    {
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            var targetResult = LogManager.Configuration.AllTargets
                .Where(t => t is NlogViewerTarget).Cast<NlogViewerTarget>();

            foreach (var target in targetResult)
            {
                target.LogReceived += LogReceived;
            }
        }
    }

    private void LogReceived(AsyncLogEventInfo logEventInfo)
    {
        var logEvent = logEventInfo.LogEvent;

        this.Dispatcher.BeginInvoke(() =>
        {
            if (_maxRowCount > 0 && ObsCol_LogInfos.Count > _maxRowCount)
                ObsCol_LogInfos.RemoveAt(0);

            var item = new LogInfo()
            {
                Time = logEvent.TimeStamp.ToString("HH:mm:ss.ffff"),
                Level = logEvent.Level.Name,
                Message = $"{logEvent.Message} {logEvent.Exception?.Message}"
            };

            ObsCol_LogInfos.Add(item);

            //Scroll the last line
            ScrollToLast();
        });
    }

    /// <summary>
    /// Scroll the log to the last line
    /// </summary>
    private void ScrollToLast()
    {
        if (ListView_Logger.Items.Count <= 0)
            return;

        ListView_Logger.SelectedIndex = ListView_Logger.Items.Count - 1;
        ListView_Logger.ScrollIntoView(ListView_Logger.SelectedItem);
    }
}

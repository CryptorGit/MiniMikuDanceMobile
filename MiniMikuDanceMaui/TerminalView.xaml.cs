using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;

namespace MiniMikuDanceMaui;

public partial class TerminalView : ContentView
{
    private int _lastIndex;
    public TerminalView()
    {
        InitializeComponent();
        LogService.LineLogged += OnLineLogged;
        var history = LogService.History;
        var textColor = (Color)Application.Current.Resources["TextColor"];
        foreach (var line in history)
        {
            LogStack.Add(new Label { Text = line, TextColor = textColor });
        }
        _lastIndex = history.Count;
    }

    private void OnLineLogged(string line)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var textColor = (Color)Application.Current.Resources["TextColor"];
            LogStack.Add(new Label { Text = line, TextColor = textColor });
            _lastIndex++;
        });
    }

    private void AddHistory()
    {
        var history = LogService.History;
        for (int i = _lastIndex; i < history.Count; i++)
        {
            var textColor = (Color)Application.Current.Resources["TextColor"];
            LogStack.Add(new Label { Text = history[i], TextColor = textColor });
        }
        _lastIndex = history.Count;
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.OldHandler != null)
        {
            LogService.LineLogged -= OnLineLogged;
        }
        if (args.NewHandler != null)
        {
            LogService.LineLogged += OnLineLogged;
            AddHistory();
        }
    }
}

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
        var history = LogService.History;
        foreach (var line in history)
        {
            LogStack.Add(new Label { Text = line, TextColor = Colors.White });
        }
        _lastIndex = history.Count;
        LogService.LineLogged += OnLineLogged;
    }

    private void OnLineLogged(string line)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogStack.Add(new Label { Text = line, TextColor = Colors.White });
            _lastIndex++;
        });
    }

    private void AddHistory()
    {
        var history = LogService.History;
        for (int i = _lastIndex; i < history.Count; i++)
        {
            LogStack.Add(new Label { Text = history[i], TextColor = Colors.White });
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
            AddHistory();
            LogService.LineLogged += OnLineLogged;
        }
    }
}

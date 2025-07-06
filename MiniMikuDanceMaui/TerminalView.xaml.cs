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
        foreach (var line in LogService.History)
        {
            LogStack.Add(new Label { Text = line, TextColor = Colors.White });
        }
        _lastIndex = LogService.History.Count;
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
        for (int i = _lastIndex; i < LogService.History.Count; i++)
        {
            LogStack.Add(new Label { Text = LogService.History[i], TextColor = Colors.White });
        }
        _lastIndex = LogService.History.Count;
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

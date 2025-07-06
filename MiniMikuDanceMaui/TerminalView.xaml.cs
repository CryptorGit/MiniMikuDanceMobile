using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;

namespace MiniMikuDanceMaui;

public partial class TerminalView : ContentView
{
    public TerminalView()
    {
        InitializeComponent();
        foreach (var line in LogService.History)
        {
            LogStack.Add(new Label { Text = line, TextColor = Colors.White });
        }
        LogService.LineLogged += OnLineLogged;
    }

    private void OnLineLogged(string line)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogStack.Add(new Label { Text = line, TextColor = Colors.White });
        });
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.OldHandler == null) return;
        LogService.LineLogged -= OnLineLogged;
    }
}

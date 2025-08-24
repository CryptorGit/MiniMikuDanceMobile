using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace MiniMikuDanceMaui;

internal static class ErrorNotifier
{
    private static readonly ILogger Logger = AppLogger.Create<ErrorNotifier>();

    public static void Notify(string message, Exception ex)
    {
        Logger.LogError(ex, message);
        var crashPath = Path.Combine(AppContext.BaseDirectory, "crash_log.txt");
        try
        {
            File.AppendAllText(crashPath, $"[{DateTime.Now:O}] {message} {ex}{Environment.NewLine}");
        }
        catch
        {
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                var steps = await page.DisplayPromptAsync(
                    "エラー", "エラーが発生しました。再現手順を入力してください。", "送信", "キャンセル");
                if (!string.IsNullOrWhiteSpace(steps))
                {
                    try
                    {
                        File.AppendAllText(crashPath,
                            $"[STEPS {DateTime.Now:O}] {steps}{Environment.NewLine}");
                    }
                    catch
                    {
                    }
                }
            }
        });
    }
}

using System;
using Microsoft.Maui.Controls.Xaml;
using MauiIcons.Material;

namespace MiniMikuDanceMaui.Extensions;

[AcceptEmptyServiceProvider]
public class MaterialExtension : IMarkupExtension
{
    public MaterialIcons Icon { get; set; }

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var extension = new MauiIcons.Material.MaterialExtension
        {
            Icon = Icon
        };
        return ((IMarkupExtension)extension).ProvideValue(serviceProvider);
    }
}

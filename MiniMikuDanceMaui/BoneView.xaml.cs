namespace MiniMikuDanceMaui;

public partial class BoneView : ContentView
{
    public BoneView()
    {
        InitializeComponent();
    }

    public void SetBones(IEnumerable<string> bones)
    {
        BoneList.Children.Clear();
        var textColor = ResourceHelper.GetColor("TextColor", Colors.Black);
        foreach (var name in bones)
        {
            BoneList.Children.Add(new Label { Text = name, TextColor = textColor });
        }
    }
}

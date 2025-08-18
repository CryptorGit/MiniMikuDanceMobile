using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MiniMikuDance.Import;

namespace MiniMikuDanceMaui.DebugScenes;

public partial class ImporterAuditScene : ContentView
{
    private ModelData? _model;

    public ImporterAuditScene()
    {
        InitializeComponent();
    }

    public void SetModel(ModelData? model)
    {
        _model = model;
        UpdateView();
    }

    private void UpdateView()
    {
        if (_model == null)
        {
            BoneList.ItemsSource = null;
            MorphList.ItemsSource = null;
            RigidBodyList.ItemsSource = null;
            JointList.ItemsSource = null;
            return;
        }

        BoneList.ItemsSource = _model.Bones.Select(b => b.Name);
        MorphList.ItemsSource = _model.Morphs.Select(m => m.Name);
        RigidBodyList.ItemsSource = _model.RigidBodies.Select(r => r.Name);
        JointList.ItemsSource = _model.Joints.Select(j => j.Name);
    }

    private async void OnExportCsvClicked(object sender, EventArgs e)
    {
        if (_model == null)
        {
            await DisplayAlert("Error", "モデルが読み込まれていません", "OK");
            return;
        }

        var path = Path.Combine(FileSystem.CacheDirectory, "ImporterAudit.csv");

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Bones");
            foreach (var b in _model.Bones)
            {
                sb.AppendLine(b.Name);
            }
            sb.AppendLine();
            sb.AppendLine("Morphs");
            foreach (var m in _model.Morphs)
            {
                sb.AppendLine(m.Name);
            }
            sb.AppendLine();
            sb.AppendLine("RigidBodies");
            foreach (var r in _model.RigidBodies)
            {
                sb.AppendLine(r.Name);
            }
            sb.AppendLine();
            sb.AppendLine("Joints");
            foreach (var j in _model.Joints)
            {
                sb.AppendLine(j.Name);
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            await DisplayAlert("書き出し完了", path, "OK");
        }
        catch (Exception ex)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            await DisplayAlert("書き込みに失敗しました", ex.Message, "OK");
        }
    }
}

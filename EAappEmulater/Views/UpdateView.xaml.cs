using EAappEmulater.Helper;

namespace EAappEmulater.Views;

/// <summary>
/// Interaction logic of UpdateView.xaml
/// </summary>
public partial class UpdateView : UserControl
{
    public UpdateView()
    {
        InitializeComponent();

        ToDoList();
    }

    private void ToDoList()
    {
        TextBoxHint_UpdateNotes.Text = FileHelper.GetEmbeddedResourceText("Misc.UpdateNotes.txt");
    }
}

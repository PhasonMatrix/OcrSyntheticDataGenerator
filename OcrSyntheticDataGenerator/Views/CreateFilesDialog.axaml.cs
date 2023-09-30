using Avalonia.Controls;
using Avalonia.Interactivity;
using OcrSyntheticDataGenerator.ViewModels;

namespace OcrSyntheticDataGenerator.Views;

public partial class CreateFilesDialog : Window
{
    public CreateFilesDialog(
        int linesProbability,
        int backgroundProbability,
        int noiseProbability,
        int blurProbability ,
        int pixelateProbability,
        int invertImageProbability)
    {
        InitializeComponent();
        DataContext = new CreateFilesViewModel(
            linesProbability,
            backgroundProbability,
            noiseProbability,
            blurProbability,
            pixelateProbability,
            invertImageProbability);
    }


    private void CloseButtonClick(object sender, RoutedEventArgs args)
    {
        Close();
    }

}

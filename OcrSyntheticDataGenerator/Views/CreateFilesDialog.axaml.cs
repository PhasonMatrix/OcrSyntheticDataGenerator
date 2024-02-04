using Avalonia.Controls;
using Avalonia.Interactivity;
using OcrSyntheticDataGenerator.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using static OcrSyntheticDataGenerator.ImageGeneration.ImageAndLabelGeneratorBase;

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


        List<string> textLayoutTypeComboBoxOptions = new List<string>();
        foreach (LayoutFileType fileType in Enum.GetValues(typeof(LayoutFileType)))
        {
            textLayoutTypeComboBoxOptions.Add(fileType.GetType().GetMember(fileType.ToString())[0].GetCustomAttribute<DescriptionAttribute>().Description);
        }
        LayoutTypeComboBox.ItemsSource = textLayoutTypeComboBoxOptions;
        LayoutTypeComboBox.SelectedIndex = 0;


        List<string> dataFileTypeComboBoxOptions = new List<string>();
        foreach (DataFileType fileType in Enum.GetValues(typeof(DataFileType)))
        {
            dataFileTypeComboBoxOptions.Add(fileType.GetType().GetMember(fileType.ToString())[0].GetCustomAttribute<DescriptionAttribute>().Description);
        }
        DataFileTypeComboBox.ItemsSource = dataFileTypeComboBoxOptions;
        DataFileTypeComboBox.SelectedIndex = 0;

    }

    public CreateFilesDialog() // parameterless ctor for designer
    {
        InitializeComponent();
    }


    private void CloseButtonClick(object sender, RoutedEventArgs args)
    {
        Close();
    }

}

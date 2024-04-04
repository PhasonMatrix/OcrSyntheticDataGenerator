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
        int pageImageWidth,
        int pageImageHeight,
        int linesProbability,
        int backgroundProbability,
        int noiseProbability,
        int blurProbability ,
        int pixelateProbability,
        int invertImageProbability)
    {
        InitializeComponent();
        DataContext = new CreateFilesViewModel(
            pageImageWidth,
            pageImageHeight,
            linesProbability,
            backgroundProbability,
            noiseProbability,
            blurProbability,
            pixelateProbability,
            invertImageProbability);


        List<string> textLayoutTypeComboBoxOptions = new List<string>();
        foreach (LayoutFileType layoutType in Enum.GetValues(typeof(LayoutFileType)))
        {
            textLayoutTypeComboBoxOptions.Add(layoutType.GetType().GetMember(layoutType.ToString())[0].GetCustomAttribute<DescriptionAttribute>().Description);
        }
        LayoutTypeComboBox.ItemsSource = textLayoutTypeComboBoxOptions;
        LayoutTypeComboBox.SelectedIndex = 0;


        List<string> dataFileTypeComboBoxOptions = new List<string>();
        foreach (DataFileType dataFileType in Enum.GetValues(typeof(DataFileType)))
        {
            dataFileTypeComboBoxOptions.Add(dataFileType.GetType().GetMember(dataFileType.ToString())[0].GetCustomAttribute<DescriptionAttribute>().Description);
        }
        DataFileTypeComboBox.ItemsSource = dataFileTypeComboBoxOptions;
        DataFileTypeComboBox.SelectedIndex = 0;


        List<string> characterNormalisationTypeComboBoxOptions = new List<string>();
        foreach (CharacterBoxNormalisationType normalisationType in Enum.GetValues(typeof(CharacterBoxNormalisationType)))
        {
            characterNormalisationTypeComboBoxOptions.Add(normalisationType.GetType().GetMember(normalisationType.ToString())[0].GetCustomAttribute<DescriptionAttribute>().Description);
        }
        CharacterImageNormalisationComboBox.ItemsSource = characterNormalisationTypeComboBoxOptions;
        CharacterImageNormalisationComboBox.SelectedIndex = 0;


    }

    public CreateFilesDialog() // parameterless ctor for designer
    {
        InitializeComponent();
    }


    private void CloseButtonClick(object sender, RoutedEventArgs args)
    {
        Close();
    }

    private void Binding(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
}

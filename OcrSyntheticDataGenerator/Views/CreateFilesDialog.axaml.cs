using Avalonia.Controls;
using Avalonia.Interactivity;
using OcrSyntheticDataGenerator.Util;
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


        List<string> textLayoutTypeComboBoxOptions = EnumUtils.GetDescriptions<LayoutFileType>();
        LayoutTypeComboBox.ItemsSource = textLayoutTypeComboBoxOptions;
        LayoutTypeComboBox.SelectedIndex = 0;

        List<string> dataFileTypeComboBoxOptions = EnumUtils.GetDescriptions<DataFileType>();
        DataFileTypeComboBox.ItemsSource = dataFileTypeComboBoxOptions;
        DataFileTypeComboBox.SelectedIndex = 0;

        List<string> characterNormalisationTypeComboBoxOptions = EnumUtils.GetDescriptions<CharacterBoxNormalisationType>();
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

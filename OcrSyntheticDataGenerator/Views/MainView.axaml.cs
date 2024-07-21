using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Diagnostics;
using Avalonia.Input;
using Avalonia;
using static OcrSyntheticDataGenerator.ImageGeneration.ImageAndLabelGeneratorBase;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Reflection;
using OcrSyntheticDataGenerator.Util;
using static OcrSyntheticDataGenerator.ViewModels.MainViewModel;

namespace OcrSyntheticDataGenerator.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        List<string> textLayoutTypeComboBoxOptions = EnumUtils.GetDescriptions<LayoutFileType>();
        LayoutTypeComboBox.ItemsSource = textLayoutTypeComboBoxOptions;
        LayoutTypeComboBox.SelectedIndex = 0;

        List<string> displayImageTypeComboBoxOptions = EnumUtils.GetDescriptions<DisplayImageType>();
        LeftImageComboBox.ItemsSource = displayImageTypeComboBoxOptions;
        LeftImageComboBox.SelectedIndex = 0;
        RightImageComboBox.ItemsSource = displayImageTypeComboBoxOptions;
        RightImageComboBox.SelectedIndex = 0;


    }

    private void Binding(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
    }
}

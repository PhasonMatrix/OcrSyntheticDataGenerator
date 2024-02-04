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

namespace OcrSyntheticDataGenerator.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        List<string> textLayoutTypeComboBoxOptions = new List<string>();
        foreach (LayoutFileType fileType in Enum.GetValues(typeof(LayoutFileType)))
        {
            textLayoutTypeComboBoxOptions.Add(fileType.GetType().GetMember(fileType.ToString())[0].GetCustomAttribute<DescriptionAttribute>().Description);
        }
        LayoutTypeComboBox.ItemsSource = textLayoutTypeComboBoxOptions;
        LayoutTypeComboBox.SelectedIndex = 0;
    }


}

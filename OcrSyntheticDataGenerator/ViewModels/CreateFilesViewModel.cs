using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using MsBox.Avalonia.Enums;
using OcrSyntheticDataGenerator.ImageGeneration;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static OcrSyntheticDataGenerator.ImageGeneration.ImageAndLabelGeneratorBase;

namespace OcrSyntheticDataGenerator.ViewModels;

public class CreateFilesViewModel: ViewModelBase
{
    private int _linesProbability;
    private int _backgroundProbability;
    private int _noiseProbability;
    private int _blurProbability;
    private int _pixelateProbability;
    private int _invertImageProbability;
    private string _comboBoxSaveDataFileType = "None";
    private double _progressBarValue;
    private string _status;
    private bool _isRunning;
    private int _numberOfFilesToGenerate = 1000;
    private string _outputDirectory = "C:\\OCR Source Images\\Generated Images and Labels\\";

    private Cursor _pointerCursor = Cursor.Default;

    public int NumberOfFilesToGenerate
    {
        get => _numberOfFilesToGenerate;
        private set => this.RaiseAndSetIfChanged(ref _numberOfFilesToGenerate, value);
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        private set => this.RaiseAndSetIfChanged(ref _outputDirectory, value);
    }

    public string ComboBoxSaveDataFileType
    {
        get => _comboBoxSaveDataFileType;
        private set => this.RaiseAndSetIfChanged(ref _comboBoxSaveDataFileType, value);
    }

    public Cursor PointerCursor
    {
        get => _pointerCursor;
        private set => this.RaiseAndSetIfChanged(ref _pointerCursor, value);
    }

    public double ProgressBarValue
    {
        get => _progressBarValue;
        private set => this.RaiseAndSetIfChanged(ref _progressBarValue, value);
    }

    public string StatusMessage 
    {
        get { return _status;  }
        private set { this.RaiseAndSetIfChanged(ref _status, value); } 
    }

    public bool IsRunning
    {
        get { return _isRunning; }
        private set { this.RaiseAndSetIfChanged(ref _isRunning, value); }
    }



    public CreateFilesViewModel() // parameterless ctor for designer
    {}

    public CreateFilesViewModel(
        int linesProbability,
        int backgroundProbability,
        int noiseProbability,
        int blurProbability,
        int pixelateProbability,
        int invertImageProbability)
    {
        _linesProbability = linesProbability;
        _backgroundProbability = backgroundProbability;
        _noiseProbability = noiseProbability;
        _blurProbability = blurProbability;
        _pixelateProbability = pixelateProbability;
        _invertImageProbability = invertImageProbability;
    }


    public async void GenerateFilesButton_ClickCommand()
    {
        // check output dir exists
        if (!Directory.Exists(OutputDirectory))
        {
            var result = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard("Wrong!", "Output directory does not exist. Try again.", ButtonEnum.Ok).ShowAsync();
            return;
        }

        IsRunning = true;
        SetMouseCursorToWaiting();

        DataFileType comboBoxSelection = DataFileType.None;
        foreach(DataFileType value in Enum.GetValues(typeof(DataFileType)))
        {
            if (_comboBoxSaveDataFileType == value.GetType().GetMember(value.ToString())[0].GetCustomAttribute<DescriptionAttribute>().Description)
            {
                comboBoxSelection = value;
                break;
            }
        }

        int total = _numberOfFilesToGenerate; // make a copy in case the user changes number in UI control while we're running the loop.
        int i = 0;
        for (; i < total; i++)
        {
            if (!IsRunning) // user can cancel while loop is running.
            {
                break;
            }
            ProgressBarValue = (double)i / total * 100.0;
            StatusMessage = $"Status: Generating {i} of {total} files ...";
            await Task.Run(() => GenerateFiles(comboBoxSelection));
        }

        if (i == total) // we reached the end without user cancelling
        {
            ProgressBarValue = 100;
            StatusMessage = "Status: Done";
        }
        SetMouseCursorToDefault();
        IsRunning = false;
    }


    public async void CancelButton_ClickCommand()
    {
        IsRunning = false;
        ProgressBarValue = 0;
        StatusMessage = "Status: Cancelled";
    }


    private void GenerateFiles(DataFileType dataFileType)
    {
        var imageAndLabelsGuid = Guid.NewGuid();
        // create a new generator object each time
        ImageAndLabelGeneratorBase generator = new ScatteredTextGenerator(650, 800);
        generator.BackgroundProbability = _backgroundProbability;
        generator.LinesProbability = _linesProbability;
        generator.NoiseProbability = _noiseProbability;
        generator.BlurProbability = _blurProbability;
        generator.PixelateProbability = _pixelateProbability;
        generator.InvertProbability = _invertImageProbability;
        generator.Generate();

        string imageFilename = $"{imageAndLabelsGuid}.png";
        string labelsFilename = $"{imageAndLabelsGuid}_labels.png";
        string bboxDataFilenameWithoutExtension = $"{imageAndLabelsGuid}";
        var imagesDirectoryPath = Path.Combine(OutputDirectory, "images");
        var labelsDirectoryPath = Path.Combine(OutputDirectory, "labels");
        var bboxDataDirectoryPath = Path.Combine(OutputDirectory, "bounding_boxes");


        if (!Directory.Exists(imagesDirectoryPath))
        {
            Directory.CreateDirectory(imagesDirectoryPath);
        }
        if (!Directory.Exists(labelsDirectoryPath))
        {
            Directory.CreateDirectory(labelsDirectoryPath);
        }
        if (!Directory.Exists(bboxDataDirectoryPath))
        {
            Directory.CreateDirectory(bboxDataDirectoryPath);
        }

        string imageFileSavePath = Path.Combine(imagesDirectoryPath, imageFilename);
        string labelsFileSavePath = Path.Combine(labelsDirectoryPath, labelsFilename);
        string bboxFileSavePath = Path.Combine(bboxDataDirectoryPath, bboxDataFilenameWithoutExtension);

        generator.SaveTextImage(imageFileSavePath, SKEncodedImageFormat.Png);
        generator.SaveLabelImage(labelsFileSavePath, SKEncodedImageFormat.Png);
        generator.SaveBoudingBoxesToTextFile(bboxFileSavePath, dataFileType);
    }



    private void SetMouseCursorToWaiting()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow.Cursor = new Cursor(StandardCursorType.Wait);
        }
    }

    private void SetMouseCursorToDefault()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow.Cursor = Cursor.Default;
        }
    }



}

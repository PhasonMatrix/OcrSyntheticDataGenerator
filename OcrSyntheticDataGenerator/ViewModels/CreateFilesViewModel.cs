using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using MsBox.Avalonia.Enums;
using OcrSyntheticDataGenerator.ImageGeneration;
using OcrSyntheticDataGenerator.Util;
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
    private int _pageImageWidth;
    private int _pageImageHeight;
    private string _comboBoxSaveDataFileType = "None";
    private string _comboBoxTextLayoutType = "Scattered Text";
    private string _comboBoxCharacterBoxNormalisationType = "Include surrounding pixels";
    private bool _checkBoxOutputPageImageFiles = true;
    private bool _checkBoxOutputCharacterImageFiles;
    private double _progressBarValue;
    private string _status;
    private bool _isRunning;
    private int _numberOfFilesToGenerate = 1000;
    private string _pageImageOutputDirectory = "C:\\OCR Source Images\\Generated Images and Labels\\";
    private string _characterImageOutputDirectory = "C:\\OCR Source Images\\Generated Character Images\\";

    private Cursor _pointerCursor = Cursor.Default;

    public int NumberOfFilesToGenerate
    {
        get => _numberOfFilesToGenerate;
        private set => this.RaiseAndSetIfChanged(ref _numberOfFilesToGenerate, value);
    }

    public string PageImageOutputDirectory
    {
        get => _pageImageOutputDirectory;
        private set => this.RaiseAndSetIfChanged(ref _pageImageOutputDirectory, value);
    }

    public string CharacterImageOutputDirectory
    {
        get => _characterImageOutputDirectory;
        private set => this.RaiseAndSetIfChanged(ref _characterImageOutputDirectory, value);
    }

    public string ComboBoxTextLayoutType
    {
        get => _comboBoxTextLayoutType;
        private set => this.RaiseAndSetIfChanged(ref _comboBoxTextLayoutType, value);
    }

    public string ComboBoxSaveDataFileType
    {
        get => _comboBoxSaveDataFileType;
        private set => this.RaiseAndSetIfChanged(ref _comboBoxSaveDataFileType, value);
    }


    public string ComboBoxCharacterBoxNormalisationType
    {
        get => _comboBoxCharacterBoxNormalisationType;
        private set => this.RaiseAndSetIfChanged(ref _comboBoxCharacterBoxNormalisationType, value);
    }


    public bool CheckBoxOutputPageImageFiles
    {
        get { return _checkBoxOutputPageImageFiles; }
        private set { this.RaiseAndSetIfChanged(ref _checkBoxOutputPageImageFiles, value); }
    }


    public bool CheckBoxOutputCharacterImageFiles
    {
        get { return _checkBoxOutputCharacterImageFiles; }
        private set { this.RaiseAndSetIfChanged(ref _checkBoxOutputCharacterImageFiles, value); }
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
        int pageImageWidth,
        int pageImageHeight,
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
        _pageImageWidth = pageImageWidth;
        _pageImageHeight = pageImageHeight;
    }


    public async void GenerateFilesButton_ClickCommand()
    {
        // check output dir exists
        if (!Directory.Exists(PageImageOutputDirectory))
        {
            var result = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard("Wrong!", "Output directory does not exist. Try again.", ButtonEnum.Ok).ShowAsync();
            return;
        }

        IsRunning = true;
        SetMouseCursorToWaiting();


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
            await Task.Run(() => GenerateFiles());
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


    private void GenerateFiles()
    {
        if (!_checkBoxOutputPageImageFiles && !_checkBoxOutputCharacterImageFiles)
        {
            return;
        }

        var imageAndLabelsGuid = Guid.NewGuid();


        LayoutFileType layoutTypeComboBoxSelection = EnumUtils.ParseDescription<LayoutFileType>(_comboBoxTextLayoutType);
        DataFileType dataFileTypeComboBoxSelection = EnumUtils.ParseDescription<DataFileType>(_comboBoxSaveDataFileType);
        CharacterBoxNormalisationType characterBoxNormalisationType = EnumUtils.ParseDescription<CharacterBoxNormalisationType>(_comboBoxCharacterBoxNormalisationType);


        // create a new generator object each time
        ImageAndLabelGeneratorBase generator = null; // = new TableGenerator(700, 800); ; // new TableGenerator(650, 800);

        switch (layoutTypeComboBoxSelection)
        {

            case LayoutFileType.ScatteredText:
                generator = new ScatteredTextGenerator(_pageImageWidth, _pageImageHeight);
                break;
            case LayoutFileType.Paragraph:
                generator = new ParagraphGenerator(_pageImageWidth, _pageImageHeight);
                break;
            case LayoutFileType.Table:
                generator = new TableGenerator(_pageImageWidth, _pageImageHeight);
                break;
        }
        generator.BackgroundProbability = _backgroundProbability;
        generator.LinesProbability = _linesProbability;
        generator.NoiseProbability = _noiseProbability;
        generator.BlurProbability = _blurProbability;
        generator.PixelateProbability = _pixelateProbability;
        generator.InvertProbability = _invertImageProbability;
        generator.GenerationPipeline(true);



        if (_checkBoxOutputPageImageFiles)
        {
            string imageFilename = $"{imageAndLabelsGuid}.png";
            string labelsFilename = $"{imageAndLabelsGuid}_labels.png";
            string bboxDataFilenameWithoutExtension = $"{imageAndLabelsGuid}";
            var imagesDirectoryPath = Path.Combine(PageImageOutputDirectory, "images");
            var labelsDirectoryPath = Path.Combine(PageImageOutputDirectory, "labels");
            var bboxDataDirectoryPath = Path.Combine(PageImageOutputDirectory, "bounding_boxes");

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
            generator.SaveBoudingBoxesToTextFile(bboxFileSavePath, dataFileTypeComboBoxSelection);
        }



        if (_checkBoxOutputCharacterImageFiles)
        {
            generator.SaveCharacterImageFiles(_characterImageOutputDirectory, characterBoxNormalisationType);
        }


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

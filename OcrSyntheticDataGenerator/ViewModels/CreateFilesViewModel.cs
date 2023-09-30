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
using System.Runtime.CompilerServices;
using static OcrSyntheticDataGenerator.ImageGeneration.ImageAndLabelGenerator;

namespace OcrSyntheticDataGenerator.ViewModels;

public class CreateFilesViewModel: ViewModelBase
{
    private int _linesProbability;
    private int _backgroundProbability;
    private int _noiseProbability;
    private int _blurProbability;
    private int _pixelateProbability;
    private int _invertImageProbability;


    private int _numberOfFilesToGenerate = 1000;
    private string _outputDirectory = "C:\\OCR Source Images\\Generated Images and Labels\\";

    private Cursor _pointerCursor = new Cursor(StandardCursorType.Help);

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


    public Cursor PointerCursor
    {
        get => _pointerCursor;
        private set => this.RaiseAndSetIfChanged(ref _pointerCursor, value);
    }



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

        SetMouseCursorToWaiting();

        for (int i = 0; i < _numberOfFilesToGenerate; i++)
        {
            var imageAndLabelsGuid = Guid.NewGuid();
            // create a new generator object each time
            ImageAndLabelGenerator generator = new ImageAndLabelGenerator(650, 800);
            generator.BackgroundProbability = _backgroundProbability;
            generator.LinesProbability = _linesProbability;
            generator.NoiseProbability = _noiseProbability;
            generator.BlurProbability = _blurProbability;
            generator.PixelateProbability = _pixelateProbability;
            generator.InvertProbability = _invertImageProbability;
            generator.GenerateImages();

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
            generator.SaveBoudingBoxesToTextFile(bboxFileSavePath, DataFileType.JsonWordsAndCharacters);

        }

        SetMouseCursorToDefault();
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

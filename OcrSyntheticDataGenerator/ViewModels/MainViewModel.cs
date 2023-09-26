using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using SkiaSharp;
using OcrSyntheticDataGenerator.ImageGeneration;
using System;
using System.IO;
using static OcrSyntheticDataGenerator.ImageGeneration.ImageAndLabelGenerator;

namespace OcrSyntheticDataGenerator.ViewModels;

public class MainViewModel : ViewModelBase
{
    
    private Bitmap? _leftBitmap;
    private Bitmap? _rightBitmap;
    private int _lineProbability = 10;
    private int _BackgroundProbability = 10;
    private int _noiseProbability = 10;

    private int _blurProbability = 10;
    private int _pixelateProbability = 10;
    private int _invertImageProbability = 10;

    private int _numberOfFilesToGenerate = 1000;
    private string _outputDirectory = "C:\\OCR Source Images\\Generated Images and Labels\\";


    private Cursor _pointerCursor = new Cursor(StandardCursorType.Help);




    public Bitmap? LeftBitmap
    {
        get => _leftBitmap;
        private set => this.RaiseAndSetIfChanged(ref _leftBitmap, value);
    }

    public Bitmap? RightBitmap
    {
        get => _rightBitmap;
        private set => this.RaiseAndSetIfChanged(ref _rightBitmap, value);
    }


    public int LinesProbability
    {
        get => _lineProbability;
        private set => this.RaiseAndSetIfChanged(ref _lineProbability, value);
    }

    public int BackgroundProbability
    {
        get => _BackgroundProbability;
        private set => this.RaiseAndSetIfChanged(ref _BackgroundProbability, value);
    }

    public int NoiseProbability
    {
        get => _noiseProbability;
        private set => this.RaiseAndSetIfChanged(ref _noiseProbability, value);
    }

    public int BlurProbability
    {
        get => _blurProbability;
        private set => this.RaiseAndSetIfChanged(ref _blurProbability, value);
    }

    public int PixelateProbability
    {
        get => _pixelateProbability;
        private set => this.RaiseAndSetIfChanged(ref _pixelateProbability, value);
    }

    public int InvertImageProbability
    {
        get => _invertImageProbability;
        private set => this.RaiseAndSetIfChanged(ref _invertImageProbability, value);
    }

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


    public void PreviewButton_ClickCommand()
    {
        SetMouseCursorToWaiting();

        ImageAndLabelGenerator generator = new ImageAndLabelGenerator(650, 800);
        generator.BackgroundProbability = BackgroundProbability;
        generator.LinesProbability = LinesProbability;
        generator.NoiseProbability = NoiseProbability;
        generator.BlurProbability = BlurProbability;
        generator.PixelateProbability = PixelateProbability;
        generator.InvertProbability = InvertImageProbability;

        generator.GenerateImages();

        LeftBitmap = SKBitmapToAvaloniaBitmap(generator.TextImage);
        RightBitmap = SKBitmapToAvaloniaBitmap(generator.HeatMapImage);

        SetMouseCursorToDefault();
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
            generator.BackgroundProbability = BackgroundProbability;
            generator.LinesProbability = LinesProbability;
            generator.NoiseProbability = NoiseProbability;
            generator.BlurProbability = BlurProbability;
            generator.PixelateProbability = PixelateProbability;
            generator.InvertProbability = InvertImageProbability;
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




    public Avalonia.Media.Imaging.Bitmap SKBitmapToAvaloniaBitmap(SKBitmap skBitmap)
    {
        SKData data = skBitmap.Encode(SKEncodedImageFormat.Png, 100);
        using (Stream stream = data.AsStream())
        {
            return new Avalonia.Media.Imaging.Bitmap(stream);
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

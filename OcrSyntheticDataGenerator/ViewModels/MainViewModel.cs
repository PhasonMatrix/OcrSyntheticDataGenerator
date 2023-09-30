﻿using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using SkiaSharp;
using OcrSyntheticDataGenerator.ImageGeneration;
using System;
using System.IO;
using static OcrSyntheticDataGenerator.ImageGeneration.ImageAndLabelGenerator;
using OcrSyntheticDataGenerator.Views;

namespace OcrSyntheticDataGenerator.ViewModels;

public class MainViewModel : ViewModelBase
{
    
    private Bitmap? _leftBitmap;
    private Bitmap? _rightBitmap;
    private int _linesProbability = 10;
    private int _backgroundProbability = 10;
    private int _noiseProbability = 10;

    private int _blurProbability = 10;
    private int _pixelateProbability = 10;
    private int _invertImageProbability = 10;

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
        get => _linesProbability;
        private set => this.RaiseAndSetIfChanged(ref _linesProbability, value);
    }

    public int BackgroundProbability
    {
        get => _backgroundProbability;
        private set => this.RaiseAndSetIfChanged(ref _backgroundProbability, value);
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
        CreateFilesDialog dialog = new CreateFilesDialog(
            _linesProbability,
            _backgroundProbability,
            _noiseProbability,
            _blurProbability,
            _pixelateProbability,
            _invertImageProbability);

        dialog.Show();
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

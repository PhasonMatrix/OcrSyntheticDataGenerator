using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using SkiaSharp;
using OcrSyntheticDataGenerator.ImageGeneration;
using System;
using System.IO;
using static OcrSyntheticDataGenerator.ImageGeneration.ImageAndLabelGeneratorBase;
using OcrSyntheticDataGenerator.Views;
using System.ComponentModel;
using System.Reflection;
using OcrSyntheticDataGenerator.Util;
using Avalonia.Controls;
using System.Reflection.Emit;

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


    private int _pageImageWidth = 512;
    private int _pageImageHeight = 512;

    private string _comboBoxTextLayoutType = "Scattered Text";
    private string _comboBoxLeftImageSelection = "Bounding Box";
    private string _comboBoxRightImageSelection = "Labels";


    public enum DisplayImageType
    {
        [Description("Text")] Text,
        [Description("Bounding Box")] BoundingBox,
        [Description("Labels")] Labels,
        [Description("Heat Map")] HeatMap,
    };


    private ImageAndLabelGeneratorBase _generator = null;
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


    public string ComboBoxTextLayoutType
    {
        get => _comboBoxTextLayoutType;
        private set => this.RaiseAndSetIfChanged(ref _comboBoxTextLayoutType, value);
    }

    public string ComboBoxLeftImageSelection
    {
        get => _comboBoxLeftImageSelection;
        private set
        {
            this.RaiseAndSetIfChanged(ref _comboBoxLeftImageSelection, value);
            DisplayLeftImage();
        }
    }

    public string ComboBoxRightImageSelection
    {
        get => _comboBoxRightImageSelection;
        private set
        {
            this.RaiseAndSetIfChanged(ref _comboBoxRightImageSelection, value);
            DisplayRightImage();
        }
    }


    public Cursor PointerCursor
    {
        get => _pointerCursor;
        private set => this.RaiseAndSetIfChanged(ref _pointerCursor, value);
    }


    public void PreviewButton_ClickCommand()
    {
        SetMouseCursorToWaiting();

        LayoutFileType layoutTypeComboBoxSelection = EnumUtils.ParseDescription<LayoutFileType>(_comboBoxTextLayoutType);

        _generator = null;
        
        switch (layoutTypeComboBoxSelection) {

            case LayoutFileType.ScatteredText:
                _generator = new ScatteredTextGenerator(_pageImageWidth, _pageImageHeight); 
                break;
            case LayoutFileType.Paragraph:
                _generator = new ParagraphGenerator(_pageImageWidth, _pageImageHeight);
                break;
            case LayoutFileType.Table:
                _generator = new TableGenerator(_pageImageWidth, _pageImageHeight);
                break;
        }

        _generator.BackgroundProbability = BackgroundProbability;
        _generator.LinesProbability = LinesProbability;
        _generator.NoiseProbability = NoiseProbability;
        _generator.BlurProbability = BlurProbability;
        _generator.PixelateProbability = PixelateProbability;
        _generator.InvertProbability = InvertImageProbability;

        _generator.GenerationPipeline(false);

        DisplayLeftImage();
        DisplayRightImage();

        SetMouseCursorToDefault();
    }


    private void DisplayLeftImage()
    {
        if (_generator == null)
        {
            return;
        }
        switch (EnumUtils.ParseDescription<DisplayImageType>(ComboBoxLeftImageSelection))
        {
            case DisplayImageType.Text:
                LeftBitmap = SKBitmapToAvaloniaBitmap(_generator.TextImage);
                break;
            case DisplayImageType.Labels:
                LeftBitmap = SKBitmapToAvaloniaBitmap(_generator.LabelImage);
                break;
            case DisplayImageType.HeatMap:
                LeftBitmap = SKBitmapToAvaloniaBitmap(_generator.HeatMapImage);
                break;
            case DisplayImageType.BoundingBox:
                LeftBitmap = SKBitmapToAvaloniaBitmap(_generator.BoundingBoxImage);
                break;
        }
    }

    private void DisplayRightImage()
    {
        if (_generator == null)
        {
            return;
        }
        switch (EnumUtils.ParseDescription<DisplayImageType>(ComboBoxRightImageSelection))
        {
            case DisplayImageType.Text:
                RightBitmap = SKBitmapToAvaloniaBitmap(_generator.TextImage);
                break;
            case DisplayImageType.Labels:
                RightBitmap = SKBitmapToAvaloniaBitmap(_generator.LabelImage);
                break;
            case DisplayImageType.HeatMap:
                RightBitmap = SKBitmapToAvaloniaBitmap(_generator.HeatMapImage);
                break;
            case DisplayImageType.BoundingBox:
                RightBitmap = SKBitmapToAvaloniaBitmap(_generator.BoundingBoxImage);
                break;
        }
    }



    public async void GenerateFilesButton_ClickCommand()
    {
        CreateFilesDialog dialog = new CreateFilesDialog(
            _pageImageWidth,
            _pageImageHeight,
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

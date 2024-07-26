﻿using FoliCon.Models.Constants;
using FoliCon.Models.Data;
using FoliCon.Modules.Configuration;
using FoliCon.Modules.Extension;
using FoliCon.Modules.IGDB;
using FoliCon.Modules.TMDB;
using FoliCon.Modules.UI;
using FoliCon.Modules.utils;
using NLog;
using Collection = TMDbLib.Objects.Collections.Collection;
using Logger = NLog.Logger;

namespace FoliCon.ViewModels;

public class PosterPickerViewModel : BindableBase, IDialogAware
{
    private const string PosterPathMessage = "Poster Path: {PosterPath}";
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    #region Variables
    private string _title = "";
    private bool _stopSearch;
    private int _index;
    private string _busyContent = LangProvider.GetLang("LoadingPosters");
    private bool _isBusy;
    public event Action<IDialogResult> RequestClose;
    private ResultResponse _result;
    private int _totalPosters;
    private bool _isPickedById;
    #endregion

    #region Properties
    public int Index { get => _index; set => SetProperty(ref _index, value); }
    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public bool StopSearch { get => _stopSearch; set => SetProperty(ref _stopSearch, value); }
    public string BusyContent { get => _busyContent; set => SetProperty(ref _busyContent, value); }
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
    public ResultResponse Result { get => _result; set => SetProperty(ref _result, value); }
    public int PickedIndex { get; private set; }
    public Tmdb TmdbObject { get; private set; }
    public IgdbClass IgdbObject { get; private set; }
    public int TotalPosters { get => _totalPosters; set => SetProperty(ref _totalPosters, value); }
    private ObservableCollection<ListItem> _resultList;

    public ObservableCollection<DArtImageList> ImageUrl { get; set; }
    public DelegateCommand StopSearchCommand { get; set; }
    public DelegateCommand<DArtImageList> PickCommand { get; set; }
    public DelegateCommand<DArtImageList> OpenImageCommand { get; set; }
    #endregion
    public PosterPickerViewModel()
    {
        ImageUrl = [];
        StopSearchCommand = new DelegateCommand(delegate { StopSearch = true; });
        PickCommand = new DelegateCommand<DArtImageList>(PickMethod);
        OpenImageCommand = new DelegateCommand<DArtImageList>(OpenImageMethod);
    }

    private void OpenImageMethod(DArtImageList selectedImage)
    {
        Logger.Info("Open Image Method called with parameter: {Parameter}, MediaType: {MediaType}",selectedImage, Result.MediaType );
        var link = selectedImage.Url;
        link = Result.MediaType == MediaTypes.Game
            ? IgdbDataTransformer.GetPosterUrl(selectedImage.DeviationId, ImageSize.HD720)
            : link;
        Logger.Info("Opening Image: {Link}", link);
        var browser = new ImageBrowser(link)
        {
            ShowTitle = false,
            IsFullScreen = true
        };
        browser.Show();
    }

    protected virtual void CloseDialog(string parameter)
    {
        Logger.Info("Close Dialog called with parameter: {Parameter}", parameter);
        var result = parameter?.ToLower(CultureInfo.InvariantCulture) switch
        {
            "true" => ButtonResult.OK,
            "false" => ButtonResult.Cancel,
            _ => ButtonResult.None
        };

        RaiseRequestClose(new DialogResult(result));
    }

    public virtual void RaiseRequestClose(IDialogResult dialogResult)
    {
        RequestClose?.Invoke(dialogResult);
    }

    public virtual bool CanCloseDialog()
    {
        return true;
    }

    public virtual void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        Result = parameters.GetValue<ResultResponse>("result");
        PickedIndex = parameters.GetValue<int>("pickedIndex");
        TmdbObject = parameters.GetValue<Tmdb>("tmdbObject");
        IgdbObject = parameters.GetValue<IgdbClass>("igdbObject");
        _resultList = parameters.GetValue<ObservableCollection<ListItem>>("resultList");
        _isPickedById = parameters.GetValue<bool>("isPickedById");
        LoadData();
            
    }
    public async Task LoadData()
    {
        var resultType = Result.MediaType;
        dynamic response;
        if (_isPickedById)
        {
            response = resultType == MediaTypes.Game ? Result.Result[0] : Result.Result;
        }
        else
        {
            response = resultType == MediaTypes.Game ? Result.Result[PickedIndex] : Result.Result.Results[PickedIndex];
        }

        if (resultType != MediaTypes.Game)
        {
            ImagesWithId images = new();
            if (resultType == MediaTypes.Tv)
            {
                dynamic pickedResult = _isPickedById ? (TvShow)response : (SearchTv)response;
                Title = pickedResult.Name;
                images = await TmdbObject.SearchTvImages(pickedResult.Id);
            }
            else if (resultType == MediaTypes.Movie)
            {
                dynamic pickedResult = _isPickedById ? (Movie)response : (SearchMovie)response;
                Title = pickedResult.Title;
                images = await TmdbObject.SearchMovieImages(pickedResult.Id);
            }
            else if (resultType == MediaTypes.Collection)
            {
                dynamic pickedResult = _isPickedById ? (Collection)response : (SearchCollection)response;
                Title = pickedResult.Name;
                images = await TmdbObject.SearchCollectionImages(pickedResult.Id);
            }
            else if (resultType == MediaTypes.Mtv)
            {
                MediaType mediaType = response.MediaType;
                switch (mediaType)
                {
                    case MediaType.Tv:
                    {
                        SearchTv pickedResult = response;
                        Title = pickedResult.Name;
                        images = await TmdbObject.SearchTvImages(pickedResult.Id);
                        break;
                    }
                    case MediaType.Movie:
                    {
                        SearchMovie pickedResult = response;
                        Title = pickedResult.Title;
                        images = await TmdbObject.SearchMovieImages(pickedResult.Id);
                        break;
                    }
                }
            }
            await LoadImages(images);
        }
        else
        {
            Logger.Debug("Media Type is Game, loading images from IGDB");
            Artwork[] images =response.Artworks.Values;
            await LoadImages(images);
        }
    }

    private async Task LoadImages(ImagesWithId images)
    {
            
        StopSearch = false;
        ImageUrl.Clear();
        IsBusy = true;
        if (images is not null && images.Posters.Count > 0)
        {
            TotalPosters = images.Posters.Count;
            Logger.Debug("Total Posters: {TotalPosters}", TotalPosters);
            foreach (var item in images.Posters.GetEnumeratorWithIndex())
            {
                var image = item.Value;
                Index = item.Index + 1;
                if (image is not null)
                {
                    var posterPath = image.FilePath != null ? TmdbObject.GetClient().GetImageUrl(PosterSize.W92, image.FilePath).ToString() : null;
                    var qualityPath = TmdbObject.GetClient().GetImageUrl(PosterSize.W500, image.FilePath)
                        .ToString(); //TODO: give user option to set quality of preview.-
                    
                    Logger.Info(PosterPathMessage, posterPath);
                    Logger.Info("Quality Path: {QualityPath}", qualityPath);
                    var response = await Services.HttpC.GetAsync(posterPath);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Logger.ForErrorEvent().Message("Error getting poster from TMDb: {StatusCode}",
                                response.StatusCode)
                            .Property("Response", response).Log();
                        
                        continue;
                    }
                    ImageUrl.Add(new DArtImageList(qualityPath, posterPath));
                }

                if (!_stopSearch)
                {
                    continue;
                }

                Logger.Trace("Stop Search is true, breaking loop");
                break;
            }
        }
        else
        {
            Logger.Warn("No posters found for {Title}", Title);
            IsBusy = false;
            MessageBox.Show(CustomMessageBox.Warning(LangProvider.GetLang("NoPosterFound"), Title));
        }
        IsBusy = false;
    }
    private async Task LoadImages(Artwork[] images)
    {
        ResetState();
        IsBusy = true;
    
        if (images is { Length: > 0 })
        {
            await ProcessImages(images);
        }
        else
        {
            HandleNoImagesFound();
        }
    
        IsBusy = false;
    }
    
    private void ResetState()
    {
        StopSearch = false;
        ImageUrl.Clear();
    }
    
    private async Task ProcessImages(Artwork[] images)
    {
        TotalPosters = images.Length;
        Logger.Debug("Total Posters: {TotalPosters}", TotalPosters);
    
        foreach (var item in images.GetEnumeratorWithIndex())
        {
            Index = item.Index + 1;
            TryLoadImage(item.Value);
            if (!StopSearch) continue;
            Logger.Trace("Stop Search is true, breaking loop");
            break;
        }
    }
    
    private void TryLoadImage(Artwork image)
    {

        var posterPath = IgdbDataTransformer.GetPosterUrl(image.ImageId, ImageSize.ScreenshotMed);
        Logger.Info(PosterPathMessage, posterPath);
        AddImageToCollection(posterPath, image);
    }
    
    private void AddImageToCollection(string posterPath, Artwork image)
    {
        ImageUrl.Add(new DArtImageList(posterPath, posterPath, image.ImageId));
    }
    
    private void HandleNoImagesFound()
    {
        Logger.Warn("No posters found for {Title}", Title);
        MessageBox.Show(CustomMessageBox.Warning(LangProvider.GetLang("NoPosterFound"), Title));
    }
    private void PickMethod(DArtImageList pickedImage)
    {
        Logger.Info("Pick Method called with parameter: {Parameter}", pickedImage);
        var link = pickedImage.Url;
        dynamic result;
        if (_isPickedById)
        {
            result = Result.MediaType == MediaTypes.Game ? Result.Result[0] : Result.Result;
        }
        else
        {
            result = Result.MediaType == MediaTypes.Game
                ? Result.Result[PickedIndex]
                : Result.Result.Results[PickedIndex];
        }

        _resultList[PickedIndex].SetInitialPoster();
        if (Result.MediaType == MediaTypes.Game)
        {
            result.Cover.Value.ImageId = pickedImage.DeviationId;
            _resultList[PickedIndex].Poster =IgdbDataTransformer.GetPosterUrl(pickedImage.DeviationId, ImageSize.HD720);
        }
        else
        {
            result.PosterPath = link;
            _resultList[PickedIndex].Poster = link;
        }

        Logger.Trace(PosterPathMessage, _resultList[PickedIndex].Poster);
        CloseDialog("true");
    }

}
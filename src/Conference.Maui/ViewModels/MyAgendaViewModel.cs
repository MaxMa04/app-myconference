using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Plugin.Maui.SwipeCardView.Core;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels;

public partial class MyAgendaViewModel : ObservableObject
{
    private readonly IEventDataService _eventService;
    private readonly IDatabaseService _databaseService;
   
    

    public ObservableCollection<Session> FavoriteSessions { get; set; } = [];
    public ObservableCollection<Session> Sessions { get; set; } = [];
    
    [ObservableProperty]
    private bool _hasSwipedAllCards;
    
    [ObservableProperty]
    private bool _isFirstLaunch = true;
    
    [ObservableProperty]
    private double _threshold = 100;
    
    public MyAgendaViewModel(IEventDataService eventService, IDatabaseService databaseService)
    {
        _eventService = eventService;
        _databaseService = databaseService;
    }
    
    public async Task Initialize()
    {
        //await _databaseService.InitializeAsync();
        
        //// Check if this is the first launch by seeing if we have any favorite data
        //var favorites = await _databaseService.GetAllFavoriteSessionsAsync();
        //IsFirstLaunch = favorites.Count == 0;
        
        await LoadData();
    }
    
    private async Task LoadData()
    {
        var allSessions = await _eventService.GetAllSessions();
        var favorites = await _databaseService.GetAllFavoriteSessionsAsync();
        
        Sessions.Clear();
        FavoriteSessions.Clear();
        
        // If it's the first launch, load all sessions for swiping
        if (IsFirstLaunch)
        {
            foreach (var session in allSessions)
            {
                Sessions.Add(session);
            }
        }
        
        // Load favorited sessions
        foreach (var session in allSessions)
        {
            var favorite = favorites.FirstOrDefault(f => f.SessionId == session.Id);
            if (favorite != null && favorite.IsFavorite)
            {
                FavoriteSessions.Add(session);
            }
        }
        
        HasSwipedAllCards = Sessions.Count == 0;
    }
    
    [RelayCommand]
    private async Task Swiped(SwipedCardEventArgs args)
    {
        if (args.Direction == SwipeCardDirection.Right)
        {
            // Like - add to favorites
            await AddToFavorites(args.Item as Session);
        }
        else if (args.Direction == SwipeCardDirection.Left)
        {
            // Nope - remove from list
            // Do nothing with the database for non-favorites
        }
        
        // Check if we've swiped all cards
        HasSwipedAllCards = Sessions.Count == 0;
    }
    
    [RelayCommand]
    private void Dragging(DraggingCardEventArgs args)
    {
        // Optional animation effects during dragging
    }
    
    [RelayCommand]
    private async Task Like(Session session)
    {
        await AddToFavorites(session);
    }
    
    [RelayCommand]
    private void Dislike(Session session)
    {
        // Just move to next card
        if (Sessions.Contains(session))
        {
            Sessions.Remove(session);
        }
        
        HasSwipedAllCards = Sessions.Count == 0;
    }
    
    private async Task AddToFavorites(Session session)
    {
        if (session == null)
            return;
        
        var favoriteSession = new FavoriteSession
        {
            SessionId = session.Id,
            IsFavorite = true,
            CreatedAt = DateTime.Now
        };
        
        await _databaseService.SaveFavoriteSessionAsync(favoriteSession);
        
        if (Sessions.Contains(session))
        {
            Sessions.Remove(session);
        }
        
        if (!FavoriteSessions.Contains(session))
        {
            FavoriteSessions.Add(session);
        }
    }
    
    [RelayCommand]
    private async Task RemoveFromFavorites(Session session)
    {
        await _databaseService.DeleteFavoriteSessionAsync(session.Id);
        
        if (FavoriteSessions.Contains(session))
        {
            FavoriteSessions.Remove(session);
        }
    }
    
    [RelayCommand]
    private async Task Reset()
    {
        // Clear all favorite sessions and start over
        var favorites = await _databaseService.GetAllFavoriteSessionsAsync();
        foreach (var favorite in favorites)
        {
            await _databaseService.DeleteFavoriteSessionAsync(favorite.SessionId);
        }
        
        IsFirstLaunch = true;
        await LoadData();
    }
    
    [RelayCommand]
    private async Task GoToSessionDetails(Session selectedSession)
    {
        await Shell.Current.GoToAsync("SessionDetails",
            new Dictionary<string, object> { { "SelectedSession", selectedSession } });
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Youtube_DJ_Bot
{
	class YouTubeClient
	{
		public class Video
		{
			public Video(string title, string description, string id)
			{
				Title = title;
				Description = description;
				Id = id;
			}

			public string Url()
			{
				return "https://www.youtube.com/watch?v=" + Id;
			}

			public readonly string Title;
			public readonly string Description;
			public readonly string Id;
		}

		public void GetFavoriteVideos()
		{
			FetchFavorites().Wait();
		}

		public List<Video> Favorites
		{
			get { return _favoriteVideos; }
		}

		private async Task FetchFavorites()
		{
			UserCredential credential;
			var credentialsPath = Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase), @"..\..\..\..\client_secret.json").Replace("file:\\", "");
			using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
			{
				credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					// This OAuth 2.0 access scope allows for read-only access to the authenticated 
					// user's account, but not other types of account access.
					new[] { YouTubeService.Scope.YoutubeReadonly },
					"YoutubeUser",
					CancellationToken.None,
					new FileDataStore(this.GetType().ToString())
				);
			}

			var youtubeService = new YouTubeService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = this.GetType().ToString()
			});

			var channelsListRequest = youtubeService.Channels.List("contentDetails");
			channelsListRequest.Mine = true;
			var channels = await channelsListRequest.ExecuteAsync();

			if (channels.Items.Count <= 0)
			{
				Console.Write("ERROR: no YT channels found.");
				return;
			}

			var favoritesListId = channels.Items[0].ContentDetails.RelatedPlaylists.Favorites;

			string nextPageToken = "";
			var favoriteVideos = new List<PlaylistItem>();
			while (nextPageToken != null)
			{
				var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
				playlistItemsListRequest.PlaylistId = favoritesListId;
				playlistItemsListRequest.MaxResults = 50;
				playlistItemsListRequest.PageToken = nextPageToken;

				var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

				favoriteVideos.AddRange(playlistItemsListResponse.Items);
				nextPageToken = playlistItemsListResponse.NextPageToken;
			}

			for (int i = 0; i < favoriteVideos.Count; ++i)
			{
				var item = favoriteVideos[i];
				if (item.Snippet.Title != "Deleted video" && item.Snippet.Title != "Private video")
				{
					Video favoriteVideo = new Video(item.Snippet.Title, item.Snippet.Description, item.Snippet.ResourceId.VideoId);
					_favoriteVideos.Add(favoriteVideo);
					Console.WriteLine(_favoriteVideos.Count.ToString() + ": " + favoriteVideo.Title);
				}
			}
		}


		/// 
		/// Data members
		/// 

		private List<Video> _favoriteVideos = new List<Video>();
	}
}

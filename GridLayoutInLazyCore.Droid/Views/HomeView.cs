﻿using Android.App;
using Android.Content;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using GridLayoutInLazyCore.Application.Views;
using GridLayoutInLazyCore.Droid.Adapters;
using LazyCore.Droid.Views;
using LazyCore.Foundation.UI.Controls;
using SharpCaster.Controllers;
using SharpCaster.Models;
using SharpCaster.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GridLayoutInLazyCore.Droid.Views
{
    [Activity(MainLauncher = true)]
    public class HomeView : View<IHomeView>, IHomeView
    {
        private List<int> _data;
        public event EventHandler<RecyclerAdapterClickEventArgs> ItemClick;
        public event Action SendValidationRequest;
        public event Action ValidateButtonClicked;
        VideoView _videoView;
        Button _btnPlayPause, _castButton;

        string _videoUrl = "";

        public IButton ValidateButton { get; set; }

        public HomeView()
        {
            _data = new List<int>();
        }

        private bool IsTablet(Context context)
        {
            Display display = ((Activity)context).WindowManager.DefaultDisplay;
            DisplayMetrics displayMetrics = new DisplayMetrics();
            display.GetMetrics(displayMetrics);

            var wInches = displayMetrics.WidthPixels / (double)displayMetrics.DensityDpi;
            var hInches = displayMetrics.HeightPixels / (double)displayMetrics.DensityDpi;

            double screenDiagonal = Math.Sqrt(Math.Pow(wInches, 2) + Math.Pow(hInches, 2));
            return (screenDiagonal >= 6.0);
        }

        protected override void SetupView()
        {
            SetupRecyclerView();

            SendValidationRequest?.Invoke();

            ValidateButton = (LazyCore.Droid.UI.Button)FindViewById(Resource.Id.buttonx);

            _videoView = FindViewById<VideoView>(Resource.Id.videoView);

            _btnPlayPause = FindViewById<Button>(Resource.Id.btnPlayPause);
            _btnPlayPause.Enabled = false;

            _btnPlayPause.Click += (e, s) =>
            {
                try
                {
                    if (!_videoView.IsPlaying)
                    {
                        Android.Net.Uri uri = Android.Net.Uri.Parse(_videoUrl);
                        _videoView.SetVideoURI(uri);
                        _videoView.RequestFocus();

                        _videoView.Completion += (ee, ss) =>
                        {
                            _btnPlayPause.Text = "Play";
                        };

                        _videoView.Prepared += (ee, ss) =>
                        {
                            _videoView.Start();
                            _btnPlayPause.Text = "Pause";
                            //_dialog.Dismiss();
                        };
                    }
                    else
                    {
                        _videoView.Pause();
                        _btnPlayPause.Text = "Play";
                    }
                }
                catch (Exception exp)
                {
                    throw;
                }
            };

            _castButton = FindViewById<Button>(Resource.Id.castButton);
            _castButton.Enabled = false;
            _castButton.Click += HandleCastButtonClick;
        }

        private async void HandleCastButtonClick(object sender, EventArgs eventArgs)
        {
            ObservableCollection<Chromecast> chromecasts = await ChromecastService.Current.StartLocatingDevices();
            ////If that does not return devices on desktop then you can use this, Where 192.168.1.2 is your machines local ip
            //ObservableCollection<Chromecast> chromecasts = await ChromecastService.Current.StartLocatingDevices("192.168.1.2);
            if (chromecasts.Count > 0)
            {
                var chromecast = chromecasts.First();
                SharpCasterDemoController _controller = new SharpCasterDemoController(ChromecastService.Current.ChromeCastClient);

                ChromecastService.Current.ChromeCastClient.ConnectedChanged += async (s, e) =>
                {
                    _controller = await ChromecastService.Current.ChromeCastClient.LaunchSharpCaster();
                };

                ChromecastService.Current.ChromeCastClient.ApplicationStarted +=
                async delegate
                {
                    while (_controller == null)
                    {
                        await Task.Delay(500);
                    }

                    await _controller.LoadMedia(_videoUrl, "video/mp4",null, "BUFFERED");
                };
                await ChromecastService.Current.ConnectToChromecast(chromecast);

            }
        }

        private void SetupRecyclerView()
        {
            RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);

            var spanCount = IsTablet(this) ? 2 : 1;
            var gridLayoutManager = new GridLayoutManager(this, spanCount: spanCount);
            recyclerView.SetLayoutManager(gridLayoutManager);

            recyclerView.HasFixedSize = true;
            var adapter = new RecyclerAdapter(_data);
            recyclerView.SetAdapter(adapter);
            adapter.ItemClick += (e, args) =>
            {
                Toast.MakeText(this, args.Position.ToString(), ToastLength.Short).Show();
            };

            var surfaceOrientation = WindowManager.DefaultDisplay.Rotation;

            if (surfaceOrientation == SurfaceOrientation.Rotation90 || surfaceOrientation == SurfaceOrientation.Rotation270)
            {
                if (IsTablet(this))
                {
                    spanCount = 3;
                }
                else
                {
                    spanCount = 2;
                }

                gridLayoutManager.SpanCount = spanCount;
                adapter.NotifyDataSetChanged();
            }
        }

        public void SetVideoUrl(string mp4Url)
        {
            _btnPlayPause.Enabled = true;
            _castButton.Enabled = true;
            _videoUrl = mp4Url;
            //Android.Net.Uri uri = Android.Net.Uri.Parse(_videoUrl);
            //_videoView.SetVideoURI(uri);
            //_videoView.RequestFocus();
            //_videoView.Start();
        }

        protected override int LayoutResourceId => Resource.Layout.home_view;

    }
}
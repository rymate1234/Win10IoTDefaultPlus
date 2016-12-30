﻿// Copyright (c) Microsoft. All rights reserved.

using IoTCoreDefaultApp.Utils;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IoTCoreDefaultApp
{
    public sealed partial class TutorialContentPage : Page
    {
        private DispatcherTimer timer;
        private string docName;

        public TutorialContentPage()
        {
            this.InitializeComponent();

            var rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigated += RootFrame_Navigated;

            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            var languageManager = LanguageManager.GetInstance();
            this.DataContext = languageManager;

            languageManager.PropertyChanged += (sender, e) =>
            {
                // If the language manager updates the 
                // language, the current content needs to
                // be reloaded.
                if (e.PropertyName == "Item[]")
                {
// Disable await warning
#pragma warning disable 4014
                    Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => { LoadDocument(docName); });
#pragma warning restore 4014
                }
            };

            this.Loaded += async (sender, e) =>
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    UpdateDateTime();

                    timer = new DispatcherTimer();
                    timer.Tick += timer_Tick;
                    timer.Interval = TimeSpan.FromSeconds(30);
                    timer.Start();
                });
            };
            this.Unloaded += (sender, e) =>
            {
                timer.Stop();
                timer = null;
            };
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            var newDocName = e.Parameter as string;
            if (docName != newDocName && newDocName != null)
            {
                docName = newDocName;
// Disable await warning
#pragma warning disable 4014
                Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>{ LoadDocument(docName); });
#pragma warning restore 4014
                NextButton.Visibility = (NavigationUtils.IsNextTutorialButtonVisible(docName) ? Visibility.Visible : Visibility.Collapsed);
            }
        }

        private void LoadDocument(string docname)
        {
            var resourceMap = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap;
            var resourceContext = Windows.ApplicationModel.Resources.Core.ResourceContext.GetForCurrentView();
            var keys = resourceMap.Keys.Where(s => { return s.StartsWith("Resources/Tutorial/" + docname + "/"); }).OrderBy(s => s).ToArray();

            TutorialRichText.Blocks.Clear();
            foreach (var key in keys)
            {
                var split = key.Split('/');
                if (split.Length == 0)
                {
                    continue;
                }
                var blockType = split.Last();
                var value = resourceMap[key].Resolve(resourceContext).ValueAsString;
                var par = new Paragraph();
                switch (blockType)
                {
                    case "title":
                        par.FontSize = 28;
                        par.Margin = new Thickness(top: 0, left: 0, right: 0, bottom: 5);
                        par.Inlines.Add(new Run { Text = value });
                        break;
                    case "subtitle":
                        par.FontSize = 11;
                        par.Margin = new Thickness(top: 0, left: 0, right: 0, bottom: 5);
                        par.Inlines.Add(new Run { Text = value });
                        break;
                    case "h1":
                        par.FontSize = 16;
                        par.Margin = new Thickness(top: 10, left: 0, right: 0, bottom: 10);
                        par.Inlines.Add(new Run { Text = value });
                        break;
                    case "p":
                        par.FontSize = 11;
                        par.Margin = new Thickness(top: 0, left: 0, right: 0, bottom: 4);
                        par.Inlines.Add(new Run { Text = value });
                        break;
                    case "ul":
                        par.FontSize = 11;
                        par.Margin = new Thickness(top: 0, left: 0, right: 0, bottom: 4);
                        value = "\u27a4 " + value;
                        par.Inlines.Add(new Run { Text = value });
                        break;
                    case "br":
                        try
                        {
                            double fontSize = 11;
                            double bottomMarging = 4;
                            var size = split[split.Length - 2];
                            if (size.Contains('x'))
                            {
                                var sizeSplit = size.Split('x');
                                fontSize = int.Parse(sizeSplit[0]);
                                bottomMarging = int.Parse(sizeSplit[1]);
                            }
                            par.FontSize = fontSize;
                            par.Margin = new Thickness(top: 0, left: 0, right: 0, bottom: bottomMarging);
                            par.Inlines.Add(new Run { Text = value });
                        }
                        catch (Exception)
                        {
                            // just ignore this entry if anything goes wrong...
                        }
                        break;
                    case "image":
                        try
                        {
                            var deviceType = DeviceTypeInformation.Type;
                            if (deviceType != DeviceTypes.DB410)
                            {
                                deviceType = DeviceTypes.RPI2; // default to RPI2 images
                            }

                            var imageSource = new BitmapImage(new Uri("ms-appx:///" + String.Format(value, deviceType)));
                            var size = split[split.Length - 2];
                            if (size.Contains('x'))
                            {
                                var sizeSplit = size.Split('x');
                                var dx = int.Parse(sizeSplit[0]);
                                var dy = int.Parse(sizeSplit[1]);
                                par.Inlines.Add(new InlineUIContainer { Child = new Image { Source = imageSource, Width = dx, Height = dy } });
                            }
                            else
                            {
                                par.Inlines.Add(new InlineUIContainer { Child = new Image { Source = imageSource } });
                            }
                            par.Margin = new Thickness(top: 6, left: 0, right: 0, bottom: 10);
                        }
                        catch (Exception)
                        {
                            // just ignore this entry if anything goes wrong...
                        }
                        break;
                }
                TutorialRichText.Blocks.Add(par);
            }
        }

        private void timer_Tick(object sender, object e)
        {
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            var t = DateTime.Now;
            this.CurrentTime.Text = t.ToString("t", CultureInfo.CurrentCulture) + Environment.NewLine + t.ToString("d", CultureInfo.CurrentCulture);
        }

        private void ShutdownButton_Clicked(object sender, RoutedEventArgs e)
        {
            ShutdownDropdown.IsOpen = true;
        }

        private void ShutdownDropdown_Opened(object sender, object e)
        {
            var w = ShutdownListView.ActualWidth;
            if (w == 0)
            {
                // trick to recalculate the size of the dropdown
                ShutdownDropdown.IsOpen = false;
                ShutdownDropdown.IsOpen = true;
            }
            var offset = -(ShutdownListView.ActualWidth - ShutdownButton.ActualWidth);
            ShutdownDropdown.HorizontalOffset = offset;
        }


        private void ShutdownHelper(ShutdownKind kind)
        {
            ShutdownManager.BeginShutdown(kind, TimeSpan.FromSeconds(0.5));
        }

        private void ShutdownListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as FrameworkElement;
            if (item == null)
            {
                return;
            }
            switch (item.Name)
            {
                case "ShutdownOption":
                    ShutdownHelper(ShutdownKind.Shutdown);
                    break;
                case "RestartOption":
                    ShutdownHelper(ShutdownKind.Restart);
                    break;
            }
        }

        private void SettingsButton_Clicked(object sender, RoutedEventArgs e)
        {
            NavigationUtils.NavigateToScreen(typeof(Settings));
        }

        private void CommandLineButton_Clicked(object sender, RoutedEventArgs e)
        {
            NavigationUtils.NavigateToScreen(typeof(CommandLinePage));
        }

        private void BackButton_Clicked(object sender, RoutedEventArgs e)
        {
            NavigationUtils.GoBack();
        }

        private void DeviceInfo_Clicked(object sender, RoutedEventArgs e)
        {
            NavigationUtils.NavigateToScreen(typeof(MainPage));
        }

        private void NextButton_Clicked(object sender, RoutedEventArgs e)
        {
            NavigationUtils.NavigateToNextTutorialFrom(docName);
        }
    }
}
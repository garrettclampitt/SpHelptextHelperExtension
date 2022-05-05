﻿namespace SpHelptextHelperExtension.Controls
{
    using Microsoft.VisualStudio.Shell;
    using Microsoft.Web.WebView2.Core;
    using Microsoft.Web.WebView2.Wpf;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;


    /// <summary>
    /// Interaction logic for FacetsDataWindowControl.
    /// </summary>
    public partial class SpHelptextWindowControl : UserControl
    {
        bool facetsLoaded = false;

        private WebView2 _webView2Control;
        private CoreWebView2Environment _environment;
        private string _json;
        private string _title;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpHelptextWindowControl"/> class.
        /// </summary>
        public SpHelptextWindowControl()
        {
            InitializeComponent();

            Loaded += FacetsDataWindowControl_Loaded;
        }

        public void SetData(string json)
        {
            if (json == null)
            {
                return;
            }

            _json = json;

            if (facetsLoaded)
            {
                LoadJson();
            }
        }

        /// <summary>
        /// Sets window header. Defaults to sp_helptext. Empty string sets header to sp_helptext.
        /// </summary>
        /// <param name="title"></param>
        public void SetHeader(string title)
        {
            _title = title;

            if (facetsLoaded)
            {
                _ = _webView2Control.ExecuteScriptAsync($"console.log('setting header');window.setHeader('{title ?? "" }');").ContinueWith(task => { /* do some other stuff */ },
                                    TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void FacetsDataWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!facetsLoaded)
            {
                _ = InitializeWebViewAsync().ContinueWith(task => { /* do some other stuff */ },
                  TaskScheduler.FromCurrentSynchronizationContext());

                facetsLoaded = !facetsLoaded;
            }
        }

        private async System.Threading.Tasks.Task InitializeWebViewAsync()
        {
            // Create the cache directory 
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string cacheFolder = Path.Combine(localAppData, "FacetsDataWindowControlWebView2");

            //            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disk-cache-size=1 ");
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("–incognito ");

            // Create the environment manually
            _environment = await CoreWebView2Environment.CreateAsync(null, cacheFolder, options);

            // Create the web view 2 control and add it to the form. 
            _webView2Control = new WebView2();

            _webView2Control.Name = "webView2Control";
            _webView2Control.CoreWebView2InitializationCompleted += _webView2Control_CoreWebView2Ready;
            _webView2Control.NavigationCompleted += _webView2Control_NavigationCompleted;

            dockPanel.Children.Add(_webView2Control);

            await _webView2Control.EnsureCoreWebView2Async(_environment);
        }

        private void _webView2Control_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_json != null)
            {
                LoadJson();
                SetHeader(_title);
            }
        }

        private void LoadJson()
        {
            _ = _webView2Control.ExecuteScriptAsync($"console.log('hello from ssms');window.setCode({_json});").ContinueWith(task => { /* do some other stuff */ },
                                    TaskScheduler.FromCurrentSynchronizationContext());
        }



        private void _webView2Control_CoreWebView2Ready(object sender, EventArgs e)
        {
            var extensionFolder = GetExtensionInstallationDirectoryOrNull();

            _webView2Control.Source = new Uri($"file:///{extensionFolder}/WebView2.Templates/sp-helptext.html");
        }

        public static string GetExtensionInstallationDirectoryOrNull()
        {
            try
            {
                var uri = new Uri(typeof(SpHelptextHelperExtensionPackage).Assembly.CodeBase, UriKind.Absolute);

                return Path.GetDirectoryName(uri.LocalPath).Replace("\\", "/");
            }
            catch
            {
                return null;
            }
        }
    }
}

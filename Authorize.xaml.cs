using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Box_Task_Manager {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Authorize : UserControl {
        private Uri _RedirectUri;
        public event EventHandler Authorized;
        private string _AuthCode;
        public string AuthCode { 
            get => _AuthCode; 
            private set {
                if(_AuthCode == value) return;
                _AuthCode = value;
                Authorized?.Invoke(this, EventArgs.Empty);
                Visibility = Visibility.Collapsed;
            }
        }
        public void GetAuthCode(Uri authUri, Uri redirectUri) {
            _RedirectUri  = redirectUri;
            browser.Navigate(authUri);
            Visibility = Visibility.Visible;
        }
        public Authorize() {
            this.InitializeComponent();
        }

        private void browser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args) {

        }

        private void browser_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e) {
            AuthCode = null;
        }

        private void browser_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args) {
            if (args.Uri.Host != _RedirectUri.Host) return;
            if (args.Uri.Query is string query) {
                if (query.StartsWith("?")) query = query.Substring(1);
                List<string> query_parts = query.Split("&").ToList();
                foreach (string part in query_parts) {
                    if (part.StartsWith("code=")) {
                        AuthCode = part.Substring(5);
                    }
                }
                args.Cancel = true;
            }
        }
    }
}

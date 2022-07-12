using Box_Task_Manager.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Box_Task_Manager {
    public sealed partial class OauthDialog : ContentDialog {
        public static readonly DependencyProperty AuthcodeProperty = DependencyProperty.Register(
            nameof(Authcode), typeof(string), typeof(AddComment), new PropertyMetadata(default(string))
            );
        public string Authcode {
            get => GetValue(AuthcodeProperty) as string;
            set => SetValue(AuthcodeProperty, value);
        }

        public OauthDialog() {
            this.InitializeComponent();
            oauth.Authorized += (sender, evt) => {
                if (sender is Authorize authorize) {
                    Authcode = authorize.AuthCode;
                    Hide();
                    Authorized?.Invoke(this, authorize);
                }
            };
            Loaded += OauthDialog_Loaded;
            Closed += OauthDialog_Closed;
        }

        private void OauthDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args) {
            if(!(Authcode is null) && Authcode.Length > 0) {
                return;
            }
            Abort?.Invoke(this, args);
        }


        public event EventHandler<ContentDialogClosedEventArgs> Abort;
        public event EventHandler<Authorize> Authorized;

        private void OauthDialog_Loaded(object sender, RoutedEventArgs e) {
            oauth.GetAuthCode(
                Main.Config.AuthCodeUri, Main.Config.RedirectUri);
        }
    }
}

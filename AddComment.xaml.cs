using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Box_Task_Manager {
    public sealed partial class AddComment : ContentDialog {
        public static readonly DependencyProperty CommentProperty = DependencyProperty.Register(
            nameof(Comment), typeof(string), typeof(AddComment), new PropertyMetadata(default(string))
            );
        public string Comment { 
            get => GetValue(CommentProperty) as string; 
            set => SetValue(CommentProperty, value); 
        }

        public AddComment() {
            this.InitializeComponent();
            Comment = "";
        }
        public event EventHandler<string> CommentCommited;
        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            if(Comment == "") {
                _ = (new MessageDialog("You must enter a comment, in order to add a comment")).ShowAsync();
                return;
            }
            CommentCommited?.Invoke(this, Comment);
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
            Hide();
        }
    }
}

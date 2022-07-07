using Box_Task_Manager.View;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Box_Task_Manager {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DocumentView : Page {
        public DocumentView() {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) {
            


        }

        private void Page_Unloaded(object sender, RoutedEventArgs e) {

        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Frame.Navigate(typeof(MainPage));
        }

        private void Action_Click(object sender, RoutedEventArgs e)  {
            if (e.OriginalSource is Button button) {
                if (button.DataContext is Command command) {
                    ActAndNavigate(command);
                }
            }
            
        }
        private async void ActAndNavigate(Command action) {
            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                Frame.Navigate(typeof(MainPage));
                action.Execute(this);

            });
        }
    }
}

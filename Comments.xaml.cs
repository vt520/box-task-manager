using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Box_Task_Manager {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Comments : Page {
        public Comments() {
            this.InitializeComponent();
        }

        private void Details_Click(object sender, RoutedEventArgs e) {
            Frame.Navigate(typeof(DocumentView));
        }

        private void Tasks_Click(object sender, RoutedEventArgs e) {
            Frame.Navigate(typeof(MainPage));
        }
    }
}

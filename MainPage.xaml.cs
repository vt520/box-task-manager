using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using System.Collections.ObjectModel;
using Box_Task_Manager.View;
using Box.V2.Models;
using Windows.UI.Popups;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using System.Diagnostics;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Box_Task_Manager
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Main _Main; 
        public static ObservableCollection<Box.V2.Models.BoxTask> TestQueue { get; set; } = new ObservableCollection<Box.V2.Models.BoxTask>();
        public MainPage()
        {
            
            this.InitializeComponent();
                _Main = Locator.Main;

            this.Loading += MainPage_Loading;
            this.Loaded += MainPage_Loaded;
            _Main.PropertyChanged += _Main_PropertyChanged;

            oauth.Authorized += async (sender, evt) => {
                if(sender is Authorize authorize) {
                    await _Main.Init(authorize.AuthCode);
                }
            };

            _Main.Ready = _Main.IsConnected;
        }

        private void _Main_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(_Main.Ready)) {
                DoValidateReady();
            }
        }

        private void DoValidateReady() {
            if (!_Main.Ready) {
                oauth.GetAuthCode(Main.Config.AuthCodeUri, Main.Config.RedirectUri);
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e) {
            DoValidateReady();
        }

        private void MainPage_Loading(FrameworkElement sender, object args) {
            
        }
        
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {

        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e) {
            if (e.ClickedItem is TaskEntry entry) {
                Locator.TaskDetail = entry;
                Frame.Navigate(typeof(DocumentView));
            }
        }

        private async void Box_Click(object sender, RoutedEventArgs e) {
            if(e.OriginalSource is Button button) {
                if(button.DataContext is TaskEntry task) {
                    _ = await Windows.System.Launcher.LaunchUriAsync(
                        new Uri($"https://app.box.com/file/{task.Task.Item.Id}")
                    );
                }
            }
            
        }

        private void Comments_Click(object sender, RoutedEventArgs e) {
            if (e.OriginalSource is Button button) {
                if (button.DataContext is TaskEntry entry) {
                    AddComment addComment = new AddComment();
                    addComment.CommentCommited += async (source, text) => {
                        try {
                            await Main.Client.CommentsManager.AddCommentAsync(new BoxCommentRequest {
                                Item = new BoxRequestEntity { Id = entry.File.Id, Type = BoxType.file },
                                Message = text
                            });
                        } catch (Exception exception) {
                            Debug.WriteLine(exception.Message);
                        }
                        //(source as ContentDialog).Hide();
                    };
                    _ = addComment.ShowAsync();
                }
            }
        }

        private void Review_Click(object sender, RoutedEventArgs e) {
            if(e.OriginalSource is Button button) {
                if(button.DataContext is TaskEntry entry ) {
                    Locator.TaskDetail = entry;
                    Frame.Navigate(typeof(DocumentView));
                }
            }
        }

       
        private async void Logout_Click(object sender, RoutedEventArgs e) {
            MessageDialog prompt = new MessageDialog("Are you sure you want to log out?");
            prompt.Commands.Add(new UICommand("Yes", (command) => {
                _ = CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    Main.Client.Auth.LogoutAsync();
                });
            }));
            prompt.Commands.Add(new UICommand("No", (command) => {

            }));
            await prompt.ShowAsync();
        }
    }
}

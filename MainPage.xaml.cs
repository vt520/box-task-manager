using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using System.Collections.ObjectModel;
using Box_Task_Manager.View;
using Box.V2.Models;
using Windows.UI.Popups;
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
            oauth.Authorized += async (sender, evt) => {
                if(sender is Authorize authorize) {
                    await _Main.Init(authorize.AuthCode);
                }
            };
            
            this.Loading += MainPage_Loading;
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e) {
            if (!_Main.Ready) {
                oauth.GetAuthCode(Main.Config.AuthCodeUri, Main.Config.RedirectUri);
            }
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
                    Locator.TaskDetail = entry;
                    if (button.Parent is Panel parent) {
                        while (parent is Panel) {
                            if (parent.FindName("CommentEntry") is Popup entry_area) {
                                entry_area.IsOpen = !entry_area.IsOpen;
                                break;
                            }
                            parent = parent.Parent as Panel;
                        }
                    }
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

        private async void Add_Click(object sender, RoutedEventArgs e) {
            if (e.OriginalSource is Button button) {
                if (button.Parent is Panel parent) {
                    while (parent is Panel) {
                        if (parent.FindName("CommentEntry") is Popup entry_area) {
                            if (entry_area.FindName("NewComment") is TextBox textBox) {
                                if(textBox.Text.Trim().Length == 0) {
                                    await (new MessageDialog("Sorry, the comment cannot be blank.")).ShowAsync();
                                    return;
                                }
                                // BoxAPIException @@ MCR
                                try {
                                    BoxComment newComment = await Main.Client.CommentsManager.AddCommentAsync(new BoxCommentRequest {
                                        Item = new BoxRequestEntity {
                                            Id = Locator.TaskDetail.Task.Item.Id,
                                            Type = BoxType.file
                                        },
                                        Message = textBox.Text
                                    }); ;
                                    if (newComment?.Id is null) { }
                                    textBox.Text = string.Empty;
                                } catch  (Exception exception){
                                    await (new MessageDialog(exception.Message)).ShowAsync();
                                    return;
                                }
                            }
                            entry_area.IsOpen = false;
                            break;
                        }
                        parent = parent.Parent as Panel;
                    }
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            if (e.OriginalSource is Button button) {
                if (button.Parent is Panel parent) {
                    while (parent is Panel) {
                        if (parent.FindName("CommentEntry") is Popup entry_area) {
                            if(entry_area.FindName("NewComment") is TextBox textBox) {
                                textBox.Text = string.Empty;
                            }
                            entry_area.IsOpen = false;
                            break;
                        }
                        parent = parent.Parent as Panel;
                    }
                }
            }
        }
    }
}

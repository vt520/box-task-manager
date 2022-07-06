using System;
using System.Collections.Concurrent;
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
using System.Threading;
using Box.V2.Managers;
using Box.V2.Services;

using System.Collections.ObjectModel;
using System.Configuration;
using Box_Task_Manager.View;
using Windows.UI.ViewManagement;
using Box.V2.Models;
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

        private async void MainPage_Loaded(object sender, RoutedEventArgs e) {
            if (!_Main.Ready) {
                oauth.GetAuthCode(Main.Config.AuthCodeUri, Main.Config.RedirectUri);
            } else {
                _Main.UpdatingTasks = true;
            }
        }

        private void MainPage_Loading(FrameworkElement sender, object args) {
            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            if(e.OriginalSource is Button button) {
                if(button.DataContext is TaskEntry entry) {
                    //Main.Client.TasksManager.UpdateTaskAssignmentAsync()
                }
            }

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
                //if()
                if(button.Parent is Panel parent) {
                    while(parent is Panel) {
                        if(parent.FindName("CommentEntry") is Popup entry_area) { 
                            entry_area.IsOpen = !entry_area.IsOpen;
                            break;
                        }
                        parent = parent.Parent as Panel;
                    }
                }
                if (button.DataContext is TaskEntry entry) {
                    Locator.TaskDetail = entry;
                    //AddComment.IsOpen = true;
                    //Frame.Navigate(typeof(Comments));
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

                                BoxComment newComment = await Main.Client.CommentsManager.AddCommentAsync(new BoxCommentRequest {
                                    Item = new BoxRequestEntity {
                                        Id = Locator.TaskDetail.Task.Item.Id,
                                        Type = BoxType.file
                                    },
                                    Message = textBox.Text
                                }); ;
                                if(newComment?.Id is null) { }
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

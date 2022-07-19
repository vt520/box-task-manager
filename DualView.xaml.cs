using Box.V2.Models;
using Box_Task_Manager.View;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Box_Task_Manager {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DualView : Page {

        public DualView() {
            this.InitializeComponent();
            Locator.Instance.PropertyChanged += Instance_PropertyChanged;
        }
        
        private void DualView_Loaded(object sender, RoutedEventArgs e) {

            
            Locator.Instance.OnPropertyChangedAsync(nameof(Locator.Instance.TaskDetail));
            App.Maximize();
        }

        private void Instance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if(e.PropertyName == nameof(Locator.Instance.TaskDetail)) {

                for(int i = 0; i < CurrentTaskList.Items.Count; i++) {
                    if (CurrentTaskList.Items[i].Equals(Locator.Instance.TaskDetail)) {
                        CurrentTaskList.SelectedIndex = i;
                    }
                }
                CurrentTaskList.SelectedItem = Locator.Instance.TaskDetail;
            }
        }

        private void CurrentTaskList_ItemClick(object sender, ItemClickEventArgs e) {
            if(e.ClickedItem is TaskEntry task) {
                Locator.Instance.TaskDetail = task;
            }
        }

        private void CurrentTaskList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            
            Locator.Instance.TaskDetail = (sender as ListView).SelectedItem as TaskEntry;
            App.Maximize();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e) {
        }

        private void PageScaleControl_Loaded(object sender, RoutedEventArgs e) {
            
        }

        private void Page_LayoutUpdated(object sender, object e) {
        }

        private void PageScaleControl_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) {

        }

        private void PageScaleControl_SizeChanged(object sender, SizeChangedEventArgs e) {
            double existing_max = (int)PageScaleControl.Maximum;
            double existing_value = (int)PageScaleControl.Value;
            double factor = ((int)PageScaleControl.ActualWidth) / existing_max;
            //if ( Math.Abs((factor - 1) * 10 ) <  2) return;
            int new_value = (int)(PageScaleControl.Value * factor);
            Debug.WriteLine($"{existing_max} {existing_value} {factor} {new_value}");

            PageScaleControl.Maximum = PageScaleControl.ActualWidth;
            PageScaleControl.Value =new_value; // stop cogging 

        }


        private  void Logout_Click(object sender, RoutedEventArgs e) {
            
            MessageDialog prompt = new MessageDialog("Are you sure you want to log out?");
            prompt.Commands.Add(new UICommand("Yes", (command) => {
                _ = CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    App.Main.Session = null;
                    ToastNotificationManagerCompat.History.Clear();
                    App.Current.Exit();
                });
            }));

            prompt.Commands.Add(new UICommand("No", (command) => {

            }));
            _ = prompt.ShowAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (e.OriginalSource is Button button) {
                if (button.DataContext is Command command) {
                    ActAndNavigate(command);
                }
            }
        }

        private void AddComment_Click(object sender, RoutedEventArgs e) {
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
                    };
                    _ = addComment.ShowAsync();
                }
            }
        }

        private async void ActAndNavigate(Command action) {
            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                Frame.Navigate(typeof(DualView));
                action.Execute(this);

            });
        }

        private void BrowserLaunch_Click(object sender, RoutedEventArgs e) {
            if (e.OriginalSource is Button button) {
                if (button.DataContext is TaskEntry task) {
                    _ = Windows.System.Launcher.LaunchUriAsync(
                       new Uri($"https://app.box.com/file/{task.Task.Item.Id}")
                   );
                }
            }
        }

        private void Page_Loading(FrameworkElement sender, object args) {
            if (!App.Main.Ready) {
                OauthDialog dialog = new OauthDialog();
                dialog.Authorized += async (source, authorized) => {
                    await App.Main.Init(authorized.AuthCode);
                };
                dialog.Abort += (source, evt) => {
                    App.Current.Exit();
                };
                _ = dialog.ShowAsync();
                return;
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e) {

        }

        private void Grid_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args) {

        }
    }
}

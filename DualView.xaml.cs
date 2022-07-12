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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Box_Task_Manager {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DualView : Page {
        public DualView() {
            this.InitializeComponent();
            Locator.Instance.PropertyChanged += Instance_PropertyChanged;
            Loaded += DualView_Loaded;
        }

        private void DualView_Loaded(object sender, RoutedEventArgs e) {
            Locator.Instance.OnPropertyChangedAsync(nameof(Locator.Instance.TaskDetail));
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
        }
    }
}

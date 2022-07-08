using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Box_Task_Manager.View {
    public class Base : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public async Task DispatchAction(Action action) {
            await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                action();
            });
        }
        public async void OnPropertyChangedAsync([CallerMemberName] string propertyName = null) {
            if (PropertyChanged is null) return;
            await DispatchAction(
                () => PropertyChanged(this, new PropertyChangedEventArgs(propertyName))
            );
        }
        protected virtual void Execute(ICommand command) {
            command.Execute(command);
        }

    }
}

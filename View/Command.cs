using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Box_Task_Manager.View {
    public class Command : Base, ICommand {
        private bool _CanExecute = true;
        private string _Name;
        public string Name {
            get {
                if(_Name is null) {
                    return "Action Item";
                }
                return _Name;
            }
            set {
                if (_Name == value) return;
                _Name = value;
                OnPropertyChangedAsync();
            }
        }
        public event EventHandler CanExecuteChanged;
        protected readonly Func<object, Task> _Execute;

        public bool CanExecute(object parameter) => _CanExecute;
        public async void Execute(object parameter) {
            if (CanExecute(parameter)) {
                await _Execute(parameter);
                _CanExecute = false;
                CanExecuteChanged?.Invoke(this, new EventArgs());
            }
        }
        public Command(Func<object, Task> execute) {
            _Execute = execute;
        }
    }
}

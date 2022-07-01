using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Box_Task_Manager.View {
    public class Command : ICommand {
        public event EventHandler CanExecuteChanged;
        protected readonly Func<object, Task> _Execute;

        public bool CanExecute(object parameter) => true;
        public async void Execute(object parameter) {
            await _Execute(parameter);
        }
        public Command(Func<object, Task> execute) {
            _Execute = execute;
        }
    }
}

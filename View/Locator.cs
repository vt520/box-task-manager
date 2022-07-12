using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Box_Task_Manager.View {
    public class Locator: Base {
        private static ConcurrentDictionary<Type, Base> _Instances = new ConcurrentDictionary<Type, Base> { };
        private static Locator _Instance;
        public Main Main {
            get {
                return InstanceOf<Main>();
            }
        }

        public TaskEntry TaskDetail {
            get {
                return InstanceOf<TaskEntry>();
            } set {
                _Instances[typeof(TaskEntry)] = value;
                OnPropertyChangedAsync();
            }
        }

        public Locator() {

        }
        private ObservableCollection<TaskEntry> _Tasks;
        public ObservableCollection<TaskEntry> Tasks {
            get {
                if (_Tasks is null) Tasks = new ObservableCollection<TaskEntry>();
                return _Tasks;
            }
            set {
                if (_Tasks == value) return;
                _Tasks = value;
                OnPropertyChangedAsync();
            }
        }





        public static Locator Instance {
            get => _Instance is null ? _Instance = new Locator() : _Instance;
        }

        public static T InstanceOf<T>() {
            Type type = typeof(T);
            if (!typeof(Base).IsAssignableFrom(type))
                throw new AccessViolationException($"{type.Name} is not supported.");

            if (!_Instances.TryGetValue(type, out Base value)) {
                value = Activator.CreateInstance(type) as Base;
                _Instances[type] = value;
            }

            if (value is T return_value)
                return return_value; // this is not really but totally is needeed
            return default;
        }
    }
}

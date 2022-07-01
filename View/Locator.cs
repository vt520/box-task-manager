using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Box_Task_Manager.View {
    public class Locator {
        private static ConcurrentDictionary<Type, Base> _Instances = new ConcurrentDictionary<Type, Base> { };
        public static Main Main {
            get {
                return Instance<Main>();
            }
        }

        public static TaskEntry TaskDetail {
            get {
                return Instance<TaskEntry>();
            } set {
                _Instances[typeof(TaskEntry)] = value;
            }
        }
        public static T Instance<T>() {
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

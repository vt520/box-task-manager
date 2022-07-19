using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Box_Task_Manager.View {
    public class Locator: Base {
        private static ConcurrentDictionary<Type, Base> _Instances = new ConcurrentDictionary<Type, Base> { };
        private static Locator _Instance;

        private BadgeUpdater _Badge = null;

        public BadgeUpdater Badge {
            get {
                if (_Badge == null) {
                    Badge = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
                }
                return _Badge;
            }
            set {
                _Badge = value;
                OnPropertyChangedAsync();
            }
        }

        public static ConcurrentQueue<EntryToast> ActiveToasts = new ConcurrentQueue<EntryToast> { };
        
        public Main Main {
            get {
                return InstanceOf<Main>();
            }
        }

        private TaskEntry _TaskDetail = null;
        public TaskEntry TaskDetail {
            get {
                if (_TaskDetail is null) {
                    //TaskDetail = Tasks.FirstOrDefault();
                }
                return _TaskDetail;
            } set {
                if (_TaskDetail == value) return;
                _TaskDetail = value;
                if (_TaskDetail is null) App.Maximize();
                OnPropertyChangedAsync();
            }
        }

        public Locator() {
            ToastNotificationManagerCompat.History.Clear();
        }

        private ObservableCollection<TaskEntry> _Tasks;
        public ObservableCollection<TaskEntry> Tasks {
            get {
                if (_Tasks is null) Tasks = new ObservableCollection<TaskEntry>();
                return _Tasks;
            }
            set {
                if (_Tasks == value) return;
                if(_Tasks is ObservableCollection<TaskEntry>) _Tasks.CollectionChanged -= Tasks_CollectionChanged;
                _Tasks = value;
                if (_Tasks is ObservableCollection<TaskEntry>) {
                    _Tasks.CollectionChanged += Tasks_CollectionChanged;
                    Tasks_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                OnPropertyChangedAsync();
            }
        }

        private void Tasks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            XmlDocument badge_template = null;
            if(Tasks.Count > 0) {
                badge_template = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
                if(badge_template.SelectSingleNode("/badge") is XmlElement badge) {
                    badge.SetAttribute("value", Tasks.Count.ToString());
                }
            } else {
                badge_template = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeGlyph);
                if (badge_template.SelectSingleNode("/badge") is XmlElement badge) {
                    badge.SetAttribute("value", "available");    
                }   
            }
            if (badge_template is XmlDocument) {
                Locator.Instance.Badge.Update(new BadgeNotification(badge_template));
            } else {
                Locator.Instance.Badge.Clear();
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

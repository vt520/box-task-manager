using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Box_Task_Manager.View {
    public class EntryToast : Base {
        //private ToastNotifier ToastNotifier;
        private bool _Shown = false;
        public bool Shown {
            get {
                return _Shown & !Dirty;
            }
            set {
                if (value == false) {
                    if (!(Tag is null)) ToastNotificationManagerCompat.History.Remove(Tag);
                    if(ToastNotification is ToastNotification notification)  ToastNotifier.Hide(notification);
                    _Shown = false;
                } else {
                    if (_Shown) return;
//                    ToastNotificationManager.History.Remove(Tag);
                    ToastNotification = null;
                    if (ToastNotification is ToastNotification notification) {
                        
                        notification.Dismissed += (source, e) => {
                            
                        };
                        notification.Activated += (source, e) => {
                            Shown = false;
                        };
                        ToastNotifier.Show(notification);
                    } else return;
                    _Shown = true;
                }
            }
        }

        ~EntryToast() {
            Shown = false;
            TaskEntry = null;
        }
        private TaskEntry _Task;
        public TaskEntry TaskEntry {
            get => _Task;
            set {
                if(_Task == value) return;
                if (_Task is TaskEntry) _Task.PropertyChanged -= _Task_PropertyChanged;
                _Task = value;
                if (_Task is TaskEntry) _Task.PropertyChanged += _Task_PropertyChanged;
                OnPropertyChangedAsync();
            }
        }
        public string Tag {
            get {
                if(TaskEntry is null) return null;
                if (TaskEntry.Task is null) return null;
                return $"task_{TaskEntry.Task.Id}";
            }
        }
        
        private void _Task_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            Dirty = true;
        }

        private bool _ToastMatchesContent = false;
        public bool ToastContentMatchesTask { 
            get => _ToastMatchesContent;
            protected set {
                _ToastMatchesContent = value;
                OnPropertyChangedAsync();
                if(!value) OnPropertyChanged(nameof(Dirty));
            }
        }

        public bool Dirty {
            get {
                return !ToastContentMatchesTask;
            }
            set {
                if (!value) return;
                ToastContentMatchesTask = false;
                OnPropertyChangedAsync();
            }
        }

        private ToastNotification _ToastNotification;
        public ToastNotification ToastNotification {
            get {
                if(_ToastNotification is null ) {
                    if (Xml is null) return null;
                    ToastNotification = new ToastNotification(Xml);
                }
                return _ToastNotification;
            }
            protected set {
                if (_ToastNotification == value) return;
                if (value is null && ToastNotification is ToastNotification) {
                    ToastNotificationManager.History.Remove(Tag);
                    ToastContent = null;
                }

                _ToastNotification = value;
                OnPropertyChangedAsync();
            }
        }
        
        public XmlDocument Xml {
            get {
                return ToastContent?.GetXml();
            }
        }
        private ToastContent _ToatContent;
        public ToastContent ToastContent { 
            get {
                if(_ToatContent is null) {
                    if (TaskEntry is null) throw new NullReferenceException($"{nameof(TaskEntry)} must be set");
                    if (TaskEntry.Task is null) return null;
                    ToastContentMatchesTask = true;
                    ToastContent = (new ToastContentBuilder()

                        .AddArgument("task_id", TaskEntry.Task.Id)
                        .AddText(TaskEntry.Task.Message, hintMaxLines: 1)
                        .AddAttributionText($"Assigned by\n{TaskEntry.Task.CreatedBy.Name}")

//                        .AddText(TaskEntry.Comments?.Entries?.Last()?.Message)
                        .AddAppLogoOverride(TaskEntry.IconUri)
                        .AddHeroImage(TaskEntry.PreviewUri)

                        .Content);

                }
                return _ToatContent;
            } 
            set {
                if (_ToatContent == value) return;
                _ToatContent = value;
                OnPropertyChangedAsync();
                OnPropertyChangedAsync(nameof(Xml));
            }
        }
        private ToastNotifierCompat _ToastNotifier;
        public ToastNotifierCompat ToastNotifier {
            get {
                if (_ToastNotifier is null) {
                    ToastNotifier = ToastNotificationManagerCompat.CreateToastNotifier();
                }
                return _ToastNotifier;
            }
            set {
                _ToastNotifier = value;
                OnPropertyChangedAsync();
            }
        }
        

    }
}

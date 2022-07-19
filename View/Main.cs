using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.Models;
using Box.V2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using System.Threading;
using Windows.UI.Xaml;
using System.Collections.Concurrent;
using Windows.System.Threading;
using System.Diagnostics;
using Windows.UI.Popups;
using Windows.ApplicationModel.Core;

namespace Box_Task_Manager.View {

    public class Main : Base {
        private bool _IsRefreshingTasks = false;
        private bool _IsConvertingTasks = false;
        private bool? _Ready = null;


        private ConcurrentQueue<BoxTask> TaskStack = new ConcurrentQueue<BoxTask> { };
        private ConcurrentQueue<BoxFile> FileStack = new ConcurrentQueue<BoxFile> { };
        private static Queue<BoxFolder> _Folders = new Queue<BoxFolder> { };

        private ThreadPoolTimer FolderReaderTimer;
        private ThreadPoolTimer FileStackTimer;
        private DispatcherTimer TaskConverter = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) };
        private DispatcherTimer TaskUpdater = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 30) };

        private static string ClientID = APIConfiguration.Instance.Client_ID;
        private static string ClientSecret = APIConfiguration.Instance.Client_Secret;
        public Uri RedirectUri = new Uri(APIConfiguration.Instance.Redirect_URL);

        
        
        public static BoxConfig Config { get; set; }
        public static BoxClient Client { get; set; }
        public bool Connected { get; set; }

        public ObservableCollection<TaskEntry> Tasks { get => Locator.Instance.Tasks; }


        private string _Status = "Initializing";
        public string Status {
            get {
                if(!(_Status is null)) {
                    return _Status;
                }
                return $"{_Folders?.Count()} folders, {FileStack.Count()} files queued for search. (Run In Background: {(App.Current as App).BackgroundEnabled})";
            }
            set {
                _Status = value;
                OnPropertyChangedAsync();
            }
        }

        public OAuthSession Session {
            get {
                Status = "Validating Session";
                if (Client?.Auth?.Session is OAuthSession currentOauth) return currentOauth;

                if(App.Configuration.Values["session"] is ApplicationDataCompositeValue stored_oauth) {
                    try {
                        return new OAuthSession(
                            stored_oauth["access_token"] as string,
                            stored_oauth["refresh_token"] as string,
                            (int)stored_oauth["expires_in"] ,
                            stored_oauth["token_type"] as string);

                    } catch {

                    }
                }

                return null;
            }
            set {
                Status = "Saving Session";
                App.Configuration.Values.Remove("session");
                if (value is null) {
                    Ready = false;
                    Client?.Auth?.LogoutAsync();
                } else {
                    ApplicationDataCompositeValue stored_oauth = new ApplicationDataCompositeValue();
                    stored_oauth["access_token"] = value.AccessToken;
                    stored_oauth["refresh_token"] = value.RefreshToken;
                    stored_oauth["expires_in"] = value.ExpiresIn;
                    stored_oauth["token_type"] = value.TokenType;

                    App.Configuration.Values.Add("session", stored_oauth);
                }
                Status = null;
            }
        }
        
        private bool _IsScanningFolders = false;

        public bool IsScanningFolders {
            get => _IsScanningFolders | !(FolderReaderTimer is null);

            set {
                Status = $"Starting folder scanning: {value}";
                if(IsScanningFolders == value) return;
                if(value) {
                    FolderReaderTimer = ThreadPoolTimer.CreatePeriodicTimer(FolderReaderTimer_Tick, new TimeSpan(0, 1, 0));
                    FolderReaderTimer_Tick(FolderReaderTimer);
                } else {
                    FolderReaderTimer?.Cancel();
                    FolderReaderTimer = null;
                }
                _IsScanningFolders = value;
                Status = null;
            }
        }

        private bool _IsScanningFiles = false;
        public bool IsScanningFiles {
            get => _IsScanningFiles | !(FileStackTimer is null);
            private set {
                Status = $"Started file scanning: {value}";
                if(IsScanningFiles == value) return;
                if(value) {
                    FileStackTimer = ThreadPoolTimer.CreatePeriodicTimer(FileStackTimer_Tick, new TimeSpan(0, 15, 0));
                    FileStackTimer_Tick(FileStackTimer);
                } else {
                    FileStackTimer?.Cancel();
                    FileStackTimer = null;
                }
                _IsScanningFiles = value;
            }
        }

        public Main() {
            TaskConverter.Tick += TaskConverter_Tick;
            TaskUpdater.Tick += TaskUpdater_Tick;

            //Session = null;
            OAuthSession session = Session; // use appsettings

            Config = new BoxConfig(ClientID, ClientSecret, RedirectUri);
            Client = new BoxClient(Config, session);
            Client.Auth.SessionAuthenticated += Auth_SessionAuthenticated;
            Client.Auth.SessionInvalidated += Auth_SessionInvalidated;
            Status = "Main Finished";
        }

        private void TaskUpdater_Tick(object sender, object e) {
            UpdateTaskEntries();
        }

        private async void FolderReaderTimer_Tick(ThreadPoolTimer timer) {
            if (_IsScanningFolders) return;
            _IsScanningFolders = true;
            Status = "Reading Files";
            try {
                Queue<BoxFile> files;
                (files, _Folders) = await ReadFolder("0");
                while (files.TryDequeue(out BoxFile file)) FileStack.Enqueue(file);
                while (_Folders.TryDequeue(out BoxFolder folder)) {
                    Queue<BoxFolder> subfolders;
                    (files, subfolders) = await ReadFolder(folder.Id);
                    while (files.TryDequeue(out BoxFile file)) {
                        if (FileStack.Where(item => item.Id == file.Id).Count() == 0) FileStack.Enqueue(file);
                    }
                    IsScanningFiles |= FileStack.Count() > 0;
                    Status = null;
                    while (subfolders.TryDequeue(out BoxFolder subfolder)) _Folders.Enqueue(subfolder);
                }
            } catch (Exception exception) {
                Debug.WriteLine(exception.Message);
            }
            _IsScanningFolders = false;
        }
        private async void FileStackTimer_Tick(ThreadPoolTimer timer) {
            if (_IsScanningFiles) return;
            _IsScanningFiles = true;
            Status = "Reading Folders";
            try {
                BoxUser boxUser = await Main.Client.UsersManager.GetCurrentUserInformationAsync();
                while (FileStack.TryDequeue(out BoxFile file)) {
                    BoxCollection<BoxTask> file_tasks = await Main.Client.FilesManager.GetFileTasks(file.Id);
                    foreach (BoxTask task in file_tasks.Entries) {
                        if (TaskStack.Where(item => item.Id == task.Id).Count() == 0) {
                            if (task.IsCompleted) continue;
                            bool all_completed = true;
                            bool partially_completed = false;
                            bool assigned_to_me = false;
                            foreach (BoxTaskAssignment assignment in task.TaskAssignments.Entries) {
                                all_completed &= assignment.Status != "incomplete";
                                partially_completed |= assignment.Status != "incomplete";
                                if (assignment.AssignedTo.Id == (boxUser).Id) {
                                    assigned_to_me = true;
                                }
                            }
                            if (!assigned_to_me) continue;
                            if (all_completed) continue;
                            if (partially_completed && task.CompletionRule == BoxCompletionRule.any_assignee) continue;
                            TaskStack.Enqueue(task);
                        } else {
                            // duplicate
                        }
                    }
                    IsConvertingTasks |= TaskStack.Count() > 0;
                    Status = null;
                }
            } catch (Exception exception) {
                Debug.WriteLine(exception.Message);
            }
            IsScanningFiles = false;
        }

        private void Auth_SessionInvalidated(object sender, EventArgs e) {
            Status = "Logging Out";
            Session = null;
            Ready = false;
            
            //throw new NotImplementedException();
        }

        private void Auth_SessionAuthenticated(object sender, SessionAuthenticatedEventArgs e) {
            Status = "Session Authorized";
            Session = e.Session;
        }

        public void ConvertTasksToEntries() {
            ObservableCollection<TaskEntry> entries = Locator.Instance.Tasks;
            while (TaskStack.TryDequeue(out BoxTask task)) {
                if(entries.Where(item=>item.Task.Id == task.Id).Count() == 0) {
                    entries.Add(new TaskEntry { Task = task });
                }
            }
        }
        public async void UpdateTaskEntries() {
            ObservableCollection<TaskEntry> entries = Locator.Instance.Tasks;
            
            List <TaskEntry> completed = entries.Where(item => item.Completed).ToList();
            foreach (TaskEntry task in completed) {
                task.Toast.Shown = false;
                entries.Remove(task);
            }
            foreach (TaskEntry task in entries) {
                await task.UpdateTask();
                task.Toast.Shown = true;
            }

        }
        private async Task<(Queue<BoxFile>, Queue<BoxFolder>)> ReadFolder(string folder_id = "0") {
            try {
                BoxCollection<BoxItem> search = await Client.FoldersManager.GetFolderItemsAsync(folder_id, 100);
                Queue<BoxFile> files = new Queue<BoxFile>();
                Queue<BoxFolder> folders = new Queue<BoxFolder>();
                foreach (BoxItem item in search.Entries) {
                    switch (item.Type) {
                        case "file":
                            files.Enqueue(item as BoxFile);
                            break;
                        case "folder":
                            folders.Enqueue(item as BoxFolder);
                            break;
                    }
                }
                return (files, folders);

            } catch (Exception e) {
                throw e;
            }
        }
        public async Task Init(string access_code) {
            Status = "Initializing Application";
            try {
                await Client.Auth.AuthenticateAsync(access_code);
                Session = Client.Auth.Session;
                Ready = IsConnected;
                if (Ready) await App.Minimize();
            } catch (Exception exception) {
                await (new MessageDialog(exception.GetType().Name, exception.Message)).ShowAsync();
            }
        }
        protected override void Execute(ICommand command) {
            try {
                base.Execute(command);
            } catch (BoxException) {
                Ready = false;
            }
        }
        public bool IsConnected {
            get {
                if (Client.Auth.Session is null) return false;
                return Client.Auth.Session.ExpiresIn > 0;
            }
        }
        public bool IsRefreshingTasks {
            get {
                return _IsRefreshingTasks;
            }
            set {
                if (IsRefreshingTasks == value) return;
                if(value) {
                    _ = DispatchAction(() => {
                        TaskUpdater_Tick(this, null);
                        TaskUpdater.Start();
                    });
                } else {
                    _ = DispatchAction(() => {
                        TaskUpdater.Stop();
                    });
                    
                }
                _IsRefreshingTasks = value;
                OnPropertyChangedAsync();
            }
        }
        public bool IsConvertingTasks {
            get {
                return _IsConvertingTasks ;
            }
            set {
                if (IsConvertingTasks == value) return;
                if(value) {
                    _ = DispatchAction(() => {
                        TaskConverter_Tick(this, null);
                        TaskConverter.Start();
                    });
                } else {
                    _ = DispatchAction(() => {
                        TaskConverter.Stop();
                    });
                }
                _IsConvertingTasks = value;
                OnPropertyChangedAsync();
            }
        }
        public bool Ready { 
            get {
                if(_Ready is null) return false;
                return _Ready.Value & IsConnected;
            }
            set {
                Status = $"Setting Up: {value}";
                if (_Ready == value) return;
                if (value) {
                    Status = "Waiting for changes";
                    IsScanningFolders = true;
                    IsRefreshingTasks = true;
                    Status = $"Timers {IsScanningFiles} {IsScanningFolders}";
                } else {
                    IsScanningFiles = false;
                    IsScanningFolders = false;
                    IsConvertingTasks = false;
                    IsRefreshingTasks = false;
                    _ = DispatchAction(() => {
                        FileStack?.Clear();
                        _Folders?.Clear();
                        TaskStack?.Clear();
                        Locator.Instance.Tasks?.Clear();
                    });
                }
                
                _Ready = value;
                OnPropertyChangedAsync();
            }
        }

        private void TaskConverter_Tick(object sender, object e) {
            ConvertTasksToEntries();
        }
    }
}

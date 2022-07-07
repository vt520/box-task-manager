﻿using Box.V2.Auth;
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

namespace Box_Task_Manager.View {

    public class Main : Base {
        private ConcurrentQueue<BoxTask> _TaskStack = new ConcurrentQueue<BoxTask> { };
        private ThreadPoolTimer FolderReaderTimer;
        private ConcurrentQueue<BoxFile> _FileStack = new ConcurrentQueue<BoxFile> { };
        private ThreadPoolTimer FileStackTimer;
        private DispatcherTimer TaskUpdateTimer = new DispatcherTimer { Interval = new TimeSpan (0, 0, 1) };
        private static Queue<BoxFolder> _Folders;

        private DispatcherTimer Timer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) };
        private static object Sync = null;
        private static string _ClientID = APIConfiguration.Instance.Client_ID;
        private static string _ClientSecret = APIConfiguration.Instance.Client_Secret;
        public Uri RedirectUri = new Uri(APIConfiguration.Instance.Redirect_URL);
        private static Thread _UpdatingTasks;

        
        
        public static BoxConfig Config { get; set; }
        public static BoxClient Client { get; set; }
        public bool Connected { get; set; }
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
        private string _Status = "Waiting to Read Task List";
        public string Status {
            get {
                return $"{Tasks.Count()} Tasks Loaded, {_Folders?.Count()} folders and {_FileStack.Count()} files left to check";
            }
            set {
                if (_Status == value) return;
                _Status = value;
                OnPropertyChangedAsync();
            }
        }
        public OAuthSession Session {
            get {
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
                App.Configuration.Values.Remove("session");
                if (value is null) {
                    Client?.Auth?.LogoutAsync();
                } else {
                    ApplicationDataCompositeValue stored_oauth = new ApplicationDataCompositeValue();
                    stored_oauth["access_token"] = value.AccessToken;
                    stored_oauth["refresh_token"] = value.RefreshToken;
                    stored_oauth["expires_in"] = value.ExpiresIn;
                    stored_oauth["token_type"] = value.TokenType;

                    App.Configuration.Values.Add("session", stored_oauth);
                }
            }
        }
        
        private bool _IsScanningFolders = false;

        public bool IsScanningFolders {
            get => _IsScanningFolders | !(FolderReaderTimer is null);

            set {
                if(IsScanningFolders == value) return;
                _IsScanningFolders = value;
                if(value) {
                    FolderReaderTimer_Tick(null);
                } else {
                    FolderReaderTimer.Cancel();
                    FolderReaderTimer = null;
                }
            }
        }

        private bool _IsScanningFiles = false;
        public bool IsScanningFiles {
            get => _IsScanningFiles | !(FileStackTimer is null);
            private set {
                if(IsScanningFiles == value) return;
                _IsScanningFiles = value;
                if(value) {
                    FileStackTimer_Tick(null);
                } else {
                    FileStackTimer.Cancel();
                    FileStackTimer = null;
                }
            }
        }

        public Main() {
            Timer.Tick += Timer_Tick;

            Session = null;
            OAuthSession session = Session; // use appsettings

            Config = new BoxConfig(_ClientID, _ClientSecret, RedirectUri);
            Client = new BoxClient(Config, session);
            Client.Auth.SessionAuthenticated += Auth_SessionAuthenticated;
            Client.Auth.SessionInvalidated += Auth_SessionInvalidated;
            Tasks = new ObservableCollection<TaskEntry>();
            Ready = !(session is null);
        }

        private async void FolderReaderTimer_Tick(ThreadPoolTimer timer) {
            BoxCollection<BoxItem> box_entry = await Main.Client.FoldersManager.GetFolderItemsAsync("0", 1000);
            Queue<BoxFile> files;

            (files, _Folders) = await ReadFolder("0");
            while (files.TryDequeue(out BoxFile file)) _FileStack.Enqueue(file);
            while (_Folders.TryDequeue(out BoxFolder folder)) {
                Queue<BoxFolder> subfolders;
                (files, subfolders) = await ReadFolder(folder.Id);
                while (files.TryDequeue(out BoxFile file)) {
                    if(_FileStack.Where(item=>item.Id == file.Id).Count() == 0) _FileStack.Enqueue(file);
                }
                OnPropertyChangedAsync(nameof(Status));
                while (subfolders.TryDequeue(out BoxFolder subfolder)) _Folders.Enqueue(subfolder);
                //while(_FileStack.Count()>10) Thread.Sleep(1000);
            }
            if (IsScanningFolders) FolderReaderTimer = ThreadPoolTimer.CreateTimer(FolderReaderTimer_Tick, new TimeSpan(0, 15, 0));
        }
        private async void FileStackTimer_Tick(ThreadPoolTimer timer) {
            BoxUser boxUser = await Main.Client.UsersManager.GetCurrentUserInformationAsync();
            while (_FileStack.TryDequeue(out BoxFile file)) {
                BoxCollection <BoxTask> file_tasks = await Main.Client.FilesManager.GetFileTasks(file.Id);
                foreach(BoxTask task in file_tasks.Entries) {
                    if(_TaskStack.Where(item => item.Id == task.Id).Count() == 0) {
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
                        _TaskStack.Enqueue(task);
                    } else {
                        // duplicate
                    }
                }
                OnPropertyChangedAsync(nameof(Status));
            }
           if (IsScanningFiles) FileStackTimer = ThreadPoolTimer.CreateTimer(FileStackTimer_Tick, new TimeSpan(0, 0, 1));
        }

        private void Auth_SessionInvalidated(object sender, EventArgs e) {
            throw new NotImplementedException();
        }

        private void Auth_SessionAuthenticated(object sender, SessionAuthenticatedEventArgs e) {
            Session = e.Session;
        }

        public async Task UpdateTaskList() {
            if (Sync is null) 
                Sync = new object(); 
            else return;

            foreach(TaskEntry task in Tasks) {
                await task.UpdateTask();
            }

            while(_TaskStack.TryDequeue(out BoxTask task)) {
                if(Tasks.Where(item=>item.Task.Id == task.Id).Count() == 0) {
                    Tasks.Add(new TaskEntry { Task = task });
                }
            }
            
            foreach(TaskEntry task in Tasks.Where(item=>item.Completed)) {
                Tasks.Remove(task);
            }

            Sync = null;
            UpdatingTasks = false;
            return;
        }

        private async Task<(Queue<BoxFile>, Queue<BoxFolder>)> ReadFolder(string folder_id = "0") {
            BoxCollection<BoxItem> search = await Client.FoldersManager.GetFolderItemsAsync(folder_id, 100, direction:BoxSortDirection.DESC);
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
        }
        public async Task Init(string access_code) {
            await Client.Auth.AuthenticateAsync(access_code);
            Session = Client.Auth.Session;
            Ready = true;
        }
        protected override void Execute(ICommand command) {
            try {
                base.Execute(command);
            } catch (BoxException ex) {
                Ready = false;
            }
        }
        public bool IsConnected {
            get {
                if (Client.Auth.Session is null) return false;
                return Client.Auth.Session.ExpiresIn > 0;
            }
        }

        private bool _TaskListUpdating;
        public bool UpdatingTasks {
            get {
                return _TaskListUpdating;
            }
            set {
                if(_TaskListUpdating == value) return;
                _TaskListUpdating = value;
                OnPropertyChangedAsync();
                if (value) UpdateTaskList();
            }
        }
        private bool _Ready = false;
        public bool Ready { 
            get => _Ready; 
            set {
                if (_Ready == value) return;
                if (value) {
                    Timer.Start();
                    IsScanningFiles = true;
                    IsScanningFolders = true;
                } else {
                    Timer.Stop();
                    IsScanningFiles = false;
                    IsScanningFolders = false;
                }
                
                UpdatingTasks = _Ready = value;
                OnPropertyChangedAsync();
            }
        }

        private void Timer_Tick(object sender, object e) {
            UpdatingTasks = true;
        }
    }
}

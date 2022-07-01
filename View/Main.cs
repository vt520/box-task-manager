using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.Models;
using Box.V2.Managers;
using Box.V2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Box_Task_Manager.View.TaskEntries;

namespace Box_Task_Manager.View {
    public class Main : Base {
        private static object Sync = null;
        private const string _ClientID = "24fhrfjv52t2uumxkzdu4jwcbp3potk5";
        private const string _ClientSecret = "";
        public Uri RedirectUri = new Uri(@"https://taskmanager.energyservices.org/");

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
                return _Status;
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
        public Main() {
            //Session = null;
            OAuthSession session = Session; // use appsettings

            Config = new BoxConfig(_ClientID, _ClientSecret, RedirectUri);
            Client = new BoxClient(Config, session);
            Client.Auth.SessionAuthenticated += Auth_SessionAuthenticated;
            Client.Auth.SessionInvalidated += Auth_SessionInvalidated;
            Tasks = new ObservableCollection<TaskEntry>();
            Ready = !(session is null);
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

            BoxUser boxUser = await Client.UsersManager.GetCurrentUserInformationAsync();
            Status = "Reading Root Folder";
            (Queue<BoxItem> files, Queue<BoxItem> folders) = await ReadFolder("0");

            while(folders.TryDequeue(out BoxItem folder)) {
                Status = $"Finding files, {folders.Count} folders remaining, {files.Count} files found";
                (Queue<BoxItem> folder_files, Queue<BoxItem> folder_folders) = await ReadFolder(folder.Id);
                while (folder_files.TryDequeue(out BoxItem file)) files.Enqueue(file);
                while (folder_folders.TryDequeue(out BoxItem child)) folders.Enqueue(child);
            }

            while(files.TryDequeue(out BoxItem file)) {
                Status = $"{files.Count} files left to check for tasks, {Tasks.Count} found";
                BoxCollection<BoxTask> file_tasks = await Client.FilesManager.GetFileTasks(file.Id);
                foreach (BoxTask task in file_tasks.Entries) {
                    bool task_completed = task.IsCompleted;
                    bool all_complete = (task.CompletionRule == BoxCompletionRule.all_assignees);
                    bool assigned_to_me = false;
                    if (task_completed) continue;


                    BoxCollection<BoxTaskAssignment> task_assignments = task.TaskAssignments;
                    foreach (BoxTaskAssignment assignment in task_assignments.Entries) {
                        task_completed |= assignment.Status != "incomplete";
                        all_complete &= assignment.Status != "incomplete";
                        if (assignment.AssignedTo.Id == boxUser.Id) assigned_to_me = true;
                    }

                    if (!assigned_to_me) continue;
                    if (all_complete) continue;
                    if (task.CompletionRule == BoxCompletionRule.any_assignee && task_completed) continue;

                    if (Tasks.Where(item => (item.Task.Id == task.Id)).Count() > 0) continue; // Linq No Dupe id
                
                    // this is fugly
                        TaskEntry entry;
                    if(task.Action == "review") {
                        entry = new ApprovalTaskEntry {
                            Task = task
                        };
                    } else {
                        entry = new SimpleTaskEntry {
                            Task = task
                        };
                    }
                    this.Tasks.Add(entry);
                }
            }
            Status = $"Completed";
            Sync = null;
        }

        private async Task<(Queue<BoxItem>, Queue<BoxItem>)> ReadFolder(string folder_id = "0") {
            BoxCollection<BoxItem> search = await Client.FoldersManager.GetFolderItemsAsync(folder_id, 100, direction:BoxSortDirection.DESC);
            Queue<BoxItem> files = new Queue<BoxItem>();
            Queue<BoxItem> folders = new Queue<BoxItem>();
            foreach (BoxItem item in search.Entries) {
                switch (item.Type) {
                    case "file":
                        files.Enqueue(item);
                        break;
                    case "folder":
                        folders.Enqueue(item);
                        break;
                }
            }
            return (files, folders);
        }
        public async Task Init(string access_code) {
            await Client.Auth.AuthenticateAsync(access_code);
            Command after_auth = new Command(async _ => {
                await UpdateTaskList();
            });
            Execute(after_auth);
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
        private bool _Ready = false;
        public bool Ready { 
            get => _Ready; 
            set {
                if (_Ready == value) return;
                _Ready = value;
                OnPropertyChangedAsync();
            } 
        }
    }
}

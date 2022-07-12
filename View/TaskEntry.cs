using Box.V2.Models;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Box_Task_Manager.View {
    public class TaskEntry : Base {
        private EntryToast Toast;
        protected Assignment _Assignment;
        public Assignment Assignment {
            get { return _Assignment; }
            set {
                if (_Assignment == value) return;
                if (_Assignment?.BoxTaskAssignment.Id == value.BoxTaskAssignment.Id) {
                    if (_Assignment.BoxTaskAssignment.Status == value.BoxTaskAssignment.Status) return;
                }
                _Assignment = value;
                OnPropertyChangedAsync();
            }
        }
        protected ImageSource _Icon;
        public ImageSource Icon {
            get {
                if (_Icon is null) return new BitmapImage(new Uri("ms-appx:///Assets/Square150x150Logo.scale-200.png"));
                return _Icon;
            }
            set {
                if (_Icon == value) return;
                _Icon = value;
                OnPropertyChangedAsync();
                OnPropertyChangedAsync(nameof(Preview));
            }
        }
        private ObservableCollection<ImageSource> _Pages;

        public ObservableCollection<ImageSource> Pages {
            get {
                if (_Pages is null) Pages = new ObservableCollection<ImageSource> { Preview };
                return _Pages;
            }
            set {
                if (_Pages == value) return;
                _Pages = value;
                OnPropertyChangedAsync();
            }
        }
        private bool _SoloCompletion = false;

        public bool SoloCompletion {
            get => _SoloCompletion;
            set {
                if (value == _SoloCompletion) return;
                _SoloCompletion = value;
                OnPropertyChangedAsync();
            }
        }
        protected virtual async void UpdatePages() {
            Pages.Clear();
            for (uint index = 0; index < Document.PageCount; index++) {
                PdfPage page = Document.GetPage(index);
                InMemoryRandomAccessStream buffer = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(buffer);
                BitmapImage image = new BitmapImage();
                await image.SetSourceAsync(buffer);
                Pages.Add(image);
            }
        }

        protected ImageSource _Preview;
        public ImageSource Preview {
            get {
                if (_Preview is null) return Icon;
                return _Preview;
            }
            set {
                if (_Preview == value) return;
                _Preview = value;
                OnPropertyChangedAsync();
            }
        }
        protected PdfDocument _Document;
        public PdfDocument Document {
            get {
                return _Document;
            }
            set {
                if (_Document == value) return;
                _Document = value;
                OnPropertyChangedAsync();
                UpdatePages();
            }
        }
        
        protected BoxTask _Task;
        public BoxTask Task {
            get => _Task;
            set {
                if (_Task == value) return;
                _Task = value;
                OnPropertyChangedAsync();

                // maybe make async command?

                UpdateAll();
            }
        }
        public Uri PreviewUri { get => new Uri($"{Storage.Path}\\{Task?.Id}_preview.png"); }
        public Uri IconUri { get => new Uri($"{Storage.Path}\\{Task?.Id}_icon.png"); }
        private async Task ShowTaskToast() {
            string toast_tag = $"btm_{Task.Id}";
            ToastNotificationManagerCompat.History.Remove(toast_tag);


            //Uri test = await Main.Client.FilesManager.Get(Task.Item.Id);
            //Main.Client.FilesManager.GetThumbnailAsync(Task.Item.Id, 1);
            //Application.Current.Resources.TryAdd($"testing/{Task.Item.Id}", Icon);
            new ToastContentBuilder()
                .AddArgument("task_id", Task.Id)
                .AddText(Task.Message, hintMaxLines: 1)
                .AddAttributionText($"Assigned by\n{Task.CreatedBy.Name}")
              
                .AddText(Comments?.Entries.Last()?.Message)
                .AddAppLogoOverride(IconUri)
                .AddInlineImage(PreviewUri)
                .Show(toast => {
                    toast.ExpiresOnReboot = true;
                    toast.SuppressPopup = false;
                    toast.Priority = ToastNotificationPriority.High;
                    toast.Tag = toast_tag; 
                });
            
        }

        private ObservableCollection<BoxTaskAssignment> _Assignments;
        public ICollection<BoxTaskAssignment> Assignments {
            get => _Assignments;
            set {
                if (value == _Assignments) return;
                if (_Assignments is null | value is null) _Assignments = new ObservableCollection<BoxTaskAssignment>();
                foreach (BoxTaskAssignment assignment in value) {
                    bool is_changed = false;
                    if (_Assignments.Where(item => item.Id == assignment.Id).FirstOrDefault() is BoxTaskAssignment existing) {
                        is_changed |= assignment.Status != existing.Status;
                        is_changed |= assignment.AssignedTo.Id != existing.AssignedTo.Id;
                        if (is_changed) {
                            _Assignments.Remove(existing);
                            _Assignments.Add(assignment);
                        }
                    } else {
                        // add it
                        _Assignments.Add(assignment);
                    }
                }
                List<BoxTaskAssignment> missing_assignments = _Assignments.Except(value, new BoxTaskComparator()).ToList();
                foreach(BoxTaskAssignment missing_assignment in missing_assignments) {
                    _Assignments.Remove(missing_assignment);
                }
                OnPropertyChangedAsync();
            }
        }
        public static StorageFolder Storage { get => ApplicationData.Current.LocalFolder as StorageFolder; }
        public TaskEntry() {
            Toast = new EntryToast {
                TaskEntry = this
            };
            Toast.Shown = true;
        }
        private class BoxTaskComparator : IEqualityComparer<BoxTaskAssignment> {
            public bool Equals(BoxTaskAssignment x, BoxTaskAssignment y) {
                return x.Id == y.Id;
            }

            public int GetHashCode(BoxTaskAssignment obj) {
                return (int)(ulong.Parse(obj?.Id) % int.MaxValue);
            }
        }
        public async void UpdateComments() {
            try {
                BoxCollection<BoxComment> current_comments = await Main.Client.FilesManager.GetCommentsAsync(Task.Item.Id);
                if (Comments is null) {
                    Comments = current_comments;
                    return;
                }
                bool stale_comments = false;

                foreach (BoxComment comment in current_comments.Entries) {
                    IEnumerable<BoxComment> existing_comments = Comments.Entries.Where(item => (item.Id == comment.Id));
                    if (existing_comments.Count() > 0) {
                        // merge comment
                        BoxComment existing_comment = existing_comments.First();

                        if (existing_comment.Message == comment.Message) continue;
                    }
                    stale_comments = true;
                }
                if (stale_comments) Comments = current_comments;
            } catch (Exception exception) {
                Debug.WriteLine(exception.Message);
            }
        }

        public async Task UpdateTask() {
            try {
                Task = await Main.Client.TasksManager.GetTaskAsync(Task.Id);
            } catch (Exception exception) {
                Debug.WriteLine(exception.Message);
            }
        }
        private BoxUser _UploadedBy;
        public BoxUser UploadedBy {
            get => _UploadedBy;
            set {
                if(UploadedBy == value) return;
                _UploadedBy = value;
                OnPropertyChangedAsync();
            }
        }

        private BoxFile _File;
        public BoxFile File {
            get => _File;
            set {
                if (_File == value) return;
                _File = value;
                OnPropertyChangedAsync();
            }
        }
        public void UpdateAll() {
            UpdateFile();
            UpdateAssignment();
            UpdateIcon();
            UpdateComments();
            UpdatePreview();
            Toast.Shown = true;
            //_ = ShowTaskToast();
        }

        private async void UpdateFile() {
            try {
                File = await Main.Client.FilesManager.GetInformationAsync(Task.Item.Id);
                UpdateUploader();
            } catch (Exception exception) {
                Debug.WriteLine(exception.Message);
            }
        }

        private void UpdateUploader() {
            UploadedBy = File?.CreatedBy;
        }

        private async void UpdateAssignment() {
            try {
                Assignments = Task.TaskAssignments.Entries.ToList();
                SoloCompletion = Task?.CompletionRule == BoxCompletionRule.any_assignee | Assignments?.Count == 1;
                BoxUser user = await Main.Client.UsersManager.GetCurrentUserInformationAsync();
                foreach (BoxTaskAssignment assignment in Task.TaskAssignments.Entries) {
                    if (assignment.AssignedTo.Id == user.Id) {

                        Assignment = Assignment.InstanceFor(assignment, Task.Action);
                        return;
                    }
                }
            } catch (Exception exception) {
                Debug.WriteLine(exception.Message);
            }
        }

        private async void UpdateIcon() {
            if (!(_Icon is null)) return;
            try {
                Stream stream = await Main.Client.FilesManager.GetThumbnailAsync(Task.Item.Id, minHeight: 256);
                MemoryStream buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                buffer.Position = 0;

                BitmapImage thumbnail = new BitmapImage();
                thumbnail.SetSource(buffer.AsRandomAccessStream());
                Icon = thumbnail;

                if (ApplicationData.Current.LocalFolder is StorageFolder folder) {
                    try {
                        Stream output = await folder.OpenStreamForWriteAsync(Path.GetFileName(IconUri.LocalPath), CreationCollisionOption.ReplaceExisting);
                        buffer.Position = 0;
                        buffer.CopyTo(output);
                        output.Close();
                    } catch {

                    }
                }

            } catch (Exception exception) {
                Debug.WriteLine(exception.Message);
            }
        }

        protected async virtual void UpdatePreview() {
            try {
                if (!(_Preview is null)) return;
                BoxRepresentationRequest pdf_request = new BoxRepresentationRequest {
                    FileId = Task.Item.Id,
                    XRepHints = "[pdf]"
                };
                MemoryStream buffer = new MemoryStream();
                BoxRepresentationCollection<BoxRepresentation> representations // System.TimeoutException @@ MCR do this everywhere 
                    = await Main.Client.FilesManager.GetRepresentationsAsync(pdf_request);
                Stream representation_stream = null;
                try {
                    foreach (BoxRepresentation rep in representations.Entries) {
                        if (rep.Status.State != "error") {
                            representation_stream = await Main.Client.FilesManager.GetRepresentationContentAsync(pdf_request, "");
                        }
                    }
                } catch {

                }
                if (representation_stream is null) {
                    if (Task.Item.Name.ToLower().EndsWith(".pdf")) {
                        representation_stream = Main.Client.FilesManager.DownloadAsync(Task.Item.Id).Result;
                    }
                }
                if (representation_stream is Stream) {
                    await representation_stream.CopyToAsync(buffer);
                    Document = await PdfDocument.LoadFromStreamAsync(buffer.AsRandomAccessStream());

                    // Create file


                    PdfPage preview_page = Document.GetPage(0);
                    InMemoryRandomAccessStream draw_buffer = new InMemoryRandomAccessStream();
                    StorageFile preview_file = await Storage.CreateFileAsync(Path.GetFileName(PreviewUri.LocalPath), CreationCollisionOption.ReplaceExisting);
                    IRandomAccessStream preview_file_buffer = await preview_file.OpenAsync(FileAccessMode.ReadWrite);
                    await preview_page.RenderToStreamAsync(draw_buffer);
                    await preview_page.RenderToStreamAsync(preview_file_buffer);
                    preview_file_buffer.Dispose();
                    


                    BitmapImage preview = new BitmapImage();
                    await preview.SetSourceAsync(draw_buffer);
                    Preview = preview;
                }
            } catch (Exception exception) {
                Debug.WriteLine(exception.Message);
            }
        }
        private BoxCollection<BoxComment> _Comments;
        public BoxCollection<BoxComment> Comments {
            get => _Comments;
            set {
                if(_Comments == value) return;
                _Comments = value;
                OnPropertyChangedAsync();
                OnPropertyChangedAsync(nameof(LastComment));
            }
        }
        public BoxComment LastComment {
            get {
                if (Comments?.Entries?.Count is null) return null;
                if (Comments.Entries.Count < 1) return null;
                return Comments.Entries.Last();
            }
        }
        public bool Completed { 
            get {
                if (!Task.IsCompleted) {
                    bool all_completed = true;
                    bool partial_completed = false;

                    foreach (BoxTaskAssignment assignment in Task.TaskAssignments.Entries) {
                        all_completed &= assignment.Status != "incomplete";
                        partial_completed |= assignment.Status != "incomplete";
                    }
                    if (!(all_completed | (partial_completed && Task.CompletionRule == BoxCompletionRule.any_assignee))) return false;
                }
                ToastNotificationManagerCompat.History.Remove($"btm_{Task.Id}");
                return true;
            } 
        }
    }
}
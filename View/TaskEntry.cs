using Box.V2.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Box_Task_Manager.View {
    public class TaskEntry : Base {
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
                if(_Icon is null) return new BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png"));
                return _Icon;
            }
            set { 
                if(_Icon == value) return;
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
        public async void UpdateComments() {

            BoxCollection<BoxComment> current_comments  = await Main.Client.FilesManager.GetCommentsAsync(Task.Item.Id);
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
        }

        public async Task UpdateTask() {
            Task = await Main.Client.TasksManager.GetTaskAsync(Task.Id);
        }
        public void UpdateAll() {
            UpdateAssignment();
            UpdateIcon();
            UpdateComments();
            UpdatePreview();
        }

        private async void UpdateAssignment() {
            BoxUser user = await Main.Client.UsersManager.GetCurrentUserInformationAsync();
            foreach(BoxTaskAssignment assignment in Task.TaskAssignments.Entries) {
                if (assignment.AssignedTo.Id == user.Id) {
                    
                    Assignment = Assignment.InstanceFor(assignment, Task.Action);
                    return;
                }
            }
        }

        private async void UpdateIcon() {
            if (!(_Icon is null)) return;

            Stream stream = await Main.Client.FilesManager.GetThumbnailAsync(Task.Item.Id, minHeight: 256);
            MemoryStream buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            buffer.Position = 0;

            BitmapImage thumbnail = new BitmapImage();
            thumbnail.SetSource(buffer.AsRandomAccessStream());
            Icon = thumbnail;

        }

        protected async virtual void UpdatePreview() {
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
                PdfPage preview_page = Document.GetPage(0);
                InMemoryRandomAccessStream draw_buffer = new InMemoryRandomAccessStream();
                await preview_page.RenderToStreamAsync(draw_buffer);
                BitmapImage preview = new BitmapImage();
                await preview.SetSourceAsync(draw_buffer);
                Preview = preview;
            }
        }
        private BoxCollection<BoxComment> _Comments;
        public BoxCollection<BoxComment> Comments {
            get => _Comments;
            set {
                if(_Comments == value) return;
                _Comments = value;
                OnPropertyChangedAsync();
            }
        }

        public bool Completed { 
            get {
                if (Task.IsCompleted) return true;
                bool all_completed = true;
                bool partial_completed = false;

                foreach(BoxTaskAssignment assignment in Task.TaskAssignments.Entries) {
                    all_completed &= assignment.Status != "incomplete";
                    partial_completed |= assignment.Status != "incomplete";
                }
                return all_completed | (partial_completed && Task.CompletionRule == BoxCompletionRule.any_assignee);
            } 
        }
    }
}
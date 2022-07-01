using Box.V2.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using Windows.Data.Pdf;
using Windows.Graphics.Imaging;
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
            Comments = await Main.Client.FilesManager.GetCommentsAsync(Task.Item.Id);

        }

        public void UpdateAll() {
            UpdateIcon();
            UpdateComments();
            UpdatePreview();
        }
        private async void UpdateIcon() {

            Stream stream = await Main.Client.FilesManager.GetThumbnailAsync(Task.Item.Id, minHeight: 256);
            MemoryStream buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            buffer.Position = 0;

            BitmapImage thumbnail = new BitmapImage();
            thumbnail.SetSource(buffer.AsRandomAccessStream());
            Icon = thumbnail;

        }

        protected async virtual void UpdatePreview() {
            BoxRepresentationRequest pdf_request = new BoxRepresentationRequest {
                FileId = Task.Item.Id,
                XRepHints = "[pdf]"
            };
            MemoryStream buffer = new MemoryStream();
            BoxRepresentationCollection<BoxRepresentation> representations
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
    }
}
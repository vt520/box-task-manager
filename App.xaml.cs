using Box.V2.Models;
using Box_Task_Manager.View;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Box_Task_Manager
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public const string Authorization_Key = "box_auth";
        public static readonly Main Main = Locator.Instance.Main;
        private static ExtendedExecutionSession BackgroundSession = null;
        public bool BackgroundEnabled {
            get {
                return !(BackgroundSession is null);
            }
            set {
                BackgroundSession?.Dispose();
                if (value) {
                    _ = CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => {
                        App.BackgroundSession = new ExtendedExecutionSession();
                        App.BackgroundSession.Reason = ExtendedExecutionReason.Unspecified;
                        App.BackgroundSession.Description = "Raising periodic toasts";
                        App.BackgroundSession.Revoked += BackgroundSession_Revoked;
                        ExtendedExecutionResult result = await BackgroundSession.RequestExtensionAsync();
                        if(result == ExtendedExecutionResult.Denied) {
                            App.BackgroundSession = null;
                        }
                    });
                } else {
                    BackgroundSession = null;
                }
            }
        }

        private void BackgroundSession_Revoked(object sender, ExtendedExecutionRevokedEventArgs args) {
            App.BackgroundSession = null;
        }

        //private stati80
        public static ApplicationDataContainer Configuration { get => Windows.Storage.ApplicationData.Current.LocalSettings; }
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {


            //ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Maximized;


            
            this.InitializeComponent();
            Suspending += App_Suspending;
            //Window.Current.SetTitleBar( new TextBlock { Text = "Okay" });
            Window.Current.SizeChanged += Current_SizeChanged;
        }


        private void App_Suspending(object sender, SuspendingEventArgs e) {
            
        }

        private async void App_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e) {
            e.Handled = true;
            Locator.Instance.TaskDetail = null;
            CoreVirtualKeyStates state = Window.Current.CoreWindow.GetAsyncKeyState(VirtualKey.Control);
            if((state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down) {
                Exit();
            }
            await Minimize();
        }
        public static async Task Minimize() {
            IList<AppDiagnosticInfo> information = await AppDiagnosticInfo.RequestInfoForAppAsync();
            IList<AppResourceGroupInfo> resource_information = information.FirstOrDefault().GetResourceGroups();
            await resource_information.FirstOrDefault().StartSuspendAsync();
        }

        private void SystemEvents_SessionEnded(object sender, SessionEndedEventArgs e) {
            ToastNotificationManagerCompat.History.Clear();
            App.Current.Exit();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

            Frame rootFrame = Window.Current.Content as Frame;
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += App_CloseRequested;
            Maximize();

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();


                Main.Ready = Main.IsConnected;
                if (Main.Ready) {
                    Minimize();
                }

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e?.PrelaunchActivated == false)
            {

                if (rootFrame.Content == null)
                {

                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(DualView), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
            BackgroundEnabled = true;

            //            Window.Current.SizeChanged += Current_SizeChanged;
        }

        private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e) {
           // throw new NotImplementedException();
        }

        public static void Maximize() {
            DisplayInformation resolution = DisplayInformation.GetForCurrentView();
            double conversion = resolution.RawPixelsPerViewPixel;
            Size relativeSize = new Size(0.80, 0.80);
            Size windowSize = new Size((resolution.ScreenWidthInRawPixels / conversion) * relativeSize.Width, (resolution.ScreenHeightInRawPixels / conversion) * relativeSize.Height);

            if (Locator.Instance.TaskDetail is null) {
                windowSize.Height = windowSize.Height / 2;
                if (windowSize.Height < 700) windowSize.Height = 700;
                windowSize.Width = 450;
                ApplicationView.PreferredLaunchViewSize = windowSize;
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            }

            ApplicationView.GetForCurrentView().TryResizeView(windowSize);
            ApplicationView.GetForCurrentView().SetPreferredMinSize(windowSize);
        }
        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
        protected override async void OnActivated(IActivatedEventArgs args) {
            OnLaunched(null);
            Frame rootFrame = Window.Current.Content as Frame;
            //Locator.Instance.TaskDetail = null; 
            if (!Main.Ready) {
                rootFrame.Navigate(typeof(DualView));
            }
            if (args is ToastNotificationActivatedEventArgs toast) {
                List<string> parameter_sets = toast.Argument.Split(";").ToList();
                Dictionary<string, string> arguments = new Dictionary<string, string>();
                foreach (string parameter in parameter_sets) {
                    List<string> sections = parameter.Split("=").ToList();
                    if (sections.Count() < 2) sections.Add("true");
                    arguments[Uri.UnescapeDataString(sections[0])] = Uri.UnescapeDataString(sections[1]);
                }
                if(arguments.TryGetValue("task_id", out string task_id)) {
                    try {
                        BoxTask task = await Main.Client.TasksManager.GetTaskAsync(task_id);
                        TaskEntry taskEntry = null;
                        foreach (TaskEntry existing_task in Locator.Instance.Tasks) {
                            if (existing_task.Task.Id == task_id) taskEntry = existing_task;
                        }
                        if (taskEntry is null) taskEntry = new TaskEntry {
                            Task = task
                        };
                        // maybe be more selective?
                        Locator.Instance.TaskDetail = taskEntry;
                    } catch (Exception exception) {
                        Locator.Instance.TaskDetail = null;
                        await (new MessageDialog(exception.GetType().Name, exception.Message)).ShowAsync();
                    }
                }
            }
            rootFrame.Navigate(typeof(DualView));

            base.OnActivated(args);
        }
    }
}

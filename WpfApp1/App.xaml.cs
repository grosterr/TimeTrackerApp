using System;
using System.IO;
using System.Windows;
using WpfApp1.Core;
using WpfApp1.ViewModels;

namespace WpfApp1
{
    public partial class App : Application
    {
        public static ITaskRepository TaskRepository { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(folderPath, "TimeTrackerApp");
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            string dbPath = Path.Combine(appFolder, "TimeTracker.db3");
            TaskRepository = new SqliteTaskRepository(dbPath);
            var mainVM = new MainViewModel(TaskRepository);
            MainWindow mainWindow = new MainWindow
            {
                DataContext = mainVM
            };

            mainWindow.Show();
        }
    }
}
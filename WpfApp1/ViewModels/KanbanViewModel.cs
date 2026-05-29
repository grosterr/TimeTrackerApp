using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GongSolutions.Wpf.DragDrop;
using WpfApp1.Core;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class KanbanViewModel : ObservableObject, IDropTarget
    {
        private readonly ITaskRepository _taskRepository;
        public ObservableCollection<TaskItem> ToDoTasks { get; set; } = new();
        public ObservableCollection<TaskItem> InProgressTasks { get; set; } = new();
        public ObservableCollection<TaskItem> DoneTasks { get; set; } = new();
        public ObservableCollection<TaskItem> BlockedTasks { get; set; } = new();

        public RelayCommand EditTaskCommand { get; set; }

        public KanbanViewModel(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
            _ = LoadTasksAsync();
            EditTaskCommand = new RelayCommand(ExecuteEditTask);
        }

        public async Task LoadTasksAsync()
        {
            var allTasks = (await _taskRepository.GetAllAsync()).OrderBy(t => t.DueDate).ToList();

            ToDoTasks.Clear();
            InProgressTasks.Clear();
            DoneTasks.Clear();
            BlockedTasks.Clear();

            foreach (var task in allTasks)
            {
                switch (task.Status)
                {
                    case "To-Do": ToDoTasks.Add(task); break;
                    case "В процесі": InProgressTasks.Add(task); break;
                    case "Виконано": DoneTasks.Add(task); break;
                    case "Заблоковано": BlockedTasks.Add(task); break;
                    default: ToDoTasks.Add(task); break;
                }
            }
        }

        private void ExecuteEditTask(object obj)
        {
            if (obj is TaskItem task)
            {
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainVM)
                {
                    mainVM.SelectedTask = task;
                }
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is TaskItem && dropInfo.TargetCollection is ObservableCollection<TaskItem>)
            {
                dropInfo.Effects = System.Windows.DragDropEffects.Move;
            }
        }

        public async void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is TaskItem task && dropInfo.TargetCollection is ObservableCollection<TaskItem> targetList)
            {
                if (targetList == ToDoTasks) task.Status = "To-Do";
                else if (targetList == InProgressTasks) task.Status = "В процесі";
                else if (targetList == DoneTasks) task.Status = "Виконано";
                else if (targetList == BlockedTasks) task.Status = "Заблоковано";
                await App.TaskRepository.SaveAsync(task);
                await LoadTasksAsync();

                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainVM)
                {
                    _ = mainVM.LoadTasksAsync();
                }
            }
        }
    }
}
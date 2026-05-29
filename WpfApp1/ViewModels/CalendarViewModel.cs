using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GongSolutions.Wpf.DragDrop;
using WpfApp1.Core;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class CalendarViewModel : ObservableObject, IDropTarget
    {
        private readonly ITaskRepository _taskRepository;

        private DateTime _currentStartDate;
        private int _daysToDisplay = 7;

        public int DaysToDisplay
        {
            get => _daysToDisplay;
            set
            {
                _daysToDisplay = value;
                OnPropertyChanged();
                _ = LoadCalendarAsync();
            }
        }

        private string _monthYearHeader;
        public string MonthYearHeader
        {
            get => _monthYearHeader;
            set { _monthYearHeader = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Hours { get; } = new ObservableCollection<string>();
        private ObservableCollection<CalendarDay> _days;

        public ObservableCollection<CalendarDay> Days
        {
            get => _days;
            set { _days = value; OnPropertyChanged(); }
        }

        public RelayCommand NextCommand { get; set; }
        public RelayCommand PreviousCommand { get; set; }
        public RelayCommand SetScaleCommand { get; set; }
        public RelayCommand EditTaskCommand { get; set; }

        public CalendarViewModel(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;

            _currentStartDate = GetStartOfWeek(DateTime.Today);
            NextCommand = new RelayCommand(ExecuteNext);
            PreviousCommand = new RelayCommand(ExecutePrevious);
            EditTaskCommand = new RelayCommand(ExecuteEditTask);

            SetScaleCommand = new RelayCommand(param =>
            {
                if (int.TryParse(param.ToString(), out int scale))
                {
                    DaysToDisplay = scale;
                }
            });
            for (int i = 0; i < 24; i++) Hours.Add($"{i:D2}:00");

            _ = LoadCalendarAsync();
        }

        public void GoToDate(DateTime date)
        {
            _currentStartDate = GetStartOfWeek(date);
            _ = LoadCalendarAsync();
        }

        public async Task LoadCalendarAsync()
        {
            ObservableCollection<CalendarDay> newDays = new();
            var allTasks = await _taskRepository.GetAllAsync();

            for (int i = 0; i < DaysToDisplay; i++)
            {
                var targetDate = _currentStartDate.AddDays(i);
                CalendarDay newDay = new()
                {
                    Date = targetDate,
                    Tasks = new ObservableCollection<TaskItem>(allTasks.Where(t => t.DueDate.Date == targetDate.Date))
                };
                newDays.Add(newDay);
            }

            Days = newDays;
            var ukCulture = new CultureInfo("uk-UA");
            string monthName = _currentStartDate.ToString("MMMM yyyy", ukCulture);
            MonthYearHeader = char.ToUpper(monthName[0]) + monthName[1..];
        }

        private void ExecuteNext(object obj)
        {
            _currentStartDate = _currentStartDate.AddDays(DaysToDisplay);
            _ = LoadCalendarAsync();
        }

        private void ExecutePrevious(object obj)
        {
            _currentStartDate = _currentStartDate.AddDays(-DaysToDisplay);
            _ = LoadCalendarAsync();
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

        private static DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff).Date;
        }

        public async void MoveTaskToDate(TaskItem task, DateTime newDate)
        {
            if (task == null) return;

            task.DueDate = newDate;
            task.LastUpdated = DateTime.Now;

            await _taskRepository.SaveAsync(task);
            await LoadCalendarAsync();

            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow?.DataContext is MainViewModel mainVM)
            {
                _ = mainVM.LoadTasksAsync();
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is TaskItem)
            {
                dropInfo.Effects = System.Windows.DragDropEffects.Move;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is TaskItem task)
            {
                CalendarDay targetDay = null;

                var element = dropInfo.VisualTarget as System.Windows.DependencyObject;
                while (element != null)
                {
                    if (element is System.Windows.FrameworkElement fe && fe.DataContext is CalendarDay day)
                    {
                        targetDay = day;
                        break;
                    }
                    element = System.Windows.Media.VisualTreeHelper.GetParent(element);
                }

                if (targetDay != null)
                {
                    double dropY = dropInfo.DropPosition.Y;
                    int totalMinutes = (int)Math.Max(0, dropY);
                    int snappedMinutes = (int)Math.Round(totalMinutes / 5.0) * 5;
                    int hours = Math.Min(23, snappedMinutes / 60);
                    int minutes = Math.Min(55, snappedMinutes % 60);

                    DateTime newDate = new DateTime(targetDay.Date.Year, targetDay.Date.Month, targetDay.Date.Day, hours, minutes, 0);
                    MoveTaskToDate(task, newDate);
                }
            }
        }
    }
}
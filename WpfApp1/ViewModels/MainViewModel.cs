using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Core;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly ITaskRepository _taskRepository;

        public bool IsMenuVisible => !(_currentViewModel is LoginViewModel);

        private ObservableCollection<TaskItem> _tasks;
        public ObservableCollection<TaskItem> Tasks
        {
            get => _tasks;
            set { _tasks = value; OnPropertyChanged(); }
        }

        private object _currentViewModel;
        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMenuVisible));
            }
        }

        private TaskItem _selectedTask;
        public TaskItem SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                OnPropertyChanged();

                if (_selectedTask != null)
                {
                    CurrentViewModel = new TaskEditorViewModel(_taskRepository, async () => await LoadTasksAsync(), _selectedTask);
                }
            }
        }

        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
                UpdateCalendarView();
            }
        }

        public RelayCommand ShowCalendarCommand { get; set; }
        public RelayCommand AddPageCommand { get; set; }
        public RelayCommand ShowTimerCommand { get; set; }
        public RelayCommand ShowKanbanCommand { get; set; }
        public RelayCommand ShowAnalyticsCommand { get; set; }

        public RelayCommand ToggleThemeCommand { get; set; }

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set { _isDarkMode = value; OnPropertyChanged(); }
        }

        private void ShowLoginScreen()
        {
            CurrentViewModel = new LoginViewModel(_taskRepository, () =>
            {
                _ = LoadTasksAsync();
                CurrentViewModel = new CalendarViewModel(_taskRepository);
            });
        }

        public MainViewModel(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;

            InitializeCommands();
            ShowLoginScreen();
        }

        private void InitializeCommands()
        {
            ShowCalendarCommand = new RelayCommand(o => CurrentViewModel = new CalendarViewModel(_taskRepository));
            AddPageCommand = new RelayCommand(o => CurrentViewModel = new TaskEditorViewModel(_taskRepository, async () => await LoadTasksAsync()));
            ShowTimerCommand = new RelayCommand(o => CurrentViewModel = new TimeTrackerViewModel(_taskRepository));
            ShowKanbanCommand = new RelayCommand(o => CurrentViewModel = new KanbanViewModel(_taskRepository));
            ShowAnalyticsCommand = new RelayCommand(o => CurrentViewModel = new AnalyticsViewModel(_taskRepository));

            ToggleThemeCommand = new RelayCommand(o => 
            {
                ThemeManager.ToggleTheme();
                IsDarkMode = ThemeManager.IsDarkMode;
            });
        }

        public async Task LoadTasksAsync()
        {
            try
            {
                var tasksList = await _taskRepository.GetAllAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Tasks = new ObservableCollection<TaskItem>(tasksList);
                });
            }
            catch (Exception)
            {
                MessageBox.Show("Виникла помилка при оновленні списку завдань.");
            }
        }

        private void UpdateCalendarView()
        {
            if (CurrentViewModel is CalendarViewModel calendarVM)
            {
                calendarVM.GoToDate(_selectedDate);
            }
            else
            {
                var newCalendarVM = new CalendarViewModel(_taskRepository);
                newCalendarVM.GoToDate(_selectedDate);
                CurrentViewModel = newCalendarVM;
            }
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WpfApp1.Core;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class TimeTrackerViewModel : ObservableObject
    {
        private readonly DispatcherTimer _timer;
        private int _timeRemainingSeconds;
        
        private const int POMODORO_WORK_MINUTES = 25;
        private const int POMODORO_SHORT_BREAK_MINUTES = 5;
        private const int POMODORO_LONG_BREAK_MINUTES = 15;
        private const int POMODORO_CYCLES_BEFORE_LONG_BREAK = 4;

        private bool _isPomodoroEnabled;
        private bool _isPomodoroBreak;
        private int _pomodoroSecondsRemaining;
        private int _pomodoroCompletedCycles;
        private readonly DispatcherTimer _pomodoroBreakTimer;
        private int _elapsedSecondsTotal;
        private DateTime _sessionStartTime;

        private ObservableCollection<TaskItem>? _availableTasks;
        public ObservableCollection<TaskItem>? AvailableTasks
        {
            get { return _availableTasks; }
            set { _availableTasks = value; OnPropertyChanged(); }
        }

        private TaskItem? _selectedTask;
        public TaskItem? SelectedTask
        {
            get { return _selectedTask; }
            set
            {
                _selectedTask = value;
                OnPropertyChanged();

                if (_selectedTask != null)
                {
                    TaskProgressInfo = $"Ви вибрали: {_selectedTask.Title}";
                    _timeRemainingSeconds = _selectedTask.EstimatedTimeMinutes * 60;
                    _elapsedSecondsTotal = 0;
                    UpdateTimeDisplay();
                }
                else
                {
                    TaskProgressInfo = "Оберіть завдання для початку роботи";
                    CurrentTimeDisplay = "00:00:00";
                }
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _currentTimeDisplay = "00:00:00";
        public string CurrentTimeDisplay
        {
            get { return _currentTimeDisplay; }
            set { _currentTimeDisplay = value; OnPropertyChanged(); }
        }

        private string _taskProgressInfo = "Оберіть завдання для початку роботи";
        public string TaskProgressInfo
        {
            get { return _taskProgressInfo; }
            set { _taskProgressInfo = value; OnPropertyChanged(); }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                _isRunning = value;
                OnPropertyChanged();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _isTimeUpPanelVisible;
        public bool IsTimeUpPanelVisible
        {
            get => _isTimeUpPanelVisible;
            set { _isTimeUpPanelVisible = value; OnPropertyChanged(); }
        }

        public bool IsPomodoroEnabled
        {
            get => _isPomodoroEnabled;
            set
            {
                _isPomodoroEnabled = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PomodoroStatusText));
                if (value)
                    StartPomodoroWork();
                else
                    StopPomodoro();
            }
        }

        private string _pomodoroTimeDisplay = "25:00";
        public string PomodoroTimeDisplay
        {
            get => _pomodoroTimeDisplay;
            set { _pomodoroTimeDisplay = value; OnPropertyChanged(); }
        }

        private string _pomodoroPhaseText = "РОБОТА";
        public string PomodoroPhaseText
        {
            get => _pomodoroPhaseText;
            set { _pomodoroPhaseText = value; OnPropertyChanged(); }
        }

        public string PomodoroStatusText =>
            _isPomodoroEnabled
                ? $"Цикл {_pomodoroCompletedCycles + 1} / {POMODORO_CYCLES_BEFORE_LONG_BREAK}"
                : "Вимкнено";

        private string _pomodoroPhaseIcon = "🍅";
        public string PomodoroPhaseIcon
        {
            get => _pomodoroPhaseIcon;
            set { _pomodoroPhaseIcon = value; OnPropertyChanged(); }
        }

        public RelayCommand StartCommand { get; set; }
        public RelayCommand PauseCommand { get; set; }
        public RelayCommand StopCommand { get; set; }
        public RelayCommand ExtendCommand { get; set; }
        public RelayCommand FinishCommand { get; set; }
        public RelayCommand TogglePomodoroCommand { get; set; }

        private readonly ITaskRepository _taskRepository;

        public TimeTrackerViewModel(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
            _ = LoadTasksAsync();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Time_Tick;

            _pomodoroBreakTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _pomodoroBreakTimer.Tick += PomodoroBreakTimer_Tick;

            StartCommand = new RelayCommand(ExecuteStart, CanStart);
            PauseCommand = new RelayCommand(ExecutePause, CanPause);
            StopCommand = new RelayCommand(ExecuteStop, CanStop);

            ExtendCommand = new RelayCommand(ExecuteExtend);
            FinishCommand = new RelayCommand(ExecuteFinish);
            TogglePomodoroCommand = new RelayCommand(o => IsPomodoroEnabled = !IsPomodoroEnabled);
        }

        public async Task LoadTasksAsync()
        {
            try
            {
                var allTasks = await _taskRepository.GetAllAsync();
                var tasks = allTasks
                              .Where(t => t.IsTask && t.Status != "Виконано")
                              .OrderBy(t => t.DueDate)
                              .ToList();

                AvailableTasks = [.. tasks];
            }
            catch (Exception)
            {
                MessageBox.Show("Помилка завантаження завдань для трекера.");
            }
        }

        private void Time_Tick(object? sender, EventArgs e)
        {
            _elapsedSecondsTotal++;

            if (_timeRemainingSeconds > 0)
            {
                _timeRemainingSeconds--;
                UpdateTimeDisplay();
            }
            else
            {
                ExecutePause(null);
                IsTimeUpPanelVisible = true;
            }

            if (_isPomodoroEnabled && !_isPomodoroBreak && _pomodoroSecondsRemaining > 0)
            {
                _pomodoroSecondsRemaining--;
                UpdatePomodoroDisplay();

                if (_pomodoroSecondsRemaining == 0)
                {
                    ExecutePause(null);
                    StartPomodoroBreak();
                }
            }
        }

        private void UpdateTimeDisplay()
        {
            TimeSpan time = TimeSpan.FromSeconds(_timeRemainingSeconds);
            CurrentTimeDisplay = time.ToString(@"hh\:mm\:ss");
        }

        private bool CanStart(object? obj) => SelectedTask != null && !IsRunning && _timeRemainingSeconds > 0;

        private void ExecuteStart(object? obj)
        {
            if (_elapsedSecondsTotal == 0) _sessionStartTime = DateTime.Now;
            _timer.Start();
            IsRunning = true;

            if (_isPomodoroEnabled && _isPomodoroBreak)
            {
                _timer.Stop();
                IsRunning = false;
                MessageBox.Show($"Зараз перерва Помодоро! Залишилось {PomodoroTimeDisplay}",
                    "Перерва 🍅", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool CanPause(object? obj) => IsRunning;

        private void ExecutePause(object? obj)
        {
            _timer.Stop();
            IsRunning = false;
        }

        private bool CanStop(object? obj) => _elapsedSecondsTotal > 0;

        private async void ExecuteStop(object? obj)
        {
            await SaveTimeSessionAsync();
            SelectedTask = null;
        }

        private async void ExecuteExtend(object? obj)
        {
            if (SelectedTask == null || !int.TryParse(obj?.ToString(), out int extraMinutes)) return;

            var allTasks = await _taskRepository.GetAllAsync();
            var allTasksToday = allTasks
                .Where(t => t.DueDate.Date == DateTime.Today && t.Id != SelectedTask.Id)
                .ToList();

            DateTime currentNewEndTime = SelectedTask.DueDate.AddMinutes(SelectedTask.EstimatedTimeMinutes + extraMinutes);
            bool hasCollision = allTasksToday.Any(t =>
                t.IsTimeLocked &&
                currentNewEndTime > t.DueDate &&
                SelectedTask.DueDate < t.DueDate.AddMinutes(t.EstimatedTimeMinutes));

            var subsequentTasks = allTasksToday.Where(t => t.DueDate >= SelectedTask.DueDate && !t.IsTimeLocked).ToList();

            if (!hasCollision)
            {
                foreach (var task in subsequentTasks)
                {
                    DateTime shiftedStart = task.DueDate.AddMinutes(extraMinutes);
                    DateTime shiftedEnd = shiftedStart.AddMinutes(task.EstimatedTimeMinutes);

                    if (allTasksToday.Any(t =>
                            t.IsTimeLocked &&
                            shiftedEnd > t.DueDate &&
                            shiftedStart < t.DueDate.AddMinutes(t.EstimatedTimeMinutes)))
                    {
                        hasCollision = true;
                        break;
                    }
                }
            }

            if (hasCollision)
            {
                MessageBox.Show("Краще доробити пізніше, бо у вас заплановані важливіші справи!",
                    "Увага: Перетин графіку", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedTask.EstimatedTimeMinutes += extraMinutes;
            await _taskRepository.SaveAsync(SelectedTask);

            foreach (var task in subsequentTasks)
            {
                task.DueDate = task.DueDate.AddMinutes(extraMinutes);
                await _taskRepository.SaveAsync(task);
            }

            IsTimeUpPanelVisible = false;
            _timeRemainingSeconds += extraMinutes * 60;
            UpdateTimeDisplay();
            ExecuteStart(null);
        }

        private async void ExecuteFinish(object? obj)
        {
            await SaveTimeSessionAsync();

            if (SelectedTask != null)
            {
                SelectedTask.Status = "Виконано";
                await _taskRepository.SaveAsync(SelectedTask);
            }

            IsTimeUpPanelVisible = false;
            SelectedTask = null;
            await LoadTasksAsync();
        }

        private async Task SaveTimeSessionAsync()
        {
            ExecutePause(null);
            if (SelectedTask == null || _elapsedSecondsTotal == 0) return;

            int minutes = _elapsedSecondsTotal / 60;
            if (minutes == 0) minutes = 1;

            var timeSession = new TimeItem
            {
                TaskId = SelectedTask.Id,
                StartTime = _sessionStartTime,
                EndTime = DateTime.Now,
                DurationMinutes = minutes
            };

            try
            {
                await _taskRepository.SaveTimeSessionAsync(timeSession);
                MessageBox.Show($"Сесія збережена! Ви працювали над '{SelectedTask.Title}' протягом {minutes} хвилин.");
            }
            catch (Exception)
            {
                MessageBox.Show("Помилка збереження сесії.");
            }

            _elapsedSecondsTotal = 0;
        }

        private void StartPomodoroWork()
        {
            _isPomodoroBreak = false;
            _pomodoroSecondsRemaining = POMODORO_WORK_MINUTES * 60;
            PomodoroPhaseText = "РОБОТА";
            PomodoroPhaseIcon = "🍅";
            UpdatePomodoroDisplay();
            OnPropertyChanged(nameof(PomodoroStatusText));
        }

        private void StartPomodoroBreak()
        {
            _isPomodoroBreak = true;
            _pomodoroCompletedCycles++;

            bool isLongBreak = _pomodoroCompletedCycles % POMODORO_CYCLES_BEFORE_LONG_BREAK == 0;
            int breakMinutes = isLongBreak ? POMODORO_LONG_BREAK_MINUTES : POMODORO_SHORT_BREAK_MINUTES;

            _pomodoroSecondsRemaining = breakMinutes * 60;
            PomodoroPhaseText = isLongBreak ? "ДОВГА ПЕРЕРВА" : "КОРОТКА ПЕРЕРВА";
            PomodoroPhaseIcon = "☕";
            UpdatePomodoroDisplay();
            OnPropertyChanged(nameof(PomodoroStatusText));

            _pomodoroBreakTimer.Start();

            MessageBox.Show(
                $"Час перерви! {(isLongBreak ? "Довга" : "Коротка")} перерва — {breakMinutes} хвилин.\nВідпочиньте та поверніться до роботи!",
                "Перерва Помодоро 🍅", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PomodoroBreakTimer_Tick(object? sender, EventArgs e)
        {
            if (_pomodoroSecondsRemaining > 0)
            {
                _pomodoroSecondsRemaining--;
                UpdatePomodoroDisplay();
            }
            else
            {
                _pomodoroBreakTimer.Stop();
                StartPomodoroWork();
                MessageBox.Show("Перерва завершена! Час працювати! 💪",
                    "Помодоро 🍅", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void StopPomodoro()
        {
            _pomodoroBreakTimer.Stop();
            _isPomodoroBreak = false;
            _pomodoroCompletedCycles = 0;
            _pomodoroSecondsRemaining = 0;
            PomodoroPhaseText = "РОБОТА";
            PomodoroPhaseIcon = "🍅";
            PomodoroTimeDisplay = "25:00";
            OnPropertyChanged(nameof(PomodoroStatusText));
        }

        private void UpdatePomodoroDisplay()
        {
            TimeSpan t = TimeSpan.FromSeconds(_pomodoroSecondsRemaining);
            PomodoroTimeDisplay = t.ToString(@"mm\:ss");
        }
    }
}
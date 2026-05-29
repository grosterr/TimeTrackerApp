using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.Core;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class TaskEditorViewModel : ObservableObject
    {
        private string _taskTitle;
        private int _taskId;
        private Action _onChanged;
        private bool _isTask;
        private string _status;

        public string Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged(); }
        }

        private string _priority;
        public string Priority
        {
            get { return _priority; }
            set { _priority = value; OnPropertyChanged(); }
        }

        public List<string> Statuses { get; } = new List<string> { "To-Do", "В процесі", "Виконано", "Заблоковано" };
        public List<string> Priorities { get; } = new List<string> { "Низький", "Середній", "Високий", "Критичний" };

        private ObservableCollection<string> _taskTags = new ObservableCollection<string>();
        public ObservableCollection<string> TaskTags
        {
            get { return _taskTags; }
            set
            {
                _taskTags = value;
                OnPropertyChanged();
            }
        }

        private string _newTagText;
        public string NewTagText
        {
            get { return _newTagText; }
            set { _newTagText = value; OnPropertyChanged(); }
        }

        public bool IsTask
        {
            get { return _isTask; }
            set { _isTask = value; OnPropertyChanged(); }
        }

        private DateTime? _deadTime;
        public DateTime? DeadTime
        {
            get { return _deadTime; }
            set { _deadTime = value; OnPropertyChanged(); }
        }

        private string _startTimeStr;
        public string StartTimeStr
        {
            get { return _startTimeStr; }
            set { _startTimeStr = value; OnPropertyChanged(); }
        }

        private string _estimatedMinutesStr;
        public string EstimatedMinutesStr
        {
            get { return _estimatedMinutesStr; }
            set { _estimatedMinutesStr = value; OnPropertyChanged(); }
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get { return _isEditMode; }
            set
            {
                _isEditMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadOnly));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsReadOnly => !IsEditMode;

        public string TaskTitle
        {
            get { return _taskTitle; }
            set
            {
                _taskTitle = value;
                OnPropertyChanged();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _taskDescription;
        public string TaskDescription
        {
            get { return _taskDescription; }
            set
            {
                _taskDescription = value;
                OnPropertyChanged();
            }
        }

        private bool _isTimeLocked;
        public bool IsTimeLocked
        {
            get { return _isTimeLocked; }
            set { _isTimeLocked = value; OnPropertyChanged(); }
        }

        private bool _isRecurring;
        public bool IsRecurring
        {
            get { return _isRecurring; }
            set { _isRecurring = value; OnPropertyChanged(); }
        }

        private int _recurrenceIntervalDays = 1;
        public int RecurrenceIntervalDays
        {
            get { return _recurrenceIntervalDays; }
            set { _recurrenceIntervalDays = value; OnPropertyChanged(); }
        }

        public RelayCommand AddTagCommand { get; set; }
        public RelayCommand RemoveTagCommand { get; set; }
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand DeleteCommand { get; set; }
        public RelayCommand EditCommand { get; set; }

        public TaskEditorViewModel(ITaskRepository taskRepository, Action onChanged, Models.TaskItem existingTask = null)
        {
            _onChanged = onChanged;

            AddTagCommand = new RelayCommand(o =>
            {
                if (!string.IsNullOrWhiteSpace(NewTagText) && !TaskTags.Contains(NewTagText.Trim()))
                {
                    TaskTags.Add(NewTagText.Trim());
                    NewTagText = string.Empty;
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            });

            RemoveTagCommand = new RelayCommand(tagObj =>
            {
                if (tagObj is string tag && TaskTags.Contains(tag))
                {
                    TaskTags.Remove(tag);
                }
            });

            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            DeleteCommand = new RelayCommand(ExecuteDelete, CanExecuteDelete);
            EditCommand = new RelayCommand(ExecuteEdit, CanExecuteEdit);

            if (existingTask != null)
            {
                _taskId = existingTask.Id;
                TaskTitle = existingTask.Title;
                TaskDescription = existingTask.Description;
                IsTask = existingTask.IsTask;
                IsTimeLocked = existingTask.IsTimeLocked;
                Status = existingTask.Status ?? "To-Do";
                Priority = existingTask.Priority ?? "Середній";

                if (!string.IsNullOrEmpty(existingTask.Tags))
                {
                    var tagsArray = existingTask.Tags.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    TaskTags = new ObservableCollection<string>(tagsArray);
                }

                DeadTime = existingTask.DueDate;
                StartTimeStr = existingTask.DueDate.ToString("HH:mm");
                EstimatedMinutesStr = existingTask.EstimatedTimeMinutes > 0 ? existingTask.EstimatedTimeMinutes.ToString() : string.Empty;
                CustomColor = existingTask.CustomColor ?? "";
                IsRecurring = existingTask.IsRecurring;
                RecurrenceIntervalDays = existingTask.RecurrenceIntervalDays == 0 ? 1 : existingTask.RecurrenceIntervalDays;
                IsEditMode = false;
            }
            else
            {
                Status = "To-Do";
                Priority = "Середній";
                CustomColor = "";
                IsTimeLocked = false;
                IsRecurring = false;
                RecurrenceIntervalDays = 1;
                StartTimeStr = DateTime.Now.ToString("HH:mm");
                IsEditMode = true;
            }
        }

        private bool CanExecuteSave(object parameter)
        {
            return IsEditMode && !string.IsNullOrWhiteSpace(TaskTitle);
        }
        private async void ExecuteSave(object parameter)
        {
            int parsedMinutes = 0;
            int.TryParse(EstimatedMinutesStr, out parsedMinutes);

            DateTime baseDate = DeadTime ?? DateTime.Today;
            int hours = baseDate.Hour;
            int minutes = baseDate.Minute;

            if (!string.IsNullOrWhiteSpace(StartTimeStr))
            {
                var timeParts = StartTimeStr.Split(':');
                if (timeParts.Length == 2)
                {
                    if (int.TryParse(timeParts[0].Trim(), out int h)) hours = Math.Max(0, Math.Min(23, h));
                    if (int.TryParse(timeParts[1].Trim(), out int m)) minutes = Math.Max(0, Math.Min(59, m));
                }
            }

            DateTime finalDueDate = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, hours, minutes, 0);

            var newTask = new Models.TaskItem
            {
                Id = _taskId,
                Title = TaskTitle,
                Description = TaskDescription,
                CreatedDate = DateTime.Now,
                LastUpdated = DateTime.Now,
                IsTask = IsTask,
                IsTimeLocked = IsTimeLocked,
                DueDate = finalDueDate,
                EstimatedTimeMinutes = parsedMinutes,
                Status = Status,
                Priority = Priority,
                CustomColor = CustomColor,
                IsRecurring = IsRecurring,
                RecurrenceIntervalDays = RecurrenceIntervalDays,
                Tags = string.Join("|", TaskTags)
            };

            try
            {
                await App.TaskRepository.SaveAsync(newTask);

                MessageBox.Show("Нотатку успішно збережено", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                IsEditMode = false;
                if (_taskId == 0) _taskId = newTask.Id;

                _onChanged?.Invoke();
            }
            catch (Exception)
            {
                MessageBox.Show("Помилка при збереженні нотатки. Перевірте правильність введених даних.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteDelete(object parameter)
        {
            return _taskId > 0;
        }
        private async void ExecuteDelete(object obj)
        {
            if (MessageBox.Show("Ви впевнені, що хочете видалити цю нотатку?", "Підтвердження видалення", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await App.TaskRepository.DeleteAsync(_taskId);

                    TaskTitle = string.Empty;
                    TaskDescription = string.Empty;
                    EstimatedMinutesStr = string.Empty;
                    DeadTime = null;
                    TaskTags.Clear();
                    IsTask = false;
                    _taskId = 0;
                    IsEditMode = true;

                    MessageBox.Show("Нотатку успішно видалено", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                    _onChanged?.Invoke();
                }
                catch (Exception)
                {
                    MessageBox.Show("Помилка видалення нотатки.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanExecuteEdit(object parameter)
        {
            return _taskId > 0 && !IsEditMode;
        }

        private void ExecuteEdit(object obj)
        {
            IsEditMode = true;
        }

        public class ColorOption
        {
            public string Name { get; set; }
            public string Hex { get; set; }
        }

        private string _customColor;
        public string CustomColor
        {
            get { return _customColor; }
            set { _customColor = value; OnPropertyChanged(); }
        }

        public List<ColorOption> AvailableColors { get; } = new List<ColorOption>
        {
            new ColorOption { Name = "Авто (за пріоритетом)", Hex = "" },
            new ColorOption { Name = "Червоний", Hex = "#FDE7E9" },
            new ColorOption { Name = "Жовтий", Hex = "#FFF4CE" },
            new ColorOption { Name = "Синій", Hex = "#E8F4FD" },
            new ColorOption { Name = "Зелений", Hex = "#DFF6DD" },
            new ColorOption { Name = "Фіолетовий", Hex = "#F3E8FD" },
            new ColorOption { Name = "Сірий", Hex = "#F0F0F0" }
        };
    }
}
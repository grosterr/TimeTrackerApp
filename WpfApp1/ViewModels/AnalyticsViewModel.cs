using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WpfApp1.Core;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class ChartBarItem : ObservableObject
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; }
        public string DisplayValue { get; set; }
    }

    public class AnalyticsViewModel : ObservableObject
    {
        private readonly ITaskRepository _taskRepository;
        private int _totalTasks;
        public int TotalTasks
        {
            get => _totalTasks;
            set { _totalTasks = value; OnPropertyChanged(); }
        }

        private int _todoCount;
        public int TodoCount
        {
            get => _todoCount;
            set { _todoCount = value; OnPropertyChanged(); }
        }

        private int _inProgressCount;
        public int InProgressCount
        {
            get => _inProgressCount;
            set { _inProgressCount = value; OnPropertyChanged(); }
        }

        private int _doneCount;
        public int DoneCount
        {
            get => _doneCount;
            set { _doneCount = value; OnPropertyChanged(); }
        }

        private int _blockedCount;
        public int BlockedCount
        {
            get => _blockedCount;
            set { _blockedCount = value; OnPropertyChanged(); }
        }

        private double _completionPercent;
        public double CompletionPercent
        {
            get => _completionPercent;
            set { _completionPercent = value; OnPropertyChanged(); }
        }

        private string _totalTimeFormatted;
        public string TotalTimeFormatted
        {
            get => _totalTimeFormatted;
            set { _totalTimeFormatted = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ChartBarItem> _timePerDayBars;
        public ObservableCollection<ChartBarItem> TimePerDayBars
        {
            get => _timePerDayBars;
            set { _timePerDayBars = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ChartBarItem> _completionByCategory;
        public ObservableCollection<ChartBarItem> CompletionByCategory
        {
            get => _completionByCategory;
            set { _completionByCategory = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ChartBarItem> _topCategoriesByTime;
        public ObservableCollection<ChartBarItem> TopCategoriesByTime
        {
            get => _topCategoriesByTime;
            set { _topCategoriesByTime = value; OnPropertyChanged(); }
        }

        private string _selectedPeriod = "Тиждень";
        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                _selectedPeriod = value;
                OnPropertyChanged();
                _ = LoadAnalyticsAsync();
            }
        }

        public RelayCommand SetPeriodCommand { get; set; }
        public RelayCommand RefreshCommand { get; set; }

        public AnalyticsViewModel(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
            SetPeriodCommand = new RelayCommand(param =>
            {
                if (param is string period)
                    SelectedPeriod = period;
            });

            RefreshCommand = new RelayCommand(o => _ = LoadAnalyticsAsync());

            _ = LoadAnalyticsAsync();
        }

        public async Task LoadAnalyticsAsync()
        {
            var allTasks = await _taskRepository.GetAllAsync();
            var allSessions = await _taskRepository.GetTimeSessionsAsync();
            CalculateStatusCounts(allTasks);
            BuildTimePerDayChart(allTasks, allSessions);
            BuildCompletionByCategory(allTasks);
            BuildTopCategoriesByTime(allTasks, allSessions);
        }

        private void CalculateStatusCounts(List<TaskItem> tasks)
        {
            TotalTasks = tasks.Count;
            TodoCount = tasks.Count(t => t.Status == "To-Do");
            InProgressCount = tasks.Count(t => t.Status == "В процесі");
            DoneCount = tasks.Count(t => t.Status == "Виконано");
            BlockedCount = tasks.Count(t => t.Status == "Заблоковано");
            CompletionPercent = TotalTasks > 0 ? Math.Round((double)DoneCount / TotalTasks * 100, 1) : 0;
        }

        private void BuildTimePerDayChart(List<TaskItem> tasks, List<TimeItem> sessions)
        {
            int daysBack = SelectedPeriod == "Тиждень" ? 7 : 30;
            var startDate = DateTime.Today.AddDays(-(daysBack - 1));
            var timeByDay = new Dictionary<DateTime, int>();
            for (int i = 0; i < daysBack; i++)
            {
                timeByDay[startDate.AddDays(i)] = 0;
            }

            foreach (var session in sessions)
            {
                var day = session.StartTime.Date;
                if (timeByDay.ContainsKey(day))
                {
                    timeByDay[day] += session.DurationMinutes;
                }
            }

            if (sessions.Count == 0)
            {
                foreach (var task in tasks.Where(t => t.Status == "Виконано"))
                {
                    var day = task.LastUpdated.Date;
                    if (timeByDay.ContainsKey(day))
                    {
                        timeByDay[day] += task.EstimatedTimeMinutes;
                    }
                }
            }

            double maxValue = timeByDay.Values.Any() ? timeByDay.Values.Max() : 1;
            if (maxValue == 0) maxValue = 1;

            int totalMinutes = timeByDay.Values.Sum();
            TotalTimeFormatted = $"{totalMinutes / 60} год {totalMinutes % 60} хв";

            var bars = new ObservableCollection<ChartBarItem>();
            foreach (var kvp in timeByDay.OrderBy(k => k.Key))
            {
                bars.Add(new ChartBarItem
                {
                    Label = kvp.Key.ToString("dd.MM"),
                    Value = kvp.Value,
                    Percentage = kvp.Value / maxValue * 100,
                    Color = "#5B8DEF",
                    DisplayValue = $"{kvp.Value} хв"
                });
            }

            TimePerDayBars = bars;
        }

        private void BuildCompletionByCategory(List<TaskItem> tasks)
        {
            var tagGroups = new Dictionary<string, (int total, int done)>();

            foreach (var task in tasks)
            {
                var tags = task.TagList;
                if (tags.Count == 0)
                    tags = new List<string> { "Без категорії" };

                foreach (var tag in tags)
                {
                    if (!tagGroups.ContainsKey(tag))
                        tagGroups[tag] = (0, 0);

                    var current = tagGroups[tag];
                    tagGroups[tag] = (
                        current.total + 1,
                        current.done + (task.Status == "Виконано" ? 1 : 0)
                    );
                }
            }

            string[] colors = { "#5B8DEF", "#F59E0B", "#10B981", "#EF4444", "#8B5CF6", "#EC4899", "#06B6D4" };

            var bars = new ObservableCollection<ChartBarItem>();
            int colorIndex = 0;
            foreach (var kvp in tagGroups.OrderByDescending(g => g.Value.total).Take(7))
            {
                double pct = kvp.Value.total > 0
                    ? Math.Round((double)kvp.Value.done / kvp.Value.total * 100, 1)
                    : 0;

                bars.Add(new ChartBarItem
                {
                    Label = kvp.Key,
                    Value = kvp.Value.done,
                    Percentage = pct,
                    Color = colors[colorIndex % colors.Length],
                    DisplayValue = $"{pct}% ({kvp.Value.done}/{kvp.Value.total})"
                });
                colorIndex++;
            }

            CompletionByCategory = bars;
        }

        private void BuildTopCategoriesByTime(List<TaskItem> tasks, List<TimeItem> sessions)
        {
            var taskTags = tasks.ToDictionary(t => t.Id, t => t.TagList.Count > 0 ? t.TagList : new List<string> { "Без категорії" });

            var timePerTag = new Dictionary<string, int>();

            foreach (var session in sessions)
            {
                if (taskTags.TryGetValue(session.TaskId, out var tags))
                {
                    foreach (var tag in tags)
                    {
                        if (!timePerTag.ContainsKey(tag))
                            timePerTag[tag] = 0;
                        timePerTag[tag] += session.DurationMinutes;
                    }
                }
            }
            if (sessions.Count == 0)
            {
                foreach (var task in tasks.Where(t => t.Status == "Виконано"))
                {
                    var tags = task.TagList.Count > 0 ? task.TagList : new List<string> { "Без категорії" };
                    foreach (var tag in tags)
                    {
                        if (!timePerTag.ContainsKey(tag))
                            timePerTag[tag] = 0;
                        timePerTag[tag] += task.EstimatedTimeMinutes;
                    }
                }
            }

            double maxValue = timePerTag.Values.Any() ? timePerTag.Values.Max() : 1;
            if (maxValue == 0) maxValue = 1;

            string[] colors = { "#10B981", "#5B8DEF", "#F59E0B", "#EF4444", "#8B5CF6", "#EC4899", "#06B6D4" };

            var bars = new ObservableCollection<ChartBarItem>();
            int colorIndex = 0;
            foreach (var kvp in timePerTag.OrderByDescending(g => g.Value).Take(5))
            {
                bars.Add(new ChartBarItem
                {
                    Label = kvp.Key,
                    Value = kvp.Value,
                    Percentage = kvp.Value / maxValue * 100,
                    Color = colors[colorIndex % colors.Length],
                    DisplayValue = kvp.Value >= 60 ? $"{kvp.Value / 60} год {kvp.Value % 60} хв" : $"{kvp.Value} хв"
                });
                colorIndex++;
            }

            TopCategoriesByTime = bars;
        }
    }
}

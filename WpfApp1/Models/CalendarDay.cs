using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using WpfApp1.Core;

namespace WpfApp1.Models
{
    public class CalendarDay : ObservableObject
    {
        public DateTime Date { get; set; }

        public string DayName => CultureInfo.GetCultureInfo("uk-UA").DateTimeFormat.GetDayName(Date.DayOfWeek);
        public string DayNumber => Date.Day.ToString();

        private ObservableCollection<TaskItem> _tasks = new();

        public ObservableCollection<TaskItem> Tasks
        {
            get => _tasks;
            set
            {
                _tasks = value;
                OnPropertyChanged();
            }
        }
        public bool IsToday => Date.Date == DateTime.Today;

    }
}

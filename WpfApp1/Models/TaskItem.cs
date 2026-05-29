using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1.Models
{
    [Table("Tasks")]
    public class TaskItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "To-Do";
        public string Priority { get; set; } = "Середній";
        public bool IsCompleted { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public int EstimatedTimeMinutes { get; set; }
        public bool IsTask { get; set; }
        public string Tags { get; set; }
        public bool IsTimeLocked { get; set; }
        public string CustomColor { get; set; }
        public bool IsRecurring { get; set; }
        public int RecurrenceIntervalDays { get; set; }
        public int? ParentRecurringId { get; set; }


        public void MarkAsUpdated() => LastUpdated = DateTime.Now;

        public bool IsOverdue => IsTask && !IsCompleted && DueDate < DateTime.Now;

        [Ignore]
        public double CanvasTop => DueDate.Hour * 60 + DueDate.Minute;

        [Ignore]
        public double CanvasHeight => EstimatedTimeMinutes > 40 ? EstimatedTimeMinutes : 40;

        [Ignore]
        public string BackgroundColor
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomColor)) return CustomColor;
                return Priority switch
                {
                    "Критичний" => "#FDE7E9",
                    "Високий" => "#FFF4CE",
                    "Середній" => "#E8F4FD",
                    "Низький" => "#DFF6DD",
                    _ => "#FFFFFF"
                };
            }
        }

        [Ignore]
        public List<string> TagList
        {
            get => string.IsNullOrEmpty(Tags) ? new List<string>() : Tags.Split('|').ToList();
            set => Tags = value != null ? string.Join("|", value) : string.Empty;
        }
    }
}
using SQLite;
using WpfApp1.Models;
using System.Threading.Tasks;
using SQLite;
using System.Collections.Generic;

namespace WpfApp1.Core
{
    public class SqliteTaskRepository : ITaskRepository
    {
        private readonly SQLiteAsyncConnection _db;

        public SqliteTaskRepository(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _db.CreateTableAsync<User>().Wait();
            _db.CreateTableAsync<TaskItem>().Wait();
            _db.CreateTableAsync<TimeItem>().Wait();

        }
        public Task<List<TaskItem>> GetAllAsync()
        {
            return _db.Table<TaskItem>()
                .Where(t => t.UserId == SessionManager.CurrentUserId)
                .ToListAsync();
        }

        public Task<TaskItem> GetByIdAsync(int id) => _db.Table<TaskItem>().Where(i => i.Id == id).FirstOrDefaultAsync();

        public async Task SaveAsync(TaskItem item)
        {
            bool isCompletingRecurring = false;
            if (item.Id != 0 && item.Status == "Виконано" && item.IsRecurring)
            {
                var existingTask = await GetByIdAsync(item.Id);
                if (existingTask != null && existingTask.Status != "Виконано")
                {
                    isCompletingRecurring = true;
                }
            }

            item.MarkAsUpdated();
            item.UserId = SessionManager.CurrentUserId;
            if (item.Id != 0) await _db.UpdateAsync(item);
            else await _db.InsertAsync(item);

            if (isCompletingRecurring)
            {
                var nextTask = new TaskItem
                {
                    UserId = item.UserId,
                    Title = item.Title,
                    Description = item.Description,
                    Status = "To-Do",
                    Priority = item.Priority,
                    Tags = item.Tags,
                    CustomColor = item.CustomColor,
                    IsTask = item.IsTask,
                    EstimatedTimeMinutes = item.EstimatedTimeMinutes,
                    IsRecurring = item.IsRecurring,
                    RecurrenceIntervalDays = item.RecurrenceIntervalDays,
                    ParentRecurringId = item.ParentRecurringId ?? item.Id,
                    DueDate = System.DateTime.Now.AddDays(item.RecurrenceIntervalDays)
                };
                await _db.InsertAsync(nextTask);
            }
        }

        public Task DeleteAsync(int id) => _db.DeleteAsync<TaskItem>(id);
        public Task SaveTimeSessionAsync(TimeItem session) => _db.InsertAsync(session);
        public Task RegisterUserAsync(User user) => _db.InsertAsync(user);
        public Task<User> GetUserByUsernameAsync(string username) =>
            _db.Table<User>().Where(u => u.Username == username).FirstOrDefaultAsync();

        public Task<List<TimeItem>> GetTimeSessionsAsync() =>
            _db.Table<TimeItem>().ToListAsync();


    }
}
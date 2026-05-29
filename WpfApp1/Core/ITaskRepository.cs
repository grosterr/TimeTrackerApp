using WpfApp1.Models;

public interface ITaskRepository
{
    Task<List<TaskItem>> GetAllAsync();
    Task<TaskItem> GetByIdAsync(int id);
    Task SaveAsync(TaskItem item);
    Task DeleteAsync(int id);
    Task SaveTimeSessionAsync(TimeItem session);
    Task RegisterUserAsync(User user);
    Task<User> GetUserByUsernameAsync(string username);
    Task<List<TimeItem>> GetTimeSessionsAsync();

}
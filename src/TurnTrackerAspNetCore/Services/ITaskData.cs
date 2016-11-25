using System.Collections.Generic;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public interface ITaskData
    {
        IEnumerable<TrackedTask> GetAllTasks();
        TrackedTask GetTask(long id);
        TrackedTask GetTaskDetails(long id);
        TrackedTask Add(TrackedTask newTask);
        void Commit();
        bool TakeTurn(long taskId, string userId);
        bool DeleteTask(long id);
        long DeleteTurn(long id);
        Turn GetTurn(long id);
        IEnumerable<User> GetAllUsers();
    }
}
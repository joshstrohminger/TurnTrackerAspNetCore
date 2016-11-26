using System.Collections.Generic;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public interface ITaskData
    {
        IEnumerable<TrackedTask> GetAllTasks();
        TrackedTask GetTask(long id);
        TrackedTask GetTaskDetails(long id);
        IEnumerable<TrackedTask> GetParticipations(string userId);
        TrackedTask Add(TrackedTask newTask);
        void Commit();
        bool DeleteTask(long id);
        long DeleteTurn(long id);
        Turn GetTurn(long id);
        IEnumerable<User> GetAllUsers();
    }
}
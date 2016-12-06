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
        void DeleteTask(TrackedTask task);
        long DeleteTurn(long id);
        void DeleteTurn(Turn turn);
        Turn GetTurn(long id);
        IEnumerable<User> GetAllUsers();
        IEnumerable<User> GetAllUsersWithRoles();
        Dictionary<long, List<TurnCount>> GetTurnCounts(string userId = null);
        IEnumerable<Turn> GetLatestTurns(params long[] taskIds);
        IEnumerable<TurnCount> GetTurnCounts(long taskId);
    }
}
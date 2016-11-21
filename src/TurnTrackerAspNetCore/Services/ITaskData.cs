using System.Collections.Generic;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public interface ITaskData
    {
        IEnumerable<TrackedTask> GetAll();
        TrackedTask Get(long id);
        TrackedTask Add(TrackedTask newTask);
        void Commit();
        bool TakeTurn(long taskId);
    }
}
using System.Collections.Generic;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public interface ITaskData
    {
        IEnumerable<Task> GetAll();
    }
}
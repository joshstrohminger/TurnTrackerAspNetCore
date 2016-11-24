using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public class SqlTaskData : ITaskData
    {
        private readonly TurnTrackerDbContext _context;

        public SqlTaskData(TurnTrackerDbContext context)
        {
            _context = context;
        }

        public IEnumerable<TrackedTask> GetAll()
        {
            return _context.Tasks;
        }

        public TrackedTask GetTask(long id)
        {
            return _context.Tasks.Find(id);
        }

        public TrackedTask GetTaskDetails(long id)
        {
            //return _context.Tasks.Include(task => task.Turns).ThenInclude(turn => turn.User).FirstOrDefault(task => task.Id == id);
            var task = _context.Tasks.Find(id);
            if (null != task)
            {
                task.Turns = _context.Turns
                    .Include(turn => turn.User)
                    .Where(turn => turn.TrackedTaskId == id)
                    .OrderByDescending(turn => turn.Taken)
                    .ToList();
            }
            return task;
        }

        public TrackedTask Add(TrackedTask newTask)
        {
            _context.Add(newTask);
            return newTask;
        }

        public void Commit()
        {
            _context.SaveChanges();
        }

        public bool TakeTurn(long taskId, string userId)
        {
            var task = GetTaskDetails(taskId);
            if (null != task)
            {
                task.Turns.Add(new Turn {UserId = userId});
                return true;
            }
            return false;
        }

        public bool DeleteTask(long id)
        {
            var task = _context.Tasks.Find(id);
            if (null != task)
            {
                _context.Remove(task);
                return true;
            }
            return false;
        }

        public long DeleteTurn(long id)
        {
            var turn = _context.Turns.Include(x => x.Task).FirstOrDefault(x => x.Id == id);
            if (null == turn)
            {
                return 0;
            }
            _context.Remove(turn);
            return turn.TrackedTaskId;
        }

        public Turn GetTurn(long id)
        {
            return _context.Turns.Find(id);
        }
    }
}

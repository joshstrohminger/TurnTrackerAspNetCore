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

        public TrackedTask Get(long id)
        {
            return _context.Tasks.FirstOrDefault(task => task.Id == id);
        }

        public TrackedTask GetDetails(long id)
        {
            return _context.Tasks.Include(x => x.Turns).FirstOrDefault(task => task.Id == id);
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

        public bool TakeTurn(long taskId)
        {
            var task = GetDetails(taskId);
            if (null != task)
            {
                task.Turns.Add(new Turn());
                return true;
            }
            return false;
        }

        public bool DeleteTask(long id)
        {
            var task = _context.Tasks.FirstOrDefault(x => x.Id == id);
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
    }
}

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

        public IEnumerable<TrackedTask> GetAllTasks()
        {
            return _context.Tasks;
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _context.Users;
        }

        public TrackedTask GetTask(long id)
        {
            return _context.Tasks.Find(id);
        }

        public TrackedTask GetTaskDetails(long id)
        {
            var task = _context.Tasks
                .Include(x => x.User)
                .Include(x => x.Participants)
                .ThenInclude(x => x.User)
                .FirstOrDefault(x => x.Id == id);

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

        public IEnumerable<TrackedTask> GetParticipations(string userId)
        {
            return _context.Participants.Include(x => x.Task).Where(x => x.UserId == userId).Select(x => x.Task);
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

        public void DeleteTask(TrackedTask task)
        {
            _context.Remove(task);
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

        public void DeleteTurn(Turn turn)
        {
            _context.Remove(turn);
        }

        public Turn GetTurn(long id)
        {
            return _context.Turns.Find(id);
        }
    }
}

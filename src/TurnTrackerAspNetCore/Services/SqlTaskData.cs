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
            _context.SaveChanges();
            return newTask;
        }

        public void Commit()
        {
            _context.SaveChanges();
        }

        public bool TakeTurn(long taskId)
        {
            _context.Add(new Turn {TrackedTaskId = taskId});
            return _context.SaveChanges() > 0;
        }
    }
}

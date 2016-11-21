using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TurnTrackerAspNetCore.Entities;

namespace TurnTrackerAspNetCore.Services
{
    public class SqlTaskData : ITaskData
    {
        private TurnTrackerDbContext _context;

        public SqlTaskData(TurnTrackerDbContext context)
        {
            _context = context;
        }

        public IEnumerable<TrackedTask> GetAll()
        {
            throw new NotImplementedException();
        }

        public TrackedTask Get(long id)
        {
            throw new NotImplementedException();
        }

        public TrackedTask Add(TrackedTask newTask)
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }

        public bool TakeTurn(long taskId)
        {
            throw new NotImplementedException();
        }
    }
}

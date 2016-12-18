using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
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

        public List<SiteSetting> GetSiteSettings()
        {
            return _context.SiteSettings.OrderBy(x => x.Name).ToList();
        }

        public IEnumerable<Invite> GetAllInvites()
        {
            return _context.Invites.Include(x => x.Invitee).Include(x => x.Inviter);
        }

        public void AddInvite(Invite invite)
        {
            _context.Add(invite);
        }

        public void DeleteInvite(Invite invite)
        {
            _context.Remove(invite);
        }

        public Invite GetInvite(Guid token)
        {
            return _context.Invites.Find(token);
        }

        public void Add(SiteSetting setting)
        {
            _context.Add(setting);
        }

        public void Remove(SiteSetting setting)
        {
            _context.Remove(setting);
        }

        public IEnumerable<TrackedTask> GetAllTasks()
        {
            return _context.Tasks;
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _context.Users;
        }

        public IEnumerable<User> GetAllUsersWithRoles()
        {
            return _context.Users.Include(x => x.Roles);
        }

        public IEnumerable<Turn> GetLatestTurns(params long[] taskIds)
        {
            if (taskIds.Length == 0)
            {
                return new List<Turn>();
            }
            var sb = new StringBuilder("with o as (select *, ROW_NUMBER() OVER (PARTITION BY TaskId ORDER BY Taken DESC) AS rn FROM Turns where TaskId in (");
            sb.Append(string.Join(",", Enumerable.Range(0, taskIds.Length).Select(i => $"{{{i}}}")));
            sb.Append(")) select * from o where rn = 1;");
            var sql = sb.ToString();
            return _context.Turns
                .FromSql(
                    //"with o as (select *, ROW_NUMBER() OVER (PARTITION BY TaskId ORDER BY Taken DESC) AS rn FROM Turns where TaskId in ({0})) select * from o where rn = 1;",
                    sql,
                    taskIds.Cast<object>().ToArray());
        }

        public Dictionary<long, List<TurnCount>> GetTurnCounts(string userId = null)
        {
            var sql = string.IsNullOrWhiteSpace(userId)
                ? _context.TurnCounts.FromSql("SELECT u.UserName, u.DisplayName, t.Name as TaskName, t.Id as TaskId, count(r.Taken) + p.Offset as TotalTurns, p.UserId from Participants p inner join Tasks t on p.TaskId = t.Id inner join AspNetUsers u on p.UserId = u.Id left join Turns r on r.TaskId = p.TaskId and r.UserId = p.UserId group by p.TaskId, p.UserId, u.UserName, u.DisplayName, t.Name, t.Id, p.Offset order by t.Id, TotalTurns, u.UserName;")
                : _context.TurnCounts.FromSql("SELECT u.UserName, u.DisplayName, t.Name as TaskName, t.Id as TaskId, count(r.Taken) + p.Offset as TotalTurns, p.UserId from Participants p inner join Tasks t on p.TaskId = t.Id inner join AspNetUsers u on p.UserId = u.Id left join Turns r on r.TaskId = p.TaskId and r.UserId = p.UserId where p.TaskId in (select p.TaskId from Participants p where p.UserId = {0}) or t.UserId = {0} group by p.TaskId, p.UserId, u.UserName, u.DisplayName, t.Name, t.Id, p.Offset order by t.Id, TotalTurns, u.UserName;", userId);
            return sql
                .ToList()
                .GroupBy(x => x.TaskId)
                .ToDictionary(x => x.Key, x => x.ToList());
        }

        public IEnumerable<TurnCount> GetTurnCounts(long taskId)
        {
            return _context.TurnCounts
                .FromSql(
                    "SELECT u.UserName, u.DisplayName, t.Name as TaskName, t.Id as TaskId, count(r.Taken) + p.Offset as TotalTurns, p.UserId from Participants p inner join Tasks t on p.TaskId = t.Id inner join AspNetUsers u on p.UserId = u.Id left join Turns r on r.TaskId = p.TaskId and r.UserId = p.UserId where p.TaskId = {0} group by p.TaskId, p.UserId, u.UserName, u.DisplayName, t.Name, t.Id, p.Offset order by t.Id, TotalTurns, u.UserName;",
                    taskId);
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
                    .Where(turn => turn.TaskId == id)
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
            return turn.TaskId;
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

using NextechAREvents.Models;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks;

namespace NextechAREvents.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<EventModel> EventRepository { get; }
        IGenericRepository<AttendeeModel> AttendeeRepository { get; }

        void SaveChanges();
        Task SaveChangesAsync();
        void InitializeDatabase();

        IDbContextTransaction BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();
    }
}

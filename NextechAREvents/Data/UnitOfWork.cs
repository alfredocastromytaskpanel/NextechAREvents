using NextechAREvents.Models;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextechAREvents.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private EventContext context;
        private IDbContextTransaction transaction;

        private IGenericRepository<EventModel> eventRepository;

        private IGenericRepository<AttendeeModel> attendeeRepository;

        public UnitOfWork(EventContext context)
        {
            this.context = context;
        }

        public void SaveChanges()
        {
            context.SaveChanges();
        }

        public async Task SaveChangesAsync()
        {
            await context.SaveChangesAsync();
        }

        public void InitializeDatabase()
        {

        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    context.Dispose();
                    if (transaction != null)
                    {
                        transaction.Dispose();
                        transaction = null;
                    }
                }
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //See documentation about transactions in EFCore here
        //https://docs.microsoft.com/en-us/ef/core/saving/transactions
        //There is more sofisticated ways to do this...
        //
        /// <summary>
        /// This can be used in tow ways:
        /// First way:
        ///     try
        ///     {
        ///         unitOfWork.BeginTransaction();
        ///         ...
        ///         unitOfWork.CommitTransaction();
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         unitOfWork.RollbackTransaction();
        ///     }
        ///     
        /// Second way:
        ///     using (var transaction = unitOfWork.BeginTransaction())
        ///     {
        ///         try
        ///         {
        ///             ...
        ///             transaction.Commit();
        ///         }
        ///         catch (Exception)
        ///         {
        ///             //transaction will auto-rollback when disposed if either commands fails
        ///         }
        ///     }
        /// </summary>
        public IDbContextTransaction BeginTransaction()
        {
            if (transaction == null)
            {
                transaction = context.Database.BeginTransaction();
            }
            return transaction;
        }

        public void CommitTransaction()
        {
            if (transaction != null)
            {
                transaction.Commit();
                transaction.Dispose();
                transaction = null;
            }
        }

        public void RollbackTransaction()
        {
            if (transaction != null)
            {
                transaction.Rollback(); //Not necesary, transaction will auto-rollback when disposed if either commands fails
                transaction.Dispose();
                transaction = null;
            }
        }

        public IGenericRepository<EventModel> EventRepository
        {
            get
            {
                if (eventRepository == null)
                {
                    eventRepository = new GenericRepository<EventModel>(context);
                }
                return eventRepository;
            }
        }


        public IGenericRepository<AttendeeModel> AttendeeRepository
        {
            get
            {
                if (attendeeRepository == null)
                {
                    attendeeRepository = new GenericRepository<AttendeeModel>(context);
                }
                return attendeeRepository;
            }
        }
    }
}

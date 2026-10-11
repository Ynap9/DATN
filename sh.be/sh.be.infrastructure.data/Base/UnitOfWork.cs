using Microsoft.EntityFrameworkCore.Storage;
using sh.be.domain.Base;
using sh.be.infrastructure.data.Persistence.Db;

namespace sh.be.infrastructure.data.Base
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ShDbContext _shDbContext;

        public UnitOfWork(ShDbContext shDbContext)
        {
            _shDbContext = shDbContext;
        }

        public int SaveChanges()
        {
            return _shDbContext.SaveChanges();
        }

        public Task<int> SaveChangesAsync()
        {
            return _shDbContext.SaveChangesAsync();
        }

        public async Task<IUnitOfWorkTransaction> BeginTransactionAsync()
        {
            return new UnitOfWorkTransaction(await _shDbContext.Database.BeginTransactionAsync());
        }
    }

    public class UnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public UnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync()
        {
            return _transaction.CommitAsync();
        }

        public Task RollbackAsync()
        {
            return _transaction.RollbackAsync();
        }

        public ValueTask DisposeAsync()
        {
            return _transaction.DisposeAsync();
        }
    }
}

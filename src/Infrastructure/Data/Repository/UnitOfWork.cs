using Microsoft.EntityFrameworkCore.Storage;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork,IScopedDependency
    {
        private readonly LabDbContextSec _labDbContextSec;
        private readonly dbContext _context;
        private IDbContextTransaction _transaction;

        public UnitOfWork(LabDbContextSec labDbContextSec, dbContext context)
        {
            _labDbContextSec = labDbContextSec;
            _context = context;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 所有仓储的变更（Add/Update）都会在这里被 EF Core 捕获并写入数据库
            //await _labDbContextSec.SaveChangesAsync(cancellationToken);
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            // _transaction = await _labDbContextSec.Database.BeginTransactionAsync(cancellationToken);
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                //await  _labDbContextSec.SaveChangesAsync(cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public void Dispose()
        {
            //_labDbContextSec.Dispose();
            _context.Dispose();
            _transaction?.Dispose();
        }
    }
}

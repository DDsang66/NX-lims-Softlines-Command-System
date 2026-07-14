using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Events;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork,IScopedDependency
    {
        private readonly LabDbContextSec _labDbContextSec;
        private readonly dbContext _context;
        private readonly IMediator _mediator; // 注入 MediatR
        private readonly IEventOutbox _eventOutbox;
        private IDbContextTransaction _transaction;

        public UnitOfWork(LabDbContextSec labDbContextSec, dbContext context, IMediator mediator, IEventOutbox eventOutbox)
        {
            _labDbContextSec = labDbContextSec;
            _context = context;
            _mediator = mediator;
            _eventOutbox = eventOutbox;
        }

        /// <summary>
        /// 保存更改，原子性操作
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 所有仓储的变更（Add/Update）都会在这里被 EF Core 捕获并写入数据库
            //await _labDbContextSec.SaveChangesAsync(cancellationToken);
            // 1. 收集事件（保存前）
            var events = CollectDomainEvents();

            // 2. 事件存入 Outbox（同一事务，保证原子性）
            foreach (var @event in events)
            {
                await _eventOutbox.StoreAsync(@event, cancellationToken);
            }

            var result = await _context.SaveChangesAsync(cancellationToken);

            // 4. 清空聚合根事件
            ClearDomainEvents();

            return result;
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            // _transaction = await _labDbContextSec.Database.BeginTransactionAsync(cancellationToken);
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        /// <summary>
        /// 提交事务，保证各个原子操作强一致性
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                //await  _labDbContextSec.SaveChangesAsync(cancellationToken);
                var events = CollectDomainEvents();

                // 3. 事件存入 Outbox
                foreach (var @event in events)
                {
                    await _eventOutbox.StoreAsync(@event, cancellationToken);
                }

                //保存更改
                await _context.SaveChangesAsync(cancellationToken);

                //提交事务
                await _transaction.CommitAsync(cancellationToken);

                ClearDomainEvents();
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

        /// <summary>
        /// 回滚事务
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            //_labDbContextSec.Dispose();
            _context.Dispose();
            _transaction?.Dispose();
        }

        /// <summary>
        /// 收集事件
        /// </summary>
        /// <returns></returns>
        private List<DomainEvent> CollectDomainEvents()
        {
            return _context.ChangeTracker.Entries<IAggregateRoot>()
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();
        }

        /// <summary>
        /// 清除事件
        /// </summary>
        private void ClearDomainEvents()
        {
            _context.ChangeTracker.Entries<IAggregateRoot>()
                .ToList()
                .ForEach(e => e.Entity.ClearDomainEvents());
        }
    }
}

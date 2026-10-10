using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork, IScopedDependency
    {
        private readonly LabDbContextSec _labDbContextSec;
        private readonly dbContext _context;
        private readonly IMediator _mediator;
        private readonly IEventOutbox _eventOutbox;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(
            LabDbContextSec labDbContextSec,
            dbContext context,
            IMediator mediator,
            IEventOutbox eventOutbox)
        {
            _labDbContextSec = labDbContextSec;
            _context = context;
            _mediator = mediator;
            _eventOutbox = eventOutbox;
        }

        /// <summary>
        /// 保存更改（无显式事务版本）—— 业务变更 + Outbox 在同一 SaveChanges 中原子落库
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 1. 收集事件（保存前，从统一收集器拿）
            var events = CollectDomainEvents();

            // 2. 事件写入 Outbox（同一 DbContext，同一 SaveChanges 落库）
            foreach (var @event in events)
            {
                await _eventOutbox.StoreAsync(@event, cancellationToken);
            }

            // 3. 一次性保存业务变更 + Outbox
            var result = await _context.SaveChangesAsync(cancellationToken);

            // 4. 保存成功后清空收集器
            ClearDomainEvents();

            return result;
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return _transaction;
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
                throw new InvalidOperationException("事务尚未开启，无法提交。");

            try
            {
                // 1. 收集事件
                var events = CollectDomainEvents();

                // 2. 事件写入 Outbox
                foreach (var @event in events)
                {
                    await _eventOutbox.StoreAsync(@event, cancellationToken);
                }

                // 3. 保存业务变更 + Outbox
                await _context.SaveChangesAsync(cancellationToken);

                // 4. 提交事务
                await _transaction.CommitAsync(cancellationToken);

                // 5. 提交成功后清空收集器
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
        public async Task RollbackTransactionAsync()
        {
            try
            {
                if (_transaction != null)
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
            _transaction?.Dispose();
            _transaction = null;
        }

        /// <summary>
        /// 收集事件 —— 从统一收集器取，不再反射扫聚合根
        /// </summary>
        private IReadOnlyList<IDomainEvent> CollectDomainEvents()
        {
            return DomainEvents.Get().ToList();
        }

        /// <summary>
        /// 清空事件收集器
        /// </summary>
        private void ClearDomainEvents()
        {
            DomainEvents.Clear();
        }
    }
}
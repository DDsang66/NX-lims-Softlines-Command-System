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
using System.Reflection;
using static Microsoft.IO.RecyclableMemoryStreamManager;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork, IScopedDependency
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

            // 4) 再次保存 Outbox 变化（如果 _eventOutbox.StoreAsync 未保存）
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
        private List<IDomainEvent> CollectDomainEvents()
        {
            var domainEvents = new List<IDomainEvent>();

            var entities = _context.ChangeTracker.Entries()
                .Select(e => e.Entity)
                .Where(e => e != null);

            foreach (var entity in entities)
            {
                var type = entity!.GetType();

                // 判断是否实现了 IAggregateRoot<,>
                var implementsAggregateRoot = type.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<,>));

                if (!implementsAggregateRoot) continue;

                // 读取 DomainEvents 公共属性（若存在）
                var prop = type.GetProperty("DomainEvents", BindingFlags.Instance | BindingFlags.Public);
                if (prop == null) continue;

                if (prop.GetValue(entity) is IEnumerable<IDomainEvent> events)
                {
                    domainEvents.AddRange(events);
                }
            }

            return domainEvents;
        }

        /// <summary>
        /// 清除事件
        /// </summary>
        private void ClearDomainEvents()
        {
            //_context.ChangeTracker.Entries<IAggregateRoot>()
            //    .ToList()
            //    .ForEach(e => e.Entity.ClearDomainEvents());


            var entities = _context.ChangeTracker.Entries()
                .Select(e => e.Entity)
                .Where(e => e != null);

            foreach (var entity in entities)
            {
                var type = entity!.GetType();

                // 判断是否实现了 IAggregateRoot<,>
                var implementsAggregateRoot = type.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<,>));

                if (!implementsAggregateRoot) continue;

                // 尝试调用 ClearDomainEvents 方法（可能是接口或基类公开的方法）
                var method = type.GetMethod("ClearDomainEvents", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                method?.Invoke(entity, null);
            }
        }
    }
}

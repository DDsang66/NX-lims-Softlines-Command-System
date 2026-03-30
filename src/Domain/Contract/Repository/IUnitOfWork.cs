namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        // 提交更改的方法
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // 如果需要手动开启事务（可选）
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        Task RollbackTransactionAsync();
    }
}

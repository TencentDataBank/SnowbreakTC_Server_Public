using System.Linq.Expressions;
using SnowbreakTC.Core.Models;

namespace SnowbreakTC.Database.Repositories;

/// <summary>
/// 基础仓储接口
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体对象</returns>
    Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体列表</returns>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据条件查找实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体列表</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据条件查找单个实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体对象</returns>
    Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="pageIndex">页索引（从0开始）</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>>? predicate = null,
        int pageIndex = 0,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的实体</returns>
    Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量创建实体
    /// </summary>
    /// <param name="entities">实体列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的实体列表</returns>
    Task<IEnumerable<T>> CreateManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的实体</returns>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 部分更新实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <param name="updateDefinition">更新定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdatePartialAsync(string id, object updateDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实体（物理删除）
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 软删除实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <param name="deletedBy">删除者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SoftDeleteAsync(string id, string? deletedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除实体
    /// </summary>
    /// <param name="predicate">删除条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的数量</returns>
    Task<long> DeleteManyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计实体数量
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体数量</returns>
    Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 聚合查询
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="pipeline">聚合管道</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>聚合结果</returns>
    Task<IEnumerable<TResult>> AggregateAsync<TResult>(
        object[] pipeline,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// 总记录数
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// 页索引（从0开始）
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageIndex > 0;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages - 1;
}
using Domain.Entities;

namespace Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
   /// <summary>
   ///     根据id 批量获取实体
   /// </summary>
   /// <param name="ids"></param>
   /// <returns></returns>
   Task<IEnumerable<User>> GetByIdAsync(IEnumerable<Guid> ids);

   /// <summary>
   ///     根据用户名查找用户
   /// </summary>
   /// <param name="username"></param>
   /// <returns></returns>
   Task<User?> FindByUsernameAsync(string username);

   /// <summary>
   ///     删除用户的指定角色
   /// </summary>
   /// <param name="userId"></param>
   /// <param name="roleIds"></param>
   /// <returns></returns>
   Task DeleteByUserIdAndRoleIdsAsync(Guid userId, IEnumerable<Guid> roleIds);

   /// <summary>
   ///     添加用户的指定角色
   /// </summary>
   /// <param name="userId"></param>
   /// <param name="roleIds"></param>
   /// <returns></returns>
   Task AddByUserIdAndRoleIdsAsync(Guid userId, IEnumerable<Guid> roleIds);
}
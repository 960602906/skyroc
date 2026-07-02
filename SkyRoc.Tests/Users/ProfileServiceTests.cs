using Application.DTOs.User;
using Application.Exceptions;
using Application.interfaces;
using Application.Mappers;
using Application.Services;
using Application.Validator.User;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Constants;
using Shared.Utils;
using Xunit;

namespace SkyRoc.Tests.Users;

public class ProfileServiceTests
{
    [Fact]
    public async Task GetCurrentProfileAsync_ReturnsAuthenticatedUser()
    {
        await using var context = CreateDbContext();
        var currentUser = CreateUser("current_user", "当前用户");
        var otherUser = CreateUser("other_user", "其他用户");
        context.Users.AddRange(currentUser, otherUser);
        await context.SaveChangesAsync();

        var service = CreateService(context, currentUser.Id);

        var result = await service.GetCurrentProfileAsync();

        Assert.Equal(currentUser.Id, result.Id);
        Assert.Equal(currentUser.Username, result.Username);
        Assert.Equal(currentUser.NickName, result.NickName);
    }

    [Fact]
    public async Task UpdateCurrentProfileAsync_UpdatesOnlyAuthenticatedUser()
    {
        await using var context = CreateDbContext();
        var currentUser = CreateUser("current_user", "当前用户");
        var otherUser = CreateUser("other_user", "其他用户");
        context.Users.AddRange(currentUser, otherUser);
        await context.SaveChangesAsync();

        var service = CreateService(context, currentUser.Id);

        await service.UpdateCurrentProfileAsync(new UpdateProfileDto
        {
            NickName = "更新昵称",
            Gender = GenderType.Female,
            Phone = "13900000000",
            Email = "updated@example.com"
        });

        var updatedCurrentUser = await context.Users.FindAsync(currentUser.Id);
        var unchangedOtherUser = await context.Users.FindAsync(otherUser.Id);
        Assert.NotNull(updatedCurrentUser);
        Assert.Equal("更新昵称", updatedCurrentUser.NickName);
        Assert.Equal(GenderType.Female, updatedCurrentUser.Gender);
        Assert.Equal("13900000000", updatedCurrentUser.Phone);
        Assert.Equal("updated@example.com", updatedCurrentUser.Email);
        Assert.Equal(currentUser.Id, updatedCurrentUser.UpdateBy);
        Assert.Equal("其他用户", unchangedOtherUser!.NickName);
    }

    [Fact]
    public async Task ChangeCurrentPasswordAsync_ChangesPassword_WhenOldPasswordIsCorrect()
    {
        await using var context = CreateDbContext();
        var currentUser = CreateUser("current_user", "当前用户", "OldPassword123!");
        context.Users.Add(currentUser);
        await context.SaveChangesAsync();

        var service = CreateService(context, currentUser.Id);

        await service.ChangeCurrentPasswordAsync(new ChangePasswordDto
        {
            OldPassword = "OldPassword123!",
            NewPassword = "NewPassword456!"
        });

        var updatedUser = await context.Users.FindAsync(currentUser.Id);
        Assert.NotNull(updatedUser);
        Assert.True(PasswordHasher.Verify(updatedUser.PasswordHash, "NewPassword456!"));
        Assert.False(PasswordHasher.Verify(updatedUser.PasswordHash, "OldPassword123!"));
    }

    [Fact]
    public async Task ChangeCurrentPasswordAsync_RejectsChange_WhenOldPasswordIsWrong()
    {
        await using var context = CreateDbContext();
        var currentUser = CreateUser("current_user", "当前用户", "OldPassword123!");
        var originalHash = currentUser.PasswordHash;
        context.Users.Add(currentUser);
        await context.SaveChangesAsync();

        var service = CreateService(context, currentUser.Id);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.ChangeCurrentPasswordAsync(new ChangePasswordDto
            {
                OldPassword = "WrongPassword123!",
                NewPassword = "NewPassword456!"
            }));

        Assert.Equal("旧密码错误", exception.Message);
        Assert.Equal(originalHash, currentUser.PasswordHash);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static UserService CreateService(ApplicationDbContext context, Guid currentUserId)
    {
        var mapper = new MapperConfiguration(configuration =>
            configuration.AddProfile<UserMappingProfile>()).CreateMapper();

        return new UserService(
            new UserRepository(context),
            new RoleRepository(context),
            new UnitOfWork(context),
            mapper,
            new TestCurrentUserService(currentUserId),
            new CreateUserValidator(),
            new UpdateUserValidator(),
            new UpdateProfileValidator(),
            new ChangePasswordValidator(),
            NullLogger<UserService>.Instance);
    }

    private static User CreateUser(string username, string nickName, string password = "Password123!")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            NickName = nickName,
            Gender = GenderType.Male,
            Phone = "13800000000",
            Email = $"{username}@example.com",
            PasswordHash = PasswordHasher.Hash(password)
        };
    }

    private sealed class TestCurrentUserService(Guid userId) : ICurrentUserService
    {
        public Guid? GetUserId() => userId;

        public string? GetUserName() => "test-user";

        public string? GetEmail() => "test-user@example.com";

        public string? GetRole() => null;

        public IReadOnlyList<string> GetRoles() => [];

        public bool HasClaim(string claimType, string claimValue) => false;
    }
}

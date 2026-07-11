using Application.DTOs.Auth;
using Application.Services;
using Application.interfaces;
using Application.interfaces.System;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Constants;
using Shared.Utils;
using Xunit;

namespace SkyRoc.Tests.Authorization;

/// <summary>认证流程登录审计的完整性与故障隔离回归测试。</summary>
public class AuthLoginAuditTests
{
    [Fact]
    public async Task LoginAsync_RecordsFailureOnce_WhenTokenCacheThrows()
    {
        await using var context = CreateDbContext();
        await SeedUserAsync(context);
        var audit = new CapturingLoginAuditService();
        var service = CreateService(context, new TestTokenCacheService { ThrowOnSaveAccess = true }, audit);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginAsync(new LoginReqDto
        {
            Username = "audit-user",
            Password = "correct-password"
        }));

        var entry = Assert.Single(audit.Entries);
        Assert.False(entry.IsSuccess);
        Assert.Equal("登录处理失败", entry.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_RecordsSuccessOnce_WhenLoginCompletes()
    {
        await using var context = CreateDbContext();
        await SeedUserAsync(context);
        var audit = new CapturingLoginAuditService();
        var service = CreateService(context, new TestTokenCacheService(), audit);

        var result = await service.LoginAsync(new LoginReqDto
        {
            Username = "audit-user",
            Password = "correct-password"
        });

        Assert.NotNull(result);
        var entry = Assert.Single(audit.Entries);
        Assert.True(entry.IsSuccess);
        Assert.Null(entry.FailureReason);
    }

    [Fact]
    public async Task LoginAsync_DoesNotChangeSuccessfulResult_WhenAuditThrows()
    {
        await using var context = CreateDbContext();
        await SeedUserAsync(context);
        var audit = new CapturingLoginAuditService { ThrowOnRecord = true };
        var service = CreateService(context, new TestTokenCacheService(), audit);

        var result = await service.LoginAsync(new LoginReqDto
        {
            Username = "audit-user",
            Password = "correct-password"
        });

        Assert.NotNull(result);
        Assert.Equal(1, audit.Attempts);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedUserAsync(ApplicationDbContext context)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "audit-user",
            NickName = "审计用户",
            Email = "audit@example.com",
            Gender = GenderType.Male,
            PasswordHash = PasswordHasher.Hash("correct-password")
        };
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "管理员",
            Code = SeedConstants.AdminRoleCode
        };
        context.AddRange(user, role, new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role
        });
        await context.SaveChangesAsync();
    }

    private static AuthService CreateService(
        ApplicationDbContext context,
        ITokenCacheService tokenCache,
        ILoginAuditService auditService)
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "test-only-secret-key-with-at-least-32-bytes",
            Issuer = "SkyRoc.Tests",
            Audience = "SkyRoc.Tests",
            ExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        });
        return new AuthService(
            new UserRepository(context),
            new RoleRepository(context),
            new MenuRepository(context),
            tokenCache,
            mapper,
            new JwtService(jwtSettings),
            new TestCurrentUserService(),
            auditService,
            jwtSettings);
    }

    private sealed class CapturingLoginAuditService : ILoginAuditService
    {
        public List<LoginAuditCapture> Entries { get; } = [];
        public int Attempts { get; private set; }
        public bool ThrowOnRecord { get; init; }

        public Task RecordAsync(string username, Guid? userId, bool isSuccess, string? failureReason)
        {
            Attempts++;
            if (ThrowOnRecord) throw new InvalidOperationException("audit unavailable");
            Entries.Add(new LoginAuditCapture(username, userId, isSuccess, failureReason));
            return Task.CompletedTask;
        }
    }

    private sealed record LoginAuditCapture(string Username, Guid? UserId, bool IsSuccess, string? FailureReason);

    private sealed class TestTokenCacheService : ITokenCacheService
    {
        public bool ThrowOnSaveAccess { get; init; }

        public Task SaveAccessTokenAsync(AccessTokenCacheDto data, TimeSpan? expire = null) =>
            ThrowOnSaveAccess
                ? throw new InvalidOperationException("cache unavailable")
                : Task.CompletedTask;

        public Task SaveRefreshTokenAsync(RefreshTokenCacheDto data, TimeSpan? expire = null) => Task.CompletedTask;
        public Task<AccessTokenCacheDto?> GetAccessTokenAsync(string jti) => Task.FromResult<AccessTokenCacheDto?>(null);
        public Task<bool> IsAccessTokenValidAsync(string jti) => Task.FromResult(false);
        public Task RevokeAccessTokenAsync(string jti) => Task.CompletedTask;
        public Task<RefreshTokenCacheDto?> GetRefreshTokenAsync(string refreshToken) => Task.FromResult<RefreshTokenCacheDto?>(null);
        public Task RevokeRefreshTokenAsync(string refreshToken) => Task.CompletedTask;
        public Task RevokeAllByUserAsync(Guid userId) => Task.CompletedTask;
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? GetUserId() => null;
        public string? GetUserName() => null;
        public string? GetEmail() => null;
        public string? GetRole() => null;
        public IReadOnlyList<string> GetRoles() => [];
        public bool HasClaim(string claimType, string claimValue) => false;
    }
}

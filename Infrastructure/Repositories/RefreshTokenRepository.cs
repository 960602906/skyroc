using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RefreshTokenRepository(ApplicationDbContext context)
    : Repository<RefreshToken>(context), IRefreshTokenRepository
{
}
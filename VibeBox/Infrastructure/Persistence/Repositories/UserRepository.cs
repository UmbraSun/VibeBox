using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        return _context.Users
            .AnyAsync(x => x.Email == email, cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        return _context.Users
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _context.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }
}
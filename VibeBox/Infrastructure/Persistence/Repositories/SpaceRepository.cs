using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class SpaceRepository : ISpaceRepository
{
    private readonly ApplicationDbContext _context;

    public SpaceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Space space,
        CancellationToken cancellationToken)
    {
        await _context.Spaces.AddAsync(space, cancellationToken);
    }

    public async Task<IReadOnlyList<Space>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _context.Spaces
            .AsNoTracking()
            .Where(space => space.Members.Any(member => member.UserId == userId))
            .OrderByDescending(space => space.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<Space?> GetByIdAsync(
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        return _context.Spaces
            .FirstOrDefaultAsync(
                space => space.Id == spaceId,
                cancellationToken);
    }

    public Task<SpaceMember?> GetMemberAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _context.SpaceMembers
            .FirstOrDefaultAsync(
                member => member.SpaceId == spaceId
                    && member.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<SpaceMember>> GetMembersAsync(
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        return await _context.SpaceMembers
            .AsNoTracking()
            .Where(member => member.SpaceId == spaceId)
            .Include(member => member.User)
            .OrderBy(member => member.JoinedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddMemberAsync(
        SpaceMember member,
        CancellationToken cancellationToken)
    {
        await _context.SpaceMembers.AddAsync(
            member,
            cancellationToken);
    }

    public async void RemoveMember(SpaceMember member)
    {
        _context.SpaceMembers.Remove(member);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
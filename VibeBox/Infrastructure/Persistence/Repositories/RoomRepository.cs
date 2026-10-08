using Application.Common.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence.Repositories;

public sealed class RoomRepository : IRoomRepository
{
    private readonly ApplicationDbContext _context;

    public RoomRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Room room,
        CancellationToken cancellationToken)
    {
        await _context.Rooms.AddAsync(
            room,
            cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
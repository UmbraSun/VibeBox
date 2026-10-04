using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Commands.Register;

public sealed class RegisterUserHandler
    : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new ConflictException("A user with this email already exists.");

        var user = new User(
            Guid.NewGuid(),
            email,
            _passwordHasher.Hash(request.Password),
            DateTime.UtcNow);

        await _userRepository.AddAsync(user, cancellationToken);

        return user.Id;
    }
}
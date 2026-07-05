namespace Persistence;

sealed class DuplicateIdentityEmailException(string normalizedEmail) : Exception($"Identity email already exists: {normalizedEmail}");
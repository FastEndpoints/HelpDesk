namespace Persistence;

sealed class DuplicateEmailException(string normalizedEmail)
    : Exception($"A user profile already exists for email '{normalizedEmail}'.");
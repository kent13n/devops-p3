using DataShare.Infrastructure.Services;
using AwesomeAssertions;
using Xunit;

namespace DataShare.Tests.Unit.Services;

public class FilePasswordHasherTests
{
    private readonly FilePasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesDifferentValueThanInput()
    {
        var hash = _hasher.Hash("mySecret123");

        hash.Should().NotBe("mySecret123");
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_TwoInvocationsProduceDifferentHashes()
    {
        // BCrypt utilise un salt aléatoire → deux hash du même password doivent différer
        var h1 = _hasher.Hash("password");
        var h2 = _hasher.Hash("password");

        h1.Should().NotBe(h2);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("correctPwd");

        _hasher.Verify(hash, "correctPwd").Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("correctPwd");

        _hasher.Verify(hash, "wrongPwd").Should().BeFalse();
    }
}

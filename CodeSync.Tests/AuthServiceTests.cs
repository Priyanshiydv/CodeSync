using NUnit.Framework;
using Moq;
using AuthService.Services;
using AuthService.Interfaces;
using AuthService.Models;
using AuthService.DTOs;
using AuthService.Data;
using AuthService.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CodeSync.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<IUserRepository> _userRepoMock;
        private AuthDbContext _context;
        private AuthServiceImpl _authService;
        private JwtHelper _jwtHelper;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AuthDbContext(options);
            _userRepoMock = new Mock<IUserRepository>();

            // JwtHelper requires IConfiguration - create a real one with test values
            var configData = new Dictionary<string, string>
            {
                { "Jwt:Key", "ThisIsATestSecretKeyForUnitTestingPurposes12345" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" }
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            _jwtHelper = new JwtHelper(configuration);

            _authService = new AuthServiceImpl(
                _context,
                _jwtHelper,
                _userRepoMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        // Test 1: Register should succeed when email and username are new
        [Test]
        public async Task Register_ShouldReturnToken_WhenEmailAndUsernameAreNew()
        {
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "password123",
                FullName = "Test User",
                Role = "DEVELOPER"
            };

            _userRepoMock.Setup(r => r.ExistsByEmail(user.Email)).ReturnsAsync(false);
            _userRepoMock.Setup(r => r.ExistsByUsername(user.Username)).ReturnsAsync(false);

            var result = await _authService.Register(user);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.GreaterThan(0));
        }

        // Test 2: Register should throw when email already exists
        [Test]
        public async Task Register_ShouldThrowException_WhenEmailAlreadyExists()
        {
            var user = new User
            {
                Username = "newuser",
                Email = "existing@example.com",
                PasswordHash = "password123",
                FullName = "New User",
                Role = "DEVELOPER"
            };

            _userRepoMock.Setup(r => r.ExistsByEmail(user.Email)).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<Exception>(() => _authService.Register(user));
            Assert.That(ex!.Message, Is.EqualTo("Email already registered!"));
        }

        // Test 3: Register should throw when username is already taken
        [Test]
        public async Task Register_ShouldThrowException_WhenUsernameAlreadyTaken()
        {
            var user = new User
            {
                Username = "existinguser",
                Email = "new@example.com",
                PasswordHash = "password123",
                FullName = "New User",
                Role = "DEVELOPER"
            };

            _userRepoMock.Setup(r => r.ExistsByEmail(user.Email)).ReturnsAsync(false);
            _userRepoMock.Setup(r => r.ExistsByUsername(user.Username)).ReturnsAsync(true);

            var ex = Assert.ThrowsAsync<Exception>(() => _authService.Register(user));
            Assert.That(ex!.Message, Is.EqualTo("Username already taken!"));
        }

        // Test 4: Login should return token when credentials are correct
        [Test]
        public async Task Login_ShouldReturnToken_WhenCredentialsAreCorrect()
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
            var user = new User
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = hashedPassword,
                IsActive = true,
                Role = "DEVELOPER"
            };

            _userRepoMock.Setup(r => r.FindByEmail("test@example.com")).ReturnsAsync(user);

            var result = await _authService.Login("test@example.com", "password123");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.GreaterThan(0));
        }

        // Test 5: Login should throw when email does not exist
        [Test]
        public async Task Login_ShouldThrowException_WhenEmailNotFound()
        {
            _userRepoMock.Setup(r => r.FindByEmail("wrong@example.com"))
                .ReturnsAsync((User?)null);

            var ex = Assert.ThrowsAsync<Exception>(
                () => _authService.Login("wrong@example.com", "password123"));
            Assert.That(ex!.Message, Is.EqualTo("Invalid email or password!"));
        }

        // Test 6: Login should throw when password is incorrect
        [Test]
        public async Task Login_ShouldThrowException_WhenPasswordIsWrong()
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctpassword");
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = hashedPassword,
                IsActive = true,
                Role = "DEVELOPER"
            };

            _userRepoMock.Setup(r => r.FindByEmail("test@example.com")).ReturnsAsync(user);

            var ex = Assert.ThrowsAsync<Exception>(
                () => _authService.Login("test@example.com", "wrongpassword"));
            Assert.That(ex!.Message, Is.EqualTo("Invalid email or password!"));
        }

        // Test 7: Login should throw when account is deactivated
        [Test]
        public async Task Login_ShouldThrowException_WhenAccountIsDeactivated()
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = hashedPassword,
                IsActive = false,
                Role = "DEVELOPER"
            };

            _userRepoMock.Setup(r => r.FindByEmail("test@example.com")).ReturnsAsync(user);

            var ex = Assert.ThrowsAsync<Exception>(
                () => _authService.Login("test@example.com", "password123"));
            Assert.That(ex!.Message, Is.EqualTo("Account is deactivated!"));
        }

        // Test 8: GetUserById should return user when found
        [Test]
        public async Task GetUserById_ShouldReturnUser_WhenUserExists()
        {
            var user = new User { UserId = 1, Email = "test@example.com", Username = "u", Role = "DEVELOPER" };
            _userRepoMock.Setup(r => r.FindByUserId(1)).ReturnsAsync(user);

            var result = await _authService.GetUserById(1);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.UserId, Is.EqualTo(1));
        }

        // Test 9: GetUserById should return null when user not found
        [Test]
        public async Task GetUserById_ShouldReturnNull_WhenUserNotFound()
        {
            _userRepoMock.Setup(r => r.FindByUserId(999)).ReturnsAsync((User?)null);

            var result = await _authService.GetUserById(999);

            Assert.That(result, Is.Null);
        }

        // Test 10: UpdateProfile should throw when user not found
        [Test]
        public async Task UpdateProfile_ShouldThrowException_WhenUserNotFound()
        {
            _userRepoMock.Setup(r => r.FindByUserId(999)).ReturnsAsync((User?)null);

            var dto = new UpdateProfileDto { FullName = "New Name" };

            var ex = Assert.ThrowsAsync<Exception>(
                () => _authService.UpdateProfile(999, dto));
            Assert.That(ex!.Message, Is.EqualTo("User not found!"));
        }

        // Test 11: Logout should complete successfully (JWT is stateless)
        [Test]
        public async Task Logout_ShouldCompleteSuccessfully()
        {
            Assert.DoesNotThrowAsync(() => _authService.Logout("any-token"));
        }

        // Test 12: GetUserByEmail should return correct user
        [Test]
        public async Task GetUserByEmail_ShouldReturnUser_WhenEmailExists()
        {
            var user = new User { UserId = 1, Email = "test@example.com", Username = "u", Role = "DEVELOPER" };
            _userRepoMock.Setup(r => r.FindByEmail("test@example.com")).ReturnsAsync(user);

            var result = await _authService.GetUserByEmail("test@example.com");

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Email, Is.EqualTo("test@example.com"));
        }
    }
}
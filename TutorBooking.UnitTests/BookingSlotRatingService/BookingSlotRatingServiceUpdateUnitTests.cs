using App.Core.Base;
using App.Core.Constants;
using App.DTOs.RatingDTOs;
using App.Repositories.Models.Rating;
using App.Repositories.UoW;
using App.Services.Interfaces.User;
using App.Services.Services;
using Moq;

namespace TutorBooking.UnitTests;

[TestFixture]
public class BookingSlotRatingServiceUpdateUnitTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IGenericRepository<BookingSlotRating>> _mockBookingSlotRatingRepo;
    private Mock<IUserService> _mockUserService;
    private BookingSlotRatingService _service;
    private readonly DateTime _currentUtcTime = DateTime.UtcNow;

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockBookingSlotRatingRepo = new Mock<IGenericRepository<BookingSlotRating>>();
        _mockUserService = new Mock<IUserService>();

        _mockUnitOfWork
            .Setup(uow => uow.GetRepository<BookingSlotRating>())
            .Returns(_mockBookingSlotRatingRepo.Object);

        _service = new BookingSlotRatingService(_mockUnitOfWork.Object, _mockUserService.Object);
    }

    [Test]
    public async Task UpdateAsync_ValidRequestWithinEditPeriod_UpdatesSuccessfully()
    {
        // Arrange
        var ratingId = Guid.NewGuid().ToString();
        var learnerId = Guid.NewGuid().ToString();
        var request = new BookingSlotRatingUpdateRequest
        {
            Id = ratingId,
            // Set other required properties
        };

        var existingRating = new BookingSlotRating
        {
            Id = ratingId,
            LearnerId = learnerId,
            CreatedTime = _currentUtcTime.AddDays(-5) // 5 days ago, within 7-day edit period
        };

        _mockBookingSlotRatingRepo
            .Setup(r => r.GetByIdAsync(ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRating);
        _mockUserService
            .Setup(u => u.GetCurrentUserId())
            .Returns(learnerId);

        // Act
        await _service.UpdateAsync(request);

        // Assert
        _mockBookingSlotRatingRepo.Verify(
            r => r.Update(It.IsAny<BookingSlotRating>()),
            Times.Once);
        _mockUnitOfWork.Verify(
            uow => uow.SaveAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task UpdateAsync_RatingNotFound_ThrowsErrorException()
    {
        // Arrange
        var request = new BookingSlotRatingUpdateRequest
        {
            Id = Guid.NewGuid().ToString()
        };

        _mockBookingSlotRatingRepo
            .Setup(r => r.GetByIdAsync(request.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookingSlotRating)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(
            async () => await _service.UpdateAsync(request));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.StatusCode, Is.EqualTo((int)StatusCode.NotFound));
    }

    [Test]
    public async Task UpdateAsync_EditPeriodExpired_ThrowsErrorException()
    {
        // Arrange
        var ratingId = Guid.NewGuid().ToString();
        var learnerId = Guid.NewGuid().ToString();
        var request = new BookingSlotRatingUpdateRequest
        {
            Id = ratingId
        };

        var existingRating = new BookingSlotRating
        {
            Id = ratingId,
            LearnerId = learnerId,
            CreatedTime = _currentUtcTime.AddDays(-8) // 8 days ago, outside 7-day edit period
        };

        _mockBookingSlotRatingRepo
            .Setup(r => r.GetByIdAsync(ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRating);
        _mockUserService
            .Setup(u => u.GetCurrentUserId())
            .Returns(learnerId);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(
            async () => await _service.UpdateAsync(request));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.StatusCode, Is.EqualTo((int)StatusCode.Forbidden));
    }

    [Test]
    public async Task UpdateAsync_ExactlySevenDaysOld_UpdatesSuccessfully()
    {
        // Arrange
        var ratingId = Guid.NewGuid().ToString();
        var learnerId = Guid.NewGuid().ToString();
        var request = new BookingSlotRatingUpdateRequest
        {
            Id = ratingId
        };

        var existingRating = new BookingSlotRating
        {
            Id = ratingId,
            LearnerId= learnerId,
            CreatedTime = _currentUtcTime.AddDays(-6.999) // Exactly 7 days ago
        };

        _mockBookingSlotRatingRepo
            .Setup(r => r.GetByIdAsync(ratingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRating);
        _mockUserService
            .Setup(u => u.GetCurrentUserId())
            .Returns(learnerId);

        // Act
        await _service.UpdateAsync(request);

        // Assert
        _mockBookingSlotRatingRepo.Verify(
            r => r.Update(It.IsAny<BookingSlotRating>()),
            Times.Once);
        _mockUnitOfWork.Verify(
            uow => uow.SaveAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TearDown]
    public void Cleanup()
    {
        _mockUnitOfWork = null;
        _mockBookingSlotRatingRepo = null;
        _service = null;
    }
}

using App.Core.Base;
using App.Core.Constants;
using App.DTOs.RatingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Rating;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using App.Services.Services;
using MockQueryable.Moq;
using Moq;

namespace TutorBooking.UnitTests;

public class BookingSlotRatingServiceCreateUnitTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IGenericRepository<Booking>> _mockBookingRepo;
    private Mock<IGenericRepository<BookingSlotRating>> _mockBookingSlotRatingRepo;
    private Mock<IUserService> _mockUserService;
    private Mock<INotificationService> _mockNotificationService;
    private BookingSlotRatingService _service;
    private readonly string _currentUserId = "bacsiemon";

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockBookingRepo = new Mock<IGenericRepository<Booking>>();
        _mockBookingSlotRatingRepo = new Mock<IGenericRepository<BookingSlotRating>>();
        _mockUserService = new Mock<IUserService>();

        _mockUserService
            .Setup(u => u.GetCurrentUserId())
            .Returns(_currentUserId);
        _mockUnitOfWork
            .Setup(uow => uow.GetRepository<Booking>())
            .Returns(_mockBookingRepo.Object);
        _mockUnitOfWork
            .Setup(uow => uow.GetRepository<BookingSlotRating>())
            .Returns(_mockBookingSlotRatingRepo.Object);

        _service = new BookingSlotRatingService(_mockUnitOfWork.Object, _mockUserService.Object, _mockNotificationService.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequestWithCompletedSlot_ReturnsCreatedEntity()
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString()
        };

        var booking = new Booking
        {
            Id = request.BookingSlotId,
            LearnerId = _currentUserId,
            BookedSlots = new List<BookedSlot>
            {
                new BookedSlot { Status = SlotStatus.Completed }
            },
            Tutor = new Tutor(),
            
        };

        await SetupMockBooking(booking);

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockBookingSlotRatingRepo.Verify(
            r => r.Insert(It.IsAny<BookingSlotRating>()),
            Times.Once);
        _mockUnitOfWork.Verify(
            uow => uow.SaveAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateAsync_BookingSlotNotFound_ThrowsErrorException()
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString()
        };

        await SetupMockBooking(null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(
            async () => await _service.CreateAsync(request));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.StatusCode, Is.EqualTo((int)StatusCode.NotFound));
    }

    [Test]
    public async Task CreateAsync_BookingSlotNotBelongToCurrentUser_ThrowsErrorException()
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString()
        };

        var booking = new Booking
        {
            Id = request.BookingSlotId,
            LearnerId = "different-user-id",
            BookedSlots = new List<BookedSlot>
            {
                new BookedSlot { Status = SlotStatus.Completed }
            },
            Tutor = new Tutor()
        };

        await SetupMockBooking(booking);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(
            async () => await _service.CreateAsync(request));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.StatusCode, Is.EqualTo((int)StatusCode.Forbidden));
    }

    [Test]
    public async Task CreateAsync_NoCompletedSlots_ThrowsErrorException()
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString()
        };

        var booking = new Booking
        {
            Id = request.BookingSlotId,
            LearnerId = _currentUserId,
            BookedSlots = new List<BookedSlot>
            {
                new BookedSlot { Status = SlotStatus.Pending },
                new BookedSlot { Status = SlotStatus.Cancelled }
            },
            Tutor = new Tutor()
        };

        await SetupMockBooking(booking);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(
            async () => await _service.CreateAsync(request));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.StatusCode, Is.EqualTo((int)StatusCode.BadRequest));
    }

    private Task SetupMockBooking(Booking? booking)
    {
        var bookings = (booking != null ?
            new List<Booking> { booking } :
            new List<Booking>());

        var mockDbSet = bookings.AsQueryable().BuildMockDbSet();

        _mockBookingRepo
            .Setup(r => r.ExistEntities())
            .Returns(mockDbSet.Object);

        return Task.CompletedTask;
    }

    [TearDown]
    public void Cleanup()
    {
        _mockUnitOfWork = null;
        _mockBookingRepo = null;
        _mockBookingSlotRatingRepo = null;
        _mockUserService = null;
        _service = null;
    }
}

using App.Core.Base;
using App.Core.Constants;
using App.DTOs.RatingDTOs;
using App.Repositories.Models.Rating;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces.User;
using App.Services.Services;
using MockQueryable.Moq;
using Moq;

namespace TutorBooking.UnitTests;

public class BookingSlotRatingServiceCreateUnitTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<IGenericRepository<BookingSlot>> _mockBookingSlotRepo;
    private Mock<IGenericRepository<BookingSlotRating>> _mockBookingSlotRatingRepo;
    private Mock<IUserService> _mockUserService;
    private BookingSlotRatingService _service;
    private readonly string _currentUserId = "bacsiemon";

    [SetUp]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockBookingSlotRepo = new Mock<IGenericRepository<BookingSlot>>();
        _mockBookingSlotRatingRepo = new Mock<IGenericRepository<BookingSlotRating>>();
        _mockUserService = new Mock<IUserService>();

        _mockUserService
            .Setup(u => u.GetCurrentUserId())
            .Returns(_currentUserId);

        _mockUnitOfWork
            .Setup(uow => uow.GetRepository<BookingSlot>())
            .Returns(_mockBookingSlotRepo.Object);
        _mockUnitOfWork
            .Setup(uow => uow.GetRepository<BookingSlotRating>())
            .Returns(_mockBookingSlotRatingRepo.Object);

        _service = new BookingSlotRatingService(_mockUnitOfWork.Object, _mockUserService.Object);
    }

    [Test]
    public async Task CreateAsync_ValidRequestWithCompletedSlot_ReturnsCreatedEntity()
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString()
        };

        var bookingSlot = new BookingSlot
        {
            Id = request.BookingSlotId,
            LearnerId = _currentUserId,
            BookedSlots = new List<BookedSlot>
            {
                new BookedSlot { Status = SlotStatus.Completed }
            },
            Tutor = new Tutor(),
            
        };

        await SetupMockBookingSlot(bookingSlot);

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

        await SetupMockBookingSlot(null);

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

        var bookingSlot = new BookingSlot
        {
            Id = request.BookingSlotId,
            LearnerId = "different-user-id",
            BookedSlots = new List<BookedSlot>
            {
                new BookedSlot { Status = SlotStatus.Completed }
            },
            Tutor = new Tutor()
        };

        await SetupMockBookingSlot(bookingSlot);

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

        var bookingSlot = new BookingSlot
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

        await SetupMockBookingSlot(bookingSlot);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(
            async () => await _service.CreateAsync(request));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.StatusCode, Is.EqualTo((int)StatusCode.BadRequest));
    }

    private Task SetupMockBookingSlot(BookingSlot? bookingSlot)
    {
        var bookingSlots = (bookingSlot != null ?
            new List<BookingSlot> { bookingSlot } :
            new List<BookingSlot>());

        var mockDbSet = bookingSlots.AsQueryable().BuildMockDbSet();

        _mockBookingSlotRepo
            .Setup(r => r.ExistEntities())
            .Returns(mockDbSet.Object);

        return Task.CompletedTask;
    }

    [TearDown]
    public void Cleanup()
    {
        _mockUnitOfWork = null;
        _mockBookingSlotRepo = null;
        _mockBookingSlotRatingRepo = null;
        _mockUserService = null;
        _service = null;
    }
}

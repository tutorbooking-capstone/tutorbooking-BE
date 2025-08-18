using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.BookingDTOs;
using App.DTOs.NotificationDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Services;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using System.Text.Json;

namespace TutorBooking.UnitTests.Services;

[TestFixture]
public class DisputeServiceUnitTests
{
    private IFixture _fixture;
    private Mock<IUnitOfWork> _mockUnitOfWork;
    private Mock<ICurrentUserProvider> _mockCurrentUserProvider;
    private Mock<IWalletService> _mockWalletService;
    private Mock<INotificationService> _mockNotificationService;
    private Mock<ITutorBookingService> _mockTutorBookingService;
    private Mock<IGenericRepository<BookingDispute>> _mockDisputeRepo;
    private Mock<IGenericRepository<BookedSlot>> _mockBookedSlotRepo;
    private Mock<IGenericRepository<Booking>> _mockBookingRepo;
    private Mock<IGenericRepository<Learner>> _mockLearnerRepo;
    private Mock<IGenericRepository<Tutor>> _mockTutorRepo;
    private Mock<IGenericRepository<Staff>> _mockStaffRepo;
    private Mock<IGenericRepository<HeldFund>> _mockHeldFundRepo;
    private Mock<IGenericRepository<LessonSnapshot>> _mockLessonSnapshotRepo;
    private DisputeService _service;

    private const string TestUserId = "test-user-id";
    private const string TestLearnerId = "test-learner-id";
    private const string TestTutorId = "test-tutor-id";
    private const string TestStaffId = "test-staff-id";
    private const string TestBookedSlotId = "test-booked-slot-id";
    private const string TestDisputeId = "test-dispute-id";
    private const string TestBookingId = "test-booking-id";

    [SetUp]
    public void Setup()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserProvider = new Mock<ICurrentUserProvider>();
        _mockWalletService = new Mock<IWalletService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockTutorBookingService = new Mock<ITutorBookingService>();
        
        _mockDisputeRepo = new Mock<IGenericRepository<BookingDispute>>();
        _mockBookedSlotRepo = new Mock<IGenericRepository<BookedSlot>>();
        _mockBookingRepo = new Mock<IGenericRepository<Booking>>();
        _mockLearnerRepo = new Mock<IGenericRepository<Learner>>();
        _mockTutorRepo = new Mock<IGenericRepository<Tutor>>();
        _mockStaffRepo = new Mock<IGenericRepository<Staff>>();
        _mockHeldFundRepo = new Mock<IGenericRepository<HeldFund>>();
        _mockLessonSnapshotRepo = new Mock<IGenericRepository<LessonSnapshot>>();

        SetupRepositories();
        SetupCurrentUserProvider();

        _service = new DisputeService(
            _mockUnitOfWork.Object,
            _mockCurrentUserProvider.Object,
            _mockWalletService.Object,
            _mockNotificationService.Object,
            _mockTutorBookingService.Object);
    }

    private void SetupRepositories()
    {
        _mockUnitOfWork.Setup(uow => uow.GetRepository<BookingDispute>()).Returns(_mockDisputeRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.GetRepository<BookedSlot>()).Returns(_mockBookedSlotRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.GetRepository<Booking>()).Returns(_mockBookingRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.GetRepository<Learner>()).Returns(_mockLearnerRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.GetRepository<Tutor>()).Returns(_mockTutorRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.GetRepository<Staff>()).Returns(_mockStaffRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.GetRepository<HeldFund>()).Returns(_mockHeldFundRepo.Object);
        _mockUnitOfWork.Setup(uow => uow.GetRepository<LessonSnapshot>()).Returns(_mockLessonSnapshotRepo.Object);
    }

    private void SetupCurrentUserProvider()
    {
        _mockCurrentUserProvider.Setup(p => p.GetCurrentUserId()).Returns(TestUserId);
        _mockCurrentUserProvider.Setup(p => p.IsInRole(It.IsAny<string>())).Returns(false);
    }

    #region Helper Methods Tests

    [Test]
    public void GetAuthenticatedUserId_WhenUserNotAuthenticated_ThrowsErrorException()
    {
        // Arrange
        _mockCurrentUserProvider.Setup(p => p.GetCurrentUserId()).Returns((string?)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => 
            await _service.CreateDisputeAsync(new CreateDisputeRequest { BookedSlotId = TestBookedSlotId, Reason = "Test reason" }));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.Unauthorized));
    }

    [Test]
    public async Task GetAuthenticatedLearnerIdAsync_WhenLearnerNotFound_ThrowsErrorException()
    {
        // Arrange
        _mockLearnerRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Learner, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Learner?)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => 
            await _service.CreateDisputeAsync(new CreateDisputeRequest { BookedSlotId = TestBookedSlotId, Reason = "Test reason" }));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.NotFound));
    }

    [Test]
    public async Task GetAuthenticatedTutorIdAsync_WhenTutorNotFound_ThrowsErrorException()
    {
        // Arrange
        _mockTutorRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Tutor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tutor?)null);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => 
            await _service.RespondToDisputeAsync(new RespondToDisputeRequest { DisputeId = TestDisputeId, Response = "Test response" }));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.NotFound));
    }

    #endregion

    #region CreateDisputeAsync Tests

    [Test]
    public async Task CreateDisputeAsync_ValidRequest_CreatesDisputeSuccessfully()
    {
        // Arrange
        var request = new CreateDisputeRequest
        {
            BookedSlotId = TestBookedSlotId,
            Reason = "Test dispute reason",
            EvidenceUrls = new List<string> { "http://example.com/evidence1.jpg" }
        };

        SetupValidLearner();
        SetupValidBookedSlot();
        SetupValidDispute();

        // Act
        var result = await _service.CreateDisputeAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.LearnerReason, Is.EqualTo(request.Reason));
        
        _mockDisputeRepo.Verify(r => r.Insert(It.IsAny<BookingDispute>()), Times.Once);
        _mockBookedSlotRepo.Verify(r => r.UpdateFields(It.IsAny<BookedSlot>(), It.IsAny<Expression<Func<BookedSlot, object>>[]>()), Times.Once);
        _mockBookingRepo.Verify(r => r.UpdateFields(It.IsAny<Booking>(), It.IsAny<Expression<Func<Booking, object>>[]>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(uow => uow.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockNotificationService.Verify(ns => ns.SendToUsersAsync(It.IsAny<SendNotificationToUsersRequest>()), Times.Exactly(2));
    }

    [Test]
    public async Task CreateDisputeAsync_BookedSlotNotFound_ThrowsErrorException()
    {
        // Arrange
        var request = new CreateDisputeRequest
        {
            BookedSlotId = TestBookedSlotId,
            Reason = "Test dispute reason"
        };

        SetupValidLearner();
        SetupBookedSlotNotFound();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => await _service.CreateDisputeAsync(request));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.NotFound));
    }

    [Test]
    public async Task CreateDisputeAsync_BookedSlotAlreadyCancelled_ThrowsErrorException()
    {
        // Arrange
        var request = new CreateDisputeRequest
        {
            BookedSlotId = TestBookedSlotId,
            Reason = "Test dispute reason"
        };

        SetupValidLearner();
        SetupCancelledBookedSlot();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => await _service.CreateDisputeAsync(request));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.BadRequest));
    }

    [Test]
    public async Task CreateDisputeAsync_BookedSlotAlreadyCompleted_ThrowsErrorException()
    {
        // Arrange
        var request = new CreateDisputeRequest
        {
            BookedSlotId = TestBookedSlotId,
            Reason = "Test dispute reason"
        };

        SetupValidLearner();
        SetupCompletedBookedSlot();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => await _service.CreateDisputeAsync(request));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.BadRequest));
    }

    [Test]
    public async Task CreateDisputeAsync_BookedSlotAlreadyDisputed_ThrowsErrorException()
    {
        // Arrange
        var request = new CreateDisputeRequest
        {
            BookedSlotId = TestBookedSlotId,
            Reason = "Test dispute reason"
        };

        SetupValidLearner();
        SetupDisputedBookedSlot();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => await _service.CreateDisputeAsync(request));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.BadRequest));
    }

    #endregion

    #region WithdrawDisputeAsync Tests

    [Test]
    public async Task WithdrawDisputeAsync_ValidRequest_WithdrawsDisputeSuccessfully()
    {
        // Arrange
        var request = new WithdrawDisputeRequest { DisputeId = TestDisputeId };
        
        SetupValidLearner();
        SetupValidDispute(DisputeStatus.PendingReconciliation, TestLearnerId);

        // Act
        var result = await _service.WithdrawDisputeAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        
        _mockDisputeRepo.Verify(r => r.UpdateFields(It.IsAny<BookingDispute>(), It.IsAny<Expression<Func<BookingDispute, object>>[]>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task WithdrawDisputeAsync_DisputeNotFound_ThrowsErrorException()
    {
        // Arrange
        var request = new WithdrawDisputeRequest { DisputeId = TestDisputeId };
        
        SetupValidLearner();
        SetupDisputeNotFound();

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => await _service.WithdrawDisputeAsync(request));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.NotFound));
    }

    [Test]
    public async Task WithdrawDisputeAsync_LearnerNotOwner_ThrowsErrorException()
    {
        // Arrange
        var request = new WithdrawDisputeRequest { DisputeId = TestDisputeId };
        
        SetupValidLearner();
        SetupValidDispute(DisputeStatus.PendingReconciliation, "different-learner-id");

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => await _service.WithdrawDisputeAsync(request));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.Forbidden));
    }

    [Test]
    public async Task WithdrawDisputeAsync_WrongStatus_ThrowsErrorException()
    {
        // Arrange
        var request = new WithdrawDisputeRequest { DisputeId = TestDisputeId };
        
        SetupValidLearner();
        SetupValidDispute(DisputeStatus.AwaitingStaffReview, TestLearnerId);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => await _service.WithdrawDisputeAsync(request));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.BadRequest));
    }

    #endregion

    #region GetLearnerDisputesAsync Tests

    [Test]
    public async Task GetLearnerDisputesAsync_ValidRequest_ReturnsDisputes()
    {
        // Arrange
        SetupValidLearner();
        SetupDisputeQueryable();

        // Act
        var result = await _service.GetLearnerDisputesAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<List<BookingDisputeResponse>>());
        
        _mockDisputeRepo.Verify(r => r.GetQueryable(), Times.Once);
    }

    [Test]
    public async Task GetLearnerDisputesAsync_OnlyActiveTrue_FiltersActiveDisputes()
    {
        // Arrange
        SetupValidLearner();
        SetupDisputeQueryable();

        // Act
        var result = await _service.GetLearnerDisputesAsync(onlyActive: true);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<List<BookingDisputeResponse>>());
    }

    #endregion

    #region GetDisputeDetailForLearnerAsync Tests

    [Test]
    public async Task GetDisputeDetailForLearnerAsync_ValidRequest_ReturnsDetail()
    {
        // Arrange
        SetupValidLearner();
        SetupValidDispute(DisputeStatus.PendingReconciliation, TestLearnerId);
        SetupValidBookedSlotForDetail();

        // Act
        var result = await _service.GetDisputeDetailForLearnerAsync(TestDisputeId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Dispute, Is.Not.Null);
        Assert.That(result.AffectedSlots, Is.Not.Null);
    }

    [Test]
    public async Task GetDisputeDetailForLearnerAsync_LearnerNotOwner_ThrowsErrorException()
    {
        // Arrange
        SetupValidLearner();
        SetupValidDispute(DisputeStatus.PendingReconciliation, "different-learner-id");

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => 
            await _service.GetDisputeDetailForLearnerAsync(TestDisputeId));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.Forbidden));
    }

    #endregion

    #region RespondToDisputeAsync Tests

    [Test]
    public async Task RespondToDisputeAsync_ValidRequest_RespondsSuccessfully()
    {
        // Arrange
        var request = new RespondToDisputeRequest 
        { 
            DisputeId = TestDisputeId, 
            Response = "Test tutor response" 
        };

        SetupValidTutor();
        SetupValidDispute(DisputeStatus.PendingReconciliation, TestLearnerId, TestTutorId);
        SetupValidBookedSlotForTutor();
        SetupValidStaff();

        // Act
        try
        {
            var result = await _service.RespondToDisputeAsync(request);
            // Assert
            Assert.That(result, Is.Not.Null);

            _mockDisputeRepo.Verify(r => r.UpdateFields(It.IsAny<BookingDispute>(), It.IsAny<Expression<Func<BookingDispute, object>>[]>()), Times.Exactly(2));
            _mockBookingRepo.Verify(r => r.UpdateFields(It.IsAny<Booking>(), It.IsAny<Expression<Func<Booking, object>>[]>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockNotificationService.Verify(ns => ns.SendToUsersAsync(It.IsAny<SendNotificationToUsersRequest>()), Times.Exactly(3));
        }
        catch (ErrorException ex)
        {
            Assert.Fail("ErrorDetail:" + ex.ErrorDetail.ErrorMessage);
        }
    }

    [Test]
    public async Task RespondToDisputeAsync_TutorNotOwner_ThrowsErrorException()
    {
        // Arrange
        var request = new RespondToDisputeRequest 
        { 
            DisputeId = TestDisputeId, 
            Response = "Test tutor response" 
        };

        SetupValidTutor();
        SetupValidDispute(DisputeStatus.PendingReconciliation, TestLearnerId, "different-tutor-id");

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => 
            await _service.RespondToDisputeAsync(request));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.Forbidden));
    }

    [Test]
    public async Task RespondToDisputeAsync_WrongStatus_ThrowsErrorException()
    {
        // Arrange
        var request = new RespondToDisputeRequest 
        { 
            DisputeId = TestDisputeId, 
            Response = "Test tutor response" 
        };

        SetupValidTutor();
        SetupValidDispute(DisputeStatus.AwaitingStaffReview, TestLearnerId, TestTutorId);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => 
            await _service.RespondToDisputeAsync(request));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.BadRequest));
    }

    #endregion

    #region GetTutorDisputesAsync Tests

    [Test]
    public async Task GetTutorDisputesAsync_ValidRequest_ReturnsDisputes()
    {
        // Arrange
        SetupValidTutor();
        SetupDisputeQueryable();

        // Act
        var result = await _service.GetTutorDisputesAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<List<BookingDisputeResponse>>());
        
        _mockDisputeRepo.Verify(r => r.GetQueryable(), Times.Once);
    }

    #endregion

    #region GetDisputeDetailForTutorAsync Tests

    [Test]
    public async Task GetDisputeDetailForTutorAsync_ValidRequest_ReturnsDetail()
    {
        // Arrange
        SetupValidTutor();
        SetupValidDispute(DisputeStatus.PendingReconciliation, TestLearnerId, TestTutorId);
        SetupValidBookedSlotForDetail();

        // Act
        var result = await _service.GetDisputeDetailForTutorAsync(TestDisputeId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Dispute, Is.Not.Null);
        Assert.That(result.AffectedSlots, Is.Not.Null);
    }

    [Test]
    public async Task GetDisputeDetailForTutorAsync_TutorNotOwner_ThrowsErrorException()
    {
        // Arrange
        SetupValidTutor();
        SetupValidDispute(DisputeStatus.PendingReconciliation, TestLearnerId, "different-tutor-id");

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => 
            await _service.GetDisputeDetailForTutorAsync(TestDisputeId));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.Forbidden));
    }

    #endregion

    #region Staff Operations Tests

    [Test]
    public async Task ResolveDisputeAsync_ValidRequestAsAdmin_ResolvesSuccessfully()
    {
        // Arrange
        var request = new ResolveDisputeRequest
        {
            DisputeId = TestDisputeId,
            Resolution = DisputeResolution.StaffLearnerWin,
            Notes = "Staff notes"
        };

        SetupAdminUser();
        SetupValidDispute(DisputeStatus.AwaitingStaffReview, TestLearnerId, TestTutorId);

        // Act
        var result = await _service.ResolveDisputeAsync(request);

        // Assert
        Assert.That(result, Is.Not.Null);
        
        _mockDisputeRepo.Verify(r => r.UpdateFields(It.IsAny<BookingDispute>(), It.IsAny<Expression<Func<BookingDispute, object>>[]>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ResolveDisputeAsync_NoManagerialAccess_ThrowsErrorException()
    {
        // Arrange
        var request = new ResolveDisputeRequest
        {
            DisputeId = TestDisputeId,
            Resolution = DisputeResolution.StaffLearnerWin,
            Notes = "Staff notes"
        };

        SetupValidDispute(DisputeStatus.AwaitingStaffReview, TestLearnerId, TestTutorId);

        // Act & Assert
        var exception = Assert.ThrowsAsync<ErrorException>(async () => 
            await _service.ResolveDisputeAsync(request));
        
        Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        Assert.That(exception.ErrorDetail.ErrorCode, Is.EqualTo(ErrorCode.Forbidden));
    }

    [Test]
    public async Task GetDisputesForReviewAsync_ValidStaffUser_ReturnsDisputes()
    {
        // Arrange
        SetupStaffUser();
        SetupDisputeQueryable();

        // Act
        var result = await _service.GetDisputesForReviewAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<List<BookingDisputeResponse>>());
    }

    [Test]
    public async Task GetFilteredDisputesAsync_ValidStaffUser_ReturnsPaginatedList()
    {
        // Arrange
        var filter = new StaffDisputeFilterRequest
        {
            PageIndex = 0,
            PageSize = 10,
            ResolutionFilter = new List<DisputeResolution> { DisputeResolution.StaffLearnerWin }
        };

        SetupStaffUser();
        SetupDisputeQueryableWithIncludes();

        // Act
        var result = await _service.GetFilteredDisputesAsync(filter);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<BasePaginatedList<BookingDisputeResponse>>());
    }

    [Test]
    public async Task GetDisputeDetailForStaffAsync_ValidStaffUser_ReturnsDetail()
    {
        // Arrange
        SetupStaffUser();
        SetupValidDispute(DisputeStatus.AwaitingStaffReview, TestLearnerId, TestTutorId);
        SetupValidBookedSlotForDetail();

        // Act
        var result = await _service.GetDisputeDetailForStaffAsync(TestDisputeId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Dispute, Is.Not.Null);
        Assert.That(result.AffectedSlots, Is.Not.Null);
    }

    #endregion

    #region System Operations Tests

    [Test]
    public async Task ProcessExpiredReconciliationsAsync_WithExpiredDisputes_ProcessesSuccessfully()
    {
        // Arrange
        SetupExpiredReconciliationDisputes();

        // Act
        await _service.ProcessExpiredReconciliationsAsync();

        // Assert
        _mockDisputeRepo.Verify(r => r.UpdateFields(It.IsAny<BookingDispute>(), It.IsAny<Expression<Func<BookingDispute, object>>[]>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProcessExpiredStaffReviewsAsync_WithExpiredReviews_ProcessesSuccessfully()
    {
        // Arrange
        SetupExpiredStaffReviewDisputes();

        // Act
        await _service.ProcessExpiredStaffReviewsAsync();

        // Assert
        _mockDisputeRepo.Verify(r => r.UpdateFields(It.IsAny<BookingDispute>(), It.IsAny<Expression<Func<BookingDispute, object>>[]>()), Times.Once);
        _mockUnitOfWork.Verify(uow => uow.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetDisputeMetadataAsync Tests

    [Test]
    public async Task GetDisputeMetadataAsync_ReturnsMetadata()
    {
        // Act
        var result = await _service.GetDisputeMetadataAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<Dictionary<string, object>>());
    }

    #endregion

    #region Setup Helper Methods

    private void SetupValidLearner()
    {
        var learner = new Learner { UserId = TestUserId };
        
        // Mock FindAsync with explicit cancellation token handling
        _mockLearnerRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Learner, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(learner);
    }

    private void SetupValidTutor()
    {
        var tutor = new Tutor { UserId = TestUserId };
        
        // Mock FindAsync with explicit cancellation token handling
        _mockTutorRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Tutor, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tutor);
    }

    private void SetupValidStaff()
    {
        var staff = new Staff { UserId = TestStaffId };
        var staffList = new List<Staff> { staff };
        var mockStaffQueryable = staffList.AsQueryable().BuildMockDbSet();
        
        _mockStaffRepo.Setup(r => r.GetQueryable()).Returns(mockStaffQueryable.Object);
    }

    private void SetupAdminUser()
    {
        _mockCurrentUserProvider.Setup(p => p.IsInRole("Admin")).Returns(true);
    }

    private void SetupStaffUser()
    {
        _mockCurrentUserProvider.Setup(p => p.IsInRole("Staff")).Returns(true);
    }

    private void SetupValidBookedSlot()
    {
        var booking = new Booking
        {
            Id = TestBookingId,
            LearnerId = TestUserId,
            TutorId = TestTutorId,
            Tutor = new Tutor { UserId = TestTutorId },
            Learner = new Learner { UserId = TestUserId }
        };

        var bookedSlot = new BookedSlot
        {
            Id = TestBookedSlotId,
            BookingId = TestBookingId,
            Status = SlotStatus.AwaitingPayout,
            DisputeId = null,
            Booking = booking
        };

        var bookedSlots = new List<BookedSlot> { bookedSlot };
        var mockBookedSlotQueryable = bookedSlots.AsQueryable().BuildMockDbSet();

        _mockBookedSlotRepo.Setup(r => r.GetQueryable()).Returns(mockBookedSlotQueryable.Object);
    }

    private void SetupValidBookedSlotForTutor()
    {
        var booking = new Booking
        {
            Id = TestBookingId,
            LearnerId = TestLearnerId,
            TutorId = TestTutorId
        };

        var bookedSlot = new BookedSlot
        {
            Id = TestBookedSlotId,
            BookingId = TestBookingId,
            Status = SlotStatus.AwaitingPayout,
            Booking = booking
        };

        _mockBookedSlotRepo.Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(bookedSlot);
    }

    private void SetupValidBookedSlotForDetail()
    {
        var bookedSlot = new BookedSlot
        {
            Id = TestBookedSlotId,
            BookingId = TestBookingId,
            Status = SlotStatus.AwaitingPayout,
            BookedDate = DateTime.UtcNow,
            SlotIndex = 1
        };

        var bookedSlots = new List<BookedSlot> { bookedSlot };
        var mockBookedSlotQueryable = bookedSlots.AsQueryable().BuildMockDbSet();

        _mockBookedSlotRepo.Setup(r => r.GetQueryable()).Returns(mockBookedSlotQueryable.Object);
    }

    private void SetupBookedSlotNotFound()
    {
        var emptyBookedSlots = new List<BookedSlot>();
        var mockBookedSlotQueryable = emptyBookedSlots.AsQueryable().BuildMockDbSet();

        _mockBookedSlotRepo.Setup(r => r.GetQueryable()).Returns(mockBookedSlotQueryable.Object);
    }

    private void SetupCancelledBookedSlot()
    {
        var booking = new Booking
        {
            Id = TestBookingId,
            LearnerId = TestUserId,
            TutorId = TestTutorId
        };

        var bookedSlot = new BookedSlot
        {
            Id = TestBookedSlotId,
            BookingId = TestBookingId,
            Status = SlotStatus.Cancelled,
            Booking = booking
        };

        var bookedSlots = new List<BookedSlot> { bookedSlot };
        var mockBookedSlotQueryable = bookedSlots.AsQueryable().BuildMockDbSet();

        _mockBookedSlotRepo.Setup(r => r.GetQueryable()).Returns(mockBookedSlotQueryable.Object);
    }

    private void SetupCompletedBookedSlot()
    {
        var booking = new Booking
        {
            Id = TestBookingId,
            LearnerId = TestUserId,
            TutorId = TestTutorId
        };

        var bookedSlot = new BookedSlot
        {
            Id = TestBookedSlotId,
            BookingId = TestBookingId,
            Status = SlotStatus.Completed,
            Booking = booking
        };

        var bookedSlots = new List<BookedSlot> { bookedSlot };
        var mockBookedSlotQueryable = bookedSlots.AsQueryable().BuildMockDbSet();

        _mockBookedSlotRepo.Setup(r => r.GetQueryable()).Returns(mockBookedSlotQueryable.Object);
    }

    private void SetupDisputedBookedSlot()
    {
        var booking = new Booking
        {
            Id = TestBookingId,
            LearnerId = TestUserId,
            TutorId = TestTutorId
        };

        var bookedSlot = new BookedSlot
        {
            Id = TestBookedSlotId,
            BookingId = TestBookingId,
            Status = SlotStatus.AwaitingPayout,
            DisputeId = "existing-dispute-id",
            Booking = booking
        };

        var bookedSlots = new List<BookedSlot> { bookedSlot };
        var mockBookedSlotQueryable = bookedSlots.AsQueryable().BuildMockDbSet();

        _mockBookedSlotRepo.Setup(r => r.GetQueryable()).Returns(mockBookedSlotQueryable.Object);
    }

    private void SetupValidDispute(DisputeStatus status = DisputeStatus.PendingReconciliation, 
        string? learnerId = null, string? tutorId = null)
    {
        var dispute = new BookingDispute
        {
            Id = TestDisputeId,
            BookedSlotId = TestBookedSlotId,
            LearnerId = learnerId ?? TestLearnerId,
            TutorId = tutorId ?? TestTutorId,
            Status = status,
            CaseNumber = "DSPB-20240101-001",
            LearnerReason = "Test reason",
            CreatedAt = DateTime.UtcNow,
            ReconciliationEndTime = DateTime.UtcNow.AddHours(24),
            Learner = new Learner { UserId = learnerId ?? TestLearnerId, User = new AppUser { FullName = "Test Learner" } },
            Tutor = new Tutor { UserId = tutorId ?? TestTutorId, User = new AppUser { FullName = "Test Tutor" } }
        };

        var disputes = new List<BookingDispute> { dispute };
        var mockDisputeQueryable = disputes.AsQueryable().BuildMockDbSet();

        _mockDisputeRepo.Setup(r => r.GetQueryable()).Returns(mockDisputeQueryable.Object);
    }

    private void SetupDisputeNotFound()
    {
        var emptyDisputes = new List<BookingDispute>();
        var mockDisputeQueryable = emptyDisputes.AsQueryable().BuildMockDbSet();

        _mockDisputeRepo.Setup(r => r.GetQueryable()).Returns(mockDisputeQueryable.Object);
    }

    private void SetupDisputeQueryable()
    {
        var disputes = new List<BookingDispute>
        {
            new BookingDispute
            {
                Id = TestDisputeId,
                LearnerId = TestUserId,
                TutorId = TestTutorId,
                Status = DisputeStatus.PendingReconciliation,
                Learner = new Learner { User = new AppUser { FullName = "Test Learner" } },
                Tutor = new Tutor { User = new AppUser { FullName = "Test Tutor" } }
            }
        };

        var mockDisputeQueryable = disputes.AsQueryable().BuildMockDbSet();
        _mockDisputeRepo.Setup(r => r.GetQueryable()).Returns(mockDisputeQueryable.Object);
    }

    private void SetupDisputeQueryableWithIncludes()
    {
        var disputes = new List<BookingDispute>
        {
            new BookingDispute
            {
                Id = TestDisputeId,
                LearnerId = TestUserId,
                TutorId = TestTutorId,
                Status = DisputeStatus.AwaitingStaffReview,
                Resolution = DisputeResolution.StaffLearnerWin,
                CaseNumber = "DSPB-20240101-001",
                LearnerReason = "Test reason",
                CreatedAt = DateTime.UtcNow,
                Learner = new Learner { User = new AppUser { FullName = "Test Learner" } },
                Tutor = new Tutor { User = new AppUser { FullName = "Test Tutor" } }
            }
        };

        var mockDisputeQueryable = disputes.AsQueryable().BuildMockDbSet();
        _mockDisputeRepo.Setup(r => r.GetQueryable()).Returns(mockDisputeQueryable.Object);
    }

    private void SetupExpiredReconciliationDisputes()
    {
        var expiredDisputes = new List<BookingDispute>
        {
            new BookingDispute
            {
                Id = TestDisputeId,
                Status = DisputeStatus.PendingReconciliation,
                ReconciliationEndTime = DateTime.UtcNow.AddHours(-1),
                TutorResponse = null,
                BookedSlot = new BookedSlot
                {
                    Id = TestBookedSlotId,
                    Booking = new Booking { Id = TestBookingId }
                }
            }
        };

        var mockDisputeQueryable = expiredDisputes.AsQueryable().BuildMockDbSet();
        _mockDisputeRepo.Setup(r => r.GetQueryable()).Returns(mockDisputeQueryable.Object);
    }

    private void SetupExpiredStaffReviewDisputes()
    {
        var expiredDisputes = new List<BookingDispute>
        {
            new BookingDispute
            {
                Id = TestDisputeId,
                Status = DisputeStatus.AwaitingStaffReview,
                StaffReviewEndTime = DateTime.UtcNow.AddHours(-1),
                BookedSlot = new BookedSlot
                {
                    Id = TestBookedSlotId,
                    Booking = new Booking { Id = TestBookingId }
                }
            }
        };

        var mockDisputeQueryable = expiredDisputes.AsQueryable().BuildMockDbSet();
        _mockDisputeRepo.Setup(r => r.GetQueryable()).Returns(mockDisputeQueryable.Object);
    }

    #endregion

    [TearDown]
    public void Cleanup()
    {
        _fixture = null!;
        _mockUnitOfWork = null!;
        _mockCurrentUserProvider = null!;
        _mockWalletService = null!;
        _mockNotificationService = null!;
        _mockTutorBookingService = null!;
        _service = null!;
    }
}
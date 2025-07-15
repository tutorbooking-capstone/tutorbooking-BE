using App.DTOs.RatingDTOs;
using FluentValidation.TestHelper;

namespace TutorBooking.UnitTests;

[TestFixture]
public class BookingSlotRatingRequestValidatorTests
{
    private BookedSlotRatingRequestValidator _validator;

    [SetUp]
    public void Setup()
    {
        _validator = new BookedSlotRatingRequestValidator();
    }

    [Test]
    public void Validate_ValidRequest_PassesValidation()
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString(),
            TeachingQuality = 4.5f,
            Attitude = 5f,
            Commitment = 3.5f,
            Comment = "Great teaching experience!"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_EmptyBookingSlotId_FailsValidation()
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = "",
            TeachingQuality = 4.5f,
            Attitude = 5f,
            Commitment = 3.5f
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BookingSlotId)
            .WithErrorMessage("BOOKEDSLOTID_REQUIRED");
    }

    [Test]
    public void Validate_NullBookingSlotId_FailsValidation()
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = null,
            TeachingQuality = 4.5f,
            Attitude = 5f,
            Commitment = 3.5f
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BookingSlotId)
            .WithErrorMessage("BOOKEDSLOTID_REQUIRED");
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(5.1f)]
    [TestCase(6f)]
    public void Validate_TeachingQualityOutOfRange_FailsValidation(float rating)
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString(),
            TeachingQuality = rating,
            Attitude = 4f,
            Commitment = 4f
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TeachingQuality)
            .WithErrorMessage("TEACHINGQUALITY_BETWEEN_1_AND_5");
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(5.1f)]
    [TestCase(6f)]
    public void Validate_AttitudeOutOfRange_FailsValidation(float rating)
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString(),
            TeachingQuality = 4f,
            Attitude = rating,
            Commitment = 4f
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Attitude)
            .WithErrorMessage("ATTITUDE_BETWEEN_1_AND_5");
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(5.1f)]
    [TestCase(6f)]
    public void Validate_CommitmentOutOfRange_FailsValidation(float rating)
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString(),
            TeachingQuality = 4f,
            Attitude = 4f,
            Commitment = rating
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Commitment)
            .WithErrorMessage("COMMITMENT_BETWEEN_1_AND_5");
    }

    [TestCase(1f)]
    [TestCase(3.5f)]
    [TestCase(5f)]
    public void Validate_RatingsWithinRange_PassesValidation(float rating)
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString(),
            TeachingQuality = rating,
            Attitude = rating,
            Commitment = rating
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_CommentIsOptional_PassesValidation()
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = Guid.NewGuid().ToString(),
            TeachingQuality = 4f,
            Attitude = 4f,
            Commitment = 4f,
            Comment = null
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Comment);
    }

    [Test]
    public void Validate_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var request = new BookingSlotRatingRequest
        {
            BookingSlotId = "",
            TeachingQuality = 0f,
            Attitude = 6f,
            Commitment = -1f
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BookingSlotId)
            .WithErrorMessage("BOOKEDSLOTID_REQUIRED");
        result.ShouldHaveValidationErrorFor(x => x.TeachingQuality)
            .WithErrorMessage("TEACHINGQUALITY_BETWEEN_1_AND_5");
        result.ShouldHaveValidationErrorFor(x => x.Attitude)
            .WithErrorMessage("ATTITUDE_BETWEEN_1_AND_5");
        result.ShouldHaveValidationErrorFor(x => x.Commitment)
            .WithErrorMessage("COMMITMENT_BETWEEN_1_AND_5");
    }
}

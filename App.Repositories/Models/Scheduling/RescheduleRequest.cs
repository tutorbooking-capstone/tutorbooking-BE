using App.Core.Base;
using App.Core.Utils;
using App.Repositories.Models.Scheduling;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    public enum RescheduleRequestStatus
    {
        [EnumDescription("Đang chờ phản hồi")]
        Pending = 0,
        
        [EnumDescription("Đã chấp nhận")]
        Accepted = 1,
        
        [EnumDescription("Đã từ chối")]
        Rejected = 2,
        
        [EnumDescription("Đã hết hạn")]
        Expired = 3,
        
        [EnumDescription("Đã hủy")]
        Cancelled = 4
    }

    public enum RescheduleInitiator
    {
        [EnumDescription("Học viên")]
        Learner = 0,
        
        [EnumDescription("Gia sư")]
        Tutor = 1
    }

    public class RescheduleRequest : BaseEntity
    {
        public string BookedSlotId { get; set; } = string.Empty;
        public string RequestedByUserId { get; set; } = string.Empty;
        public RescheduleInitiator Initiator { get; set; }
        public string Reason { get; set; } = string.Empty;
        public RescheduleRequestStatus Status { get; set; } = RescheduleRequestStatus.Pending;
        public string? ResponseNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; } // 24 hours after creation
        public DateTime? RespondedAt { get; set; }
        public string? AcceptedSlotId { get; set; }
        
        // Navigation properties
        public virtual BookedSlot BookedSlot { get; set; } = null!;
        public virtual ICollection<OfferedSlot> OfferedSlots { get; set; } = new List<OfferedSlot>();
        public virtual OfferedSlot? AcceptedSlot { get; set; }
        
        #region Behaviors
        public static RescheduleRequest Create(
            string bookedSlotId, 
            string requestedByUserId, 
            RescheduleInitiator initiator, 
            string reason, 
            DateTime newSlotDateTime,
            int newSlotIndex)
        {
            // Create the request
            var now = CoreHelper.SystemTimeNow.DateTime;
            var request = new RescheduleRequest
            {
                BookedSlotId = bookedSlotId,
                RequestedByUserId = requestedByUserId,
                Initiator = initiator,
                Reason = reason,
                Status = RescheduleRequestStatus.Pending,
                CreatedAt = now,
                ExpiresAt = now.AddHours(24) // Request expires in 24 hours
            };
            
            // Create the offered slot
            var offeredSlot = new OfferedSlot
            {
                RescheduleRequestId = request.Id,
                SlotDateTime = newSlotDateTime,
                SlotIndex = newSlotIndex,
                IsForReschedule = true
            };
            
            request.OfferedSlots = new List<OfferedSlot> { offeredSlot };
            return request;
        }
        
        // Check if the request can be made (24-hour rule)
        public static bool CanRequestReschedule(DateTime slotDateTime)
        {
            var now = CoreHelper.SystemTimeNow.DateTime;
            return (slotDateTime - now).TotalHours >= 24;
        }
        
        // Check if request is expired
        public bool IsExpired()
        {
            return CoreHelper.SystemTimeNow.DateTime > ExpiresAt;
        }
        
        // Accept a reschedule request
        //public async Task<BookedSlot> Accept(string offeredSlotId, string updatedBy, IUnitOfWork unitOfWork)

        // Reject a reschedule request
        public Expression<Func<RescheduleRequest, object>>[] Reject(string note, string updatedBy)
        {
            if (Status != RescheduleRequestStatus.Pending)
                throw new InvalidOperationException("Cannot reject a request that is not pending.");
                
            Status = RescheduleRequestStatus.Rejected;
            ResponseNote = note;
            RespondedAt = CoreHelper.SystemTimeNow.DateTime;
            
            LastUpdatedBy = updatedBy;
            LastUpdatedTime = CoreHelper.SystemTimeNow.DateTime;
            
            return new[]
            {
                (Expression<Func<RescheduleRequest, object>>)(x => x.Status),
                x => x.ResponseNote!,
                x => x.RespondedAt!,
                x => x.LastUpdatedBy!,
                x => x.LastUpdatedTime
            };
        }
        
        // Mark request as expired (called by background job)
        public Expression<Func<RescheduleRequest, object>>[] MarkExpired()
        {
            if (Status != RescheduleRequestStatus.Pending)
                return Array.Empty<Expression<Func<RescheduleRequest, object>>>();
                
            Status = RescheduleRequestStatus.Expired;
            LastUpdatedTime = CoreHelper.SystemTimeNow.DateTime;
            
            return new[]
            {
                (Expression<Func<RescheduleRequest, object>>)(x => x.Status),
                x => x.LastUpdatedTime
            };
        }
        
        // Cancel a request (by requester)
        public Expression<Func<RescheduleRequest, object>>[] Cancel(string updatedBy)
        {
            if (Status != RescheduleRequestStatus.Pending)
                throw new InvalidOperationException("Cannot cancel a request that is not pending.");
                
            Status = RescheduleRequestStatus.Cancelled;
            RespondedAt = CoreHelper.SystemTimeNow.DateTime;
            
            LastUpdatedBy = updatedBy;
            LastUpdatedTime = CoreHelper.SystemTimeNow.DateTime;
            
            return new[]
            {
                (Expression<Func<RescheduleRequest, object>>)(x => x.Status),
                x => x.RespondedAt!,
                x => x.LastUpdatedBy!,
                x => x.LastUpdatedTime
            };
        }
        #endregion
    }
}
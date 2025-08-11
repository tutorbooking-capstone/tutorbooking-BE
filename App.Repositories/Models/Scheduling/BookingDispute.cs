using App.Core.Base;
using App.Core.Utils;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    #region Enums
    public enum DisputeStatus
    {
        PendingReconciliation = 0,    // Giai đoạn hòa giải 24h
        ClosedWithdrawn = 1,          // Học viên rút khiếu nại
        ClosedResolved = 2,           // Tự động giải quyết (gia sư không phản hồi)
        AwaitingStaffReview = 3,      // Chuyển lên nhân viên xử lý
        ResolvedLearnerWin = 4,       // Nhân viên quyết định học viên thắng
        ResolvedTutorWin = 5,         // Nhân viên quyết định gia sư thắng
        ResolvedDraw = 6              // Nhân viên quyết định hòa
    }

    public enum DisputeResolution
    {
        None = 0,                     // Chưa giải quyết
        LearnerWithdrew = 1,          // Học viên rút khiếu nại
        TutorNoResponse = 2,          // Gia sư không phản hồi trong 24h
        StaffLearnerWin = 3,          // Nhân viên quyết định học viên thắng
        StaffTutorWin = 4,            // Nhân viên quyết định gia sư thắng
        StaffDraw = 5                 // Nhân viên quyết định hòa
        // StaffNoResponse = 6,       // Nhân viên không phản hồi trong 48h (xem xét như hòa)
    }
    #endregion

    public class BookingDispute : CoreEntity
    {
        // Thông tin cơ bản
        public string BookingId { get; set; } = string.Empty;
        public string LearnerId { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public string StaffId { get; set; } = string.Empty;
        
        // Thông tin vụ việc
        public string CaseNumber { get; set; } = string.Empty; // Định dạng: DSPB-[yyyyMMdd]-[xxx]
        public string LearnerReason { get; set; } = string.Empty;
        public string? TutorResponse { get; set; }
        public string? StaffNotes { get; set; }
        public string? EvidenceUrls { get; set; } // Danh sách URL bằng chứng dạng JSON
        
        // Theo dõi trạng thái
        public DisputeStatus Status { get; set; } = DisputeStatus.PendingReconciliation;
        public DisputeResolution Resolution { get; set; } = DisputeResolution.None;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ReconciliationEndTime { get; set; } // Thời gian kết thúc hòa giải (24h)
        public DateTime? TutorRespondedAt { get; set; }
        public DateTime? StaffReviewEndTime { get; set; } // Thời gian kết thúc xem xét (48h)
        public DateTime? ResolvedAt { get; set; }
        
        // Thuộc tính navigation
        public virtual Booking? Booking { get; set; }
        public virtual Learner? Learner { get; set; }
        public virtual Tutor? Tutor { get; set; }
        public virtual AppUser? Staff { get; set; }

        #region Behaviors
        // Tạo mới một khiếu nại
        public static BookingDispute CreateDispute(
            string bookingId,
            string learnerId,
            string tutorId,
            string learnerReason,
            string? evidenceUrls = null)
        {
            var now = DateTime.UtcNow;
            var caseNumber = $"DSPB-{now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 3)}";
            
            return new BookingDispute
            {
                BookingId = bookingId,
                LearnerId = learnerId,
                TutorId = tutorId,
                CaseNumber = caseNumber,
                LearnerReason = learnerReason,
                EvidenceUrls = evidenceUrls,
                Status = DisputeStatus.PendingReconciliation,
                CreatedAt = now,
                ReconciliationEndTime = now.AddHours(24)
            };
        }

        // Học viên rút khiếu nại
        public Expression<Func<BookingDispute, object>>[] WithdrawDispute()
        {
            if (Status != DisputeStatus.PendingReconciliation)
                throw new InvalidOperationException("Cannot withdraw dispute at current status.");
            
            Status = DisputeStatus.ClosedWithdrawn;
            Resolution = DisputeResolution.LearnerWithdrew;
            ResolvedAt = CoreHelper.SystemTimeNow.DateTime;
            
            return
            [
                x => x.Status,
                x => x.Resolution,
                x => x.ResolvedAt!
            ];
        }

        // Gia sư thêm phản hồi
        public Expression<Func<BookingDispute, object>>[] AddTutorResponse(string response)
        {
            if (Status != DisputeStatus.PendingReconciliation)
                throw new InvalidOperationException("Cannot add response at current status.");
                
            if (CoreHelper.SystemTimeNow > ReconciliationEndTime)
                throw new InvalidOperationException("Response period has ended.");
            
            TutorResponse = response;
            TutorRespondedAt = CoreHelper.SystemTimeNow.DateTime;
            
            return
            [
                x => x.TutorResponse!,
                x => x.TutorRespondedAt!
            ];
        }

        // Chuyển lên nhân viên xử lý
        public Expression<Func<BookingDispute, object>>[] EscalateToStaff(string staffId)
        {
            if (Status != DisputeStatus.PendingReconciliation || string.IsNullOrEmpty(TutorResponse))
                throw new InvalidOperationException("Cannot escalate dispute at current status or without tutor response.");
            
            Status = DisputeStatus.AwaitingStaffReview;
            StaffId = staffId;
            var now = CoreHelper.SystemTimeNow.DateTime;
            StaffReviewEndTime = now.AddHours(48);  
            
            return
            [
                x => x.Status,
                x => x.StaffId,
                x => x.StaffReviewEndTime!
            ];
        }

        // Tự động giải quyết khi gia sư không phản hồi
        public Expression<Func<BookingDispute, object>>[] ResolveNoTutorResponse()
        {
            if (Status != DisputeStatus.PendingReconciliation)
                throw new InvalidOperationException("Cannot resolve as no-response at current status.");
                
            if (CoreHelper.SystemTimeNow <= ReconciliationEndTime)
                throw new InvalidOperationException("Reconciliation period has not ended yet.");
                
            if (!string.IsNullOrEmpty(TutorResponse))
                throw new InvalidOperationException("Tutor has already responded.");
            
            Status = DisputeStatus.ClosedResolved;
            Resolution = DisputeResolution.TutorNoResponse;
            ResolvedAt = CoreHelper.SystemTimeNow.DateTime;
            
            return
            [
                x => x.Status,
                x => x.Resolution,
                x => x.ResolvedAt!
            ];
        }

        // Nhân viên giải quyết khiếu nại
        public Expression<Func<BookingDispute, object>>[] ResolveByStaff(
            DisputeResolution resolution,
            string? staffNotes = null)
        {
            if (Status != DisputeStatus.AwaitingStaffReview)
                throw new InvalidOperationException("Cannot resolve by staff at current status.");
                
            if (resolution != DisputeResolution.StaffLearnerWin && 
                resolution != DisputeResolution.StaffTutorWin && 
                resolution != DisputeResolution.StaffDraw)
                throw new ArgumentException("Invalid resolution for staff decision.");
            
            var updated = new List<Expression<Func<BookingDispute, object>>>();
            
            Resolution = resolution;
            ResolvedAt = CoreHelper.SystemTimeNow.DateTime;
            
            updated.Add(x => x.Resolution);
            updated.Add(x => x.ResolvedAt!);
            
            switch (resolution)
            {
                case DisputeResolution.StaffLearnerWin:
                    Status = DisputeStatus.ResolvedLearnerWin;
                    updated.Add(x => x.Status);
                    break;
                case DisputeResolution.StaffTutorWin:
                    Status = DisputeStatus.ResolvedTutorWin;
                    updated.Add(x => x.Status);
                    break;
                case DisputeResolution.StaffDraw:
                    Status = DisputeStatus.ResolvedDraw;
                    updated.Add(x => x.Status);
                    break;
            }
            
            if (staffNotes != null)
            {
                StaffNotes = staffNotes;
                updated.Add(x => x.StaffNotes!);
            }
            
            return updated.ToArray();
        }

        // Kiểm tra thời gian hòa giải đã hết chưa
        public bool IsReconciliationExpired()
            => CoreHelper.SystemTimeNow > ReconciliationEndTime;

        // Kiểm tra thời gian xem xét đã hết chưa
        public bool IsStaffReviewExpired()
            => StaffReviewEndTime.HasValue && CoreHelper.SystemTimeNow > StaffReviewEndTime.Value;
        
        #endregion
    }
}
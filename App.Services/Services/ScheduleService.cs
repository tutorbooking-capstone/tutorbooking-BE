using App.Core.Base;
using App.Core.Utils;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ScheduleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<WeeklyAvailabilityPattern>> GetTutorAvailabilityPatternsAsync(string tutorId)
        {
            return await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Where(p => p.TutorId == tutorId)
                .Include(p => p.Slots)
                .OrderByDescending(p => p.AppliedFrom)
                .ToListAsync();
        }

        public async Task<List<BookingSlot>> GetTutorBookingSlotsAsync(string tutorId)
        {
            return await _unitOfWork.GetRepository<BookingSlot>()
                .ExistEntities()
                .Where(b => b.TutorId == tutorId)
                .Include(b => b.Slots)
                .ToListAsync();
        }
    }
}
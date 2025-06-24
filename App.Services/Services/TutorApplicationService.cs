using App.Repositories.Models.Papers;
using App.Repositories.UoW;
using App.Services.Interfaces;

namespace App.Services.Services
{
    internal class TutorApplicationService : ITutorApplicationService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;

        public TutorApplicationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        public async Task CreateTutorApplicationAsync(string tutorId)
        {
            var tutorApplication = TutorApplication.Create(tutorId);
            _unitOfWork.GetRepository<TutorApplication>().Insert(tutorApplication);
            await _unitOfWork.SaveAsync();
        }
    }
} 
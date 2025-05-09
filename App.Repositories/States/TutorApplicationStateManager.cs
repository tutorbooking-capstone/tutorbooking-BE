using App.Repositories.Models.Papers;
using App.Repositories.UoW;

namespace App.Repositories.States
{
    public class TutorApplicationStateManager
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;

        public TutorApplicationStateManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion
        
        public async Task UpdateApplicationStatusAsync(string applicationId, ApplicationStatus newStatus)
        {
            var application = await _unitOfWork.GetRepository<TutorApplication>().FindAsync(a => a.Id == applicationId);
            var modifiedProperties = application.UpdateApplicationStatus(newStatus);

            if (modifiedProperties.Length > 0)
                _unitOfWork.GetRepository<TutorApplication>().UpdateFields(application, modifiedProperties);
        }


    }
}

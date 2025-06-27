using App.Repositories.Models.User;
using App.Repositories.UoW;

namespace App.Repositories.States
{
public class TutorStateManager
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;

        public TutorStateManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion
        
        //public async Task UpdateVerificationStatusAsync(string tutorId, VerificationStatus newStatus)
        //{
        //    var tutor = await _unitOfWork.GetRepository<Tutor>().FindAsync(t => t.UserId == tutorId);
        //    var modifiedProperties = tutor.UpdateVerificationStatus(newStatus);

        //    if (modifiedProperties.Length > 0)
        //        _unitOfWork.GetRepository<Tutor>().UpdateFields(tutor, modifiedProperties);
        //}
    }
}

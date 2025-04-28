using App.Repositories.Models;
using App.Repositories.UoW;

namespace App.Repositories.States
{
public class ApplicationRevisionStateManager
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;

        public ApplicationRevisionStateManager(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        public async Task UpdateRevisionActionAsync(string revisionId, RevisionAction newAction)
        {
            var revision = await _unitOfWork.GetRepository<ApplicationRevision>().FindAsync(r => r.Id == revisionId);
            var modifiedProperties = revision.UpdateAction(newAction);

            if (modifiedProperties.Length > 0)
                _unitOfWork.GetRepository<ApplicationRevision>().UpdateFields(revision, modifiedProperties);
        }
    }
}

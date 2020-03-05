using System.Collections.Generic;
using SFB.Web.ApplicationCore.Entities;
using System.Threading.Tasks;
using SFB.Web.ApplicationCore.DataAccess;

namespace SFB.Web.ApplicationCore.Services.DataAccess
{
    public class ContextDataService : IContextDataService
    {
        private readonly IEdubaseRepository _edubaseRepository;

        public ContextDataService(IEdubaseRepository edubaseRepository)
        {
            _edubaseRepository = edubaseRepository;
        }

        public EdubaseDataObject GetSchoolDataObjectByUrn(int urn)
        {
            return _edubaseRepository.GetSchoolDataObjectByUrn(urn);
        }

        public List<int> GetAllSchoolUrns()
        {
            return _edubaseRepository.GetAllSchoolUrns();
        }

        public List<EdubaseDataObject> GetSchoolDataObjectByLaEstab(string laEstab, bool openOnly)
        {
            return _edubaseRepository.GetSchoolsByLaEstab(laEstab, openOnly);
        }

        public List<EdubaseDataObject> GetMultipleSchoolDataObjectsByUrns(List<int> urns)
        {
            return _edubaseRepository.GetMultipleSchoolDataObjectsByUrns(urns);
        }

        public async Task<IEnumerable<EdubaseDataObject>> GetSchoolsByCompanyNumberAsync(int companyNo)
        {
            return await _edubaseRepository.GetSchoolsByCompanyNoAsync(companyNo);
        }

        public async Task<int> GetAcademiesCountByCompanyNumberAsync(int companyNo)
        {
            return await _edubaseRepository.GetAcademiesCountByCompanyNoAsync(companyNo);
        }
    }
}
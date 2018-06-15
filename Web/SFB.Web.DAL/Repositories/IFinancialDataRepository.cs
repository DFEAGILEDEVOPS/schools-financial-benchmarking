﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using SFB.Web.Common;
using SFB.Web.Common.DataObjects;

namespace SFB.Web.DAL.Repositories
{
    public interface IFinancialDataRepository
    {
        List<AcademiesContextualDataObject> GetAcademiesContextualDataObject(string term, string matNo);

        Task<IEnumerable<Document>> GetSchoolDataDocumentAsync(int urn, string term, EstablishmentType schoolFinancialType, CentralFinancingType cFinance = CentralFinancingType.Exclude);
        Document GetSchoolDataDocument(int urn, string term, EstablishmentType schoolFinancialType, CentralFinancingType cFinance = CentralFinancingType.Exclude);
        Document GetMATDataDocument(string matNo, string term, MatFinancingType matFinance);
        Task<IEnumerable<Document>> GetMATDataDocumentAsync(string matNo, string term, MatFinancingType matFinance);
        Task<List<Document>> SearchSchoolsByCriteriaAsync(BenchmarkCriteria criteria, EstablishmentType estType);
        Task<int> SearchSchoolsCountByCriteriaAsync(BenchmarkCriteria criteria, EstablishmentType estType);
        Task<List<Document>> SearchTrustsByCriteriaAsync(BenchmarkCriteria criteria);
        Task<int> SearchTrustCountByCriteriaAsync(BenchmarkCriteria criteria);        
        Task<int> GetEstablishmentRecordCountAsync(string term, EstablishmentType estType);
    }
}

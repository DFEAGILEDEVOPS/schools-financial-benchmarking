﻿using System.Collections.Generic;
using System.Threading.Tasks;
using SFB.Web.ApplicationCore.Entities;
using SFB.Web.ApplicationCore.Models;
using SFB.Web.ApplicationCore.Helpers.Enums;

namespace SFB.Web.ApplicationCore.Services.DataAccess
{
    public interface IFinancialDataService : ITermYearDataService
    {
        List<AcademiesContextualDataObject> GetAcademiesByCompanyNumber(string term, int companyNo);
        SchoolTrustFinancialDataObject GetTrustFinancialDataObject(int companyNo, string term, MatFinancingType matFinance);
        List<SchoolTrustFinancialDataObject> GetMultipleTrustDataObjectsByCompanyNumbers(List<int> companyNos);
        SchoolTrustFinancialDataObject GetTrustFinancialDataObjectByMatName(string matName, string term, MatFinancingType matFinance);
        FinancialDataModel GetSchoolsLatestFinancialDataModel(int urn, EstablishmentType schoolFinancialType);
        SchoolTrustFinancialDataObject GetSchoolFinancialDataObject(int urn, string term, EstablishmentType schoolFinancialType, CentralFinancingType cFinance = CentralFinancingType.Exclude);
        Task<IEnumerable<SchoolTrustFinancialDataObject>> GetSchoolFinancialDataObjectAsync(int urn, string term, EstablishmentType schoolFinancialType, CentralFinancingType cFinance = CentralFinancingType.Exclude);
        Task<IEnumerable<SchoolTrustFinancialDataObject>> GetTrustFinancialDataObjectAsync(int companyNo, string term, MatFinancingType matFinance);
        Task<List<SchoolTrustFinancialDataObject>> SearchTrustsByCriteriaAsync(BenchmarkCriteria criteria);
        Task<List<SchoolTrustFinancialDataObject>> SearchSchoolsByCriteriaAsync(BenchmarkCriteria criteria, EstablishmentType estType);
        Task<List<SchoolTrustFinancialDataObject>> SearchSchoolsByCriteriaAsync(BenchmarkCriteria criteria, EstablishmentType estType, bool excludePartial);
        Task<int> SearchSchoolsCountByCriteriaAsync(BenchmarkCriteria criteria, EstablishmentType estType, bool excludePartial = false);
        Task<int> SearchTrustCountByCriteriaAsync(BenchmarkCriteria criteria);
        Task<int> GetEstablishmentRecordCountAsync(string term, EstablishmentType estType);        
    }

    public interface ITermYearDataService
    {
        List<string> GetActiveTermsForMatCentral();
        List<string> GetActiveTermsForAcademies();
        List<string> GetActiveTermsForMaintained();
        int GetLatestFinancialDataYear();        
        int GetLatestDataYearPerEstabType(EstablishmentType type);
    }
}
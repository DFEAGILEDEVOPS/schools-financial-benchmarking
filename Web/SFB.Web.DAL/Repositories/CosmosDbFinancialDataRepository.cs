﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using SFB.Web.Common;
using SFB.Web.Common.Attributes;
using SFB.Web.DAL.Helpers;
using SFB.Web.Common.DataObjects;

namespace SFB.Web.DAL.Repositories
{
    public class CosmosDbFinancialDataRepository : AppInsightsLoggable, IFinancialDataRepository
    {
        private static readonly string DatabaseId = ConfigurationManager.AppSettings["database"];
        private static DocumentClient _client;
        private readonly IDataCollectionManager _dataCollectionManager;

        public CosmosDbFinancialDataRepository(IDataCollectionManager dataCollectionManager)
        {
            _dataCollectionManager = dataCollectionManager;

            _client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["endpoint"]),
                ConfigurationManager.AppSettings["authKey"],
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                });
        }

        public SchoolTrustFinancialDataObject GetSchoolFinancialDataObject(int urn, string term, EstablishmentType estabType, CentralFinancingType cFinance)
        {
            var dataGroup = estabType.ToDataGroup(cFinance);

            var collectionName = _dataCollectionManager.GetCollectionIdByTermByDataGroup(term, dataGroup);

            if (collectionName == null)
            {
                return null;
            }

            var query = $"SELECT * FROM c WHERE c.{SchoolTrustFinanceDBFieldNames.URN}=@URN";
            SqlQuerySpec querySpec = new SqlQuerySpec(query);
            querySpec.Parameters = new SqlParameterCollection();
            querySpec.Parameters.Add(new SqlParameter($"@URN", urn));

            try
            {
                var documentQuery = _client.CreateDocumentQuery<SchoolTrustFinancialDataObject>(UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName), querySpec);

                var result = documentQuery.ToList().FirstOrDefault();

                if (dataGroup == DataGroups.MATAllocs && result == null)//if nothing found in MAT-Allocs collection try to source it from Academies data
                {
                    return GetSchoolFinancialDataObject(urn, term, estabType, CentralFinancingType.Exclude);
                }

                if (result != null && result.DidNotSubmit)//School did not submit finance, return & display none in the charts
                {
                    return null;
                }

                return result;

            }
            catch (Exception ex)
            {
                if (term.Contains(_dataCollectionManager.GetLatestFinancialDataYearPerEstabType(estabType).ToString()))
                {
                    var errorMessage = $"{collectionName} could not be loaded! : {ex.Message} : {querySpec.Parameters[0].Name} = {querySpec.Parameters[0].Value}";
                    base.LogException(ex, errorMessage);
                }
                return null;
            }
        }

        public async Task<IEnumerable<SchoolTrustFinancialDataObject>> GetSchoolFinanceDataObjectAsync(int urn, string term, EstablishmentType estabType, CentralFinancingType cFinance)
        {
            var dataGroup = estabType.ToDataGroup(cFinance);

            var collectionName = _dataCollectionManager.GetCollectionIdByTermByDataGroup(term, dataGroup);

            var query = $"SELECT * FROM c WHERE c.{SchoolTrustFinanceDBFieldNames.URN}=@URN";
            SqlQuerySpec querySpec = new SqlQuerySpec(query);
            querySpec.Parameters = new SqlParameterCollection();
            querySpec.Parameters.Add(new SqlParameter($"@URN", urn));

            try
            {
                var documentQuery =
                    _client.CreateDocumentQuery<SchoolTrustFinancialDataObject>(
                        UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName),
                        querySpec);

                return await documentQuery.QueryAsync();

            }
            catch (Exception ex)
            {
                if (term.Contains(_dataCollectionManager.GetLatestFinancialDataYearPerEstabType(estabType).ToString()))
                {
                    var errorMessage = $"{collectionName} could not be loaded! : {ex.Message} : {querySpec.Parameters[0].Name} = {querySpec.Parameters[0].Value}";
                    base.LogException(ex, errorMessage);
                }
                return null;
            }
        }

        public List<AcademiesContextualDataObject> GetAcademiesContextualDataObject(string term, int companyNo)
        {
            var collectionName =
                _dataCollectionManager.GetActiveCollectionsByDataGroup(DataGroups.Academies)
                    .SingleOrDefault(sod => sod.Split('-').Last() == term.Split(' ').Last());

            var query = $"SELECT c['{SchoolTrustFinanceDBFieldNames.URN}'], c['{SchoolTrustFinanceDBFieldNames.SCHOOL_NAME}'] as EstablishmentName, c['{SchoolTrustFinanceDBFieldNames.PERIOD_COVERED_BY_RETURN}'], c['{SchoolTrustFinanceDBFieldNames.TRUST_COMPANY_NAME}'] " +
                $"FROM c WHERE c['{SchoolTrustFinanceDBFieldNames.COMPANY_NUMBER}']=@companyNo";
            SqlQuerySpec querySpec = new SqlQuerySpec(query);
            querySpec.Parameters = new SqlParameterCollection();
            querySpec.Parameters.Add(new SqlParameter($"@companyNo", companyNo));

            try
            {
                var results =
                    _client.CreateDocumentQuery<AcademiesContextualDataObject>(
                        UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName), querySpec).ToList();

                return results;
            }
            catch (Exception ex)
            {
                var errorMessage = $"{collectionName} could not be loaded! : {ex.Message} : {querySpec.Parameters[0].Name} = {querySpec.Parameters[0].Value}";
                base.LogException(ex, errorMessage);

                return null;
            }
        }

        public SchoolTrustFinancialDataObject GetTrustFinancialDataObject(int companyNo, string term, MatFinancingType matFinance)
        {
            var dataGroup = EstablishmentType.MAT.ToDataGroup(matFinance);

            var collectionName = _dataCollectionManager.GetCollectionIdByTermByDataGroup(term, dataGroup);

            if (collectionName == null)
            {
                return null;
            }

            var query = $"SELECT * FROM c WHERE c['{SchoolTrustFinanceDBFieldNames.COMPANY_NUMBER}']=@companyNo";
            SqlQuerySpec querySpec = new SqlQuerySpec(query);
            querySpec.Parameters = new SqlParameterCollection();
            querySpec.Parameters.Add(new SqlParameter($"@companyNo", companyNo));

            var res =
                _client.CreateDocumentQuery<SchoolTrustFinancialDataObject>(
                    UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName),
                    querySpec);

            try
            {
                var result = res.ToList().FirstOrDefault();

                if (result != null && result.DidNotSubmit)
                {
                    var emptyObj = new SchoolTrustFinancialDataObject();
                    emptyObj.DidNotSubmit = true;
                    return emptyObj;
                }
                return result;
            }
            catch (Exception ex)
            {
                if (term.Contains(_dataCollectionManager.GetLatestFinancialDataYearPerEstabType(EstablishmentType.MAT).ToString()))
                {
                    var errorMessage = $"{collectionName} could not be loaded! : {ex.Message} : {querySpec.Parameters[0].Name} = {querySpec.Parameters[0].Value}";
                    base.LogException(ex, errorMessage);
                }
                return null;
            }
        }

        public List<SchoolTrustFinancialDataObject> GetMultipleTrustFinancialDataObjects(List<int> companyNoList, string term, MatFinancingType matFinance)
        {
            var dataGroup = EstablishmentType.MAT.ToDataGroup(matFinance);

            var collectionName = _dataCollectionManager.GetCollectionIdByTermByDataGroup(term, dataGroup);

            if (collectionName == null)
            {
                return null;
            }

            var query = $"SELECT * FROM c WHERE c['{SchoolTrustFinanceDBFieldNames.COMPANY_NUMBER}'] in ({string.Join(",", companyNoList)})";

            var results = _client.CreateDocumentQuery<SchoolTrustFinancialDataObject>(UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName), query);

            List<SchoolTrustFinancialDataObject> resultsList;
            try
            {
                resultsList = results.ToList();

                resultsList.ForEach(result => {
                    if (result.DidNotSubmit)
                    {
                        var emptyObj = new SchoolTrustFinancialDataObject();
                        emptyObj.DidNotSubmit = true;
                        result = emptyObj;
                    }
                });               
            }
            catch (Exception ex)
            {
                var errorMessage = $"{collectionName} could not be loaded! : {ex.Message} : URNs = {string.Join(",", companyNoList)}";
                base.LogException(ex, errorMessage);
                throw new ApplicationException($"One or more documents could not be loaded from {collectionName} : URNs = {string.Join(",", companyNoList)}");
            }

            return resultsList;
        }

        public SchoolTrustFinancialDataObject GetTrustFinancialDataObjectByMatName(string matName, string term, MatFinancingType matFinance)
        {
            var dataGroup = EstablishmentType.MAT.ToDataGroup(matFinance);

            var collectionName = _dataCollectionManager.GetCollectionIdByTermByDataGroup(term, dataGroup);

            if (collectionName == null)
            {
                return null;
            }

            var query = $"SELECT * FROM c WHERE c['{SchoolTrustFinanceDBFieldNames.TRUST_COMPANY_NAME}']=@matName";
            SqlQuerySpec querySpec = new SqlQuerySpec(query);
            querySpec.Parameters = new SqlParameterCollection();
            querySpec.Parameters.Add(new SqlParameter($"@matName", matName));

            var res =
                _client.CreateDocumentQuery<SchoolTrustFinancialDataObject>(
                    UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName),
                    querySpec);

            try
            {
                var result = res.ToList().FirstOrDefault();

                if (result != null && result.DidNotSubmit)
                {
                    var emptyObj = new SchoolTrustFinancialDataObject();
                    emptyObj.DidNotSubmit = true;
                    return emptyObj;
                }
                return result;
            }
            catch (Exception ex)
            {
                if (term.Contains(_dataCollectionManager.GetLatestFinancialDataYearPerEstabType(EstablishmentType.MAT).ToString()))
                {
                    var errorMessage = $"{collectionName} could not be loaded! : {ex.Message} : {querySpec.Parameters[0].Name} = {querySpec.Parameters[0].Value}";
                    base.LogException(ex, errorMessage);
                }
                return null;
            }
        }

        public async Task<IEnumerable<SchoolTrustFinancialDataObject>> GetTrustFinancialDataObjectAsync(int companyNo, string term, MatFinancingType matFinance)
        {
            var dataGroup = EstablishmentType.MAT.ToDataGroup(matFinance);

            var collectionName = _dataCollectionManager.GetCollectionIdByTermByDataGroup(term, dataGroup);

            var query = $"SELECT * FROM c WHERE c['{SchoolTrustFinanceDBFieldNames.COMPANY_NUMBER}']=@companyNo";
            SqlQuerySpec querySpec = new SqlQuerySpec(query);
            querySpec.Parameters = new SqlParameterCollection();
            querySpec.Parameters.Add(new SqlParameter($"@companyNo", companyNo));

            try
            {
                var documentQuery =
                    _client.CreateDocumentQuery<SchoolTrustFinancialDataObject>(
                        UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName),
                        querySpec);

                return await documentQuery.QueryAsync();
            }
            catch (Exception ex)
            {
                if (term.Contains(_dataCollectionManager.GetLatestFinancialDataYearPerEstabType(EstablishmentType.MAT).ToString()))
                {
                    var errorMessage = $"{collectionName} could not be loaded! : {ex.Message} : {querySpec.Parameters[0].Name} = {querySpec.Parameters[0].Value}";
                    base.LogException(ex, errorMessage);
                }
                return null;
            }
        }

        public async Task<List<SchoolTrustFinancialDataObject>> SearchSchoolsByCriteriaAsync(BenchmarkCriteria criteria, EstablishmentType estType, bool excludePartial = false)
        {
            if (estType == EstablishmentType.All)
            {
                var maintainedSchoolsTask = QueryDBSchoolCollectionAsync(criteria, DataGroups.Maintained, excludePartial);
                var academiesTask = QueryDBSchoolCollectionAsync(criteria, DataGroups.Academies, excludePartial);
                var maintainedSchools = (await maintainedSchoolsTask).ToList();
                var academies = (await academiesTask).ToList();
                maintainedSchools.AddRange(academies);
                return maintainedSchools;
            }
            else
            {
                return (await QueryDBSchoolCollectionAsync(criteria, estType.ToDataGroup(), excludePartial)).ToList();
            }
        }

        public async Task<int> SearchSchoolsCountByCriteriaAsync(BenchmarkCriteria criteria, EstablishmentType estType, bool excludePartial = false)
        {
            if (estType == EstablishmentType.All)
            {
                var maintainedSchoolsCountTask = QueryDBSchoolCollectionForCountAsync(criteria, DataGroups.Maintained, excludePartial);
                var academiesCountTask = QueryDBSchoolCollectionForCountAsync(criteria, DataGroups.Academies, excludePartial);
                return (await maintainedSchoolsCountTask).First() + (await academiesCountTask).First();
            }
            else
            {
                var type = estType == EstablishmentType.Academies ? DataGroups.Academies : DataGroups.Maintained;
                var result = (await QueryDBSchoolCollectionForCountAsync(criteria, type, excludePartial)).First();
                return result;
            }
        }

        public async Task<List<SchoolTrustFinancialDataObject>> SearchTrustsByCriteriaAsync(BenchmarkCriteria criteria)
        {
            return (await QueryDBTrustCollectionAsync(criteria, DataGroups.MATOverview)).ToList();
        }

        public async Task<int> SearchTrustCountByCriteriaAsync(BenchmarkCriteria criteria)
        {
            return (await QueryDBTrustCollectionForCountAsync(criteria)).First();
        }

        public async Task<int> GetEstablishmentRecordCountAsync(string term, EstablishmentType estType)
        {
            var collectionName = string.Empty;
            switch (estType)
            {
                case EstablishmentType.Academies:
                    collectionName = _dataCollectionManager.GetCollectionIdByTermByDataGroup(term, DataGroups.MATAllocs);
                    break;
                case EstablishmentType.Maintained:
                    collectionName = _dataCollectionManager.GetCollectionIdByTermByDataGroup(term, DataGroups.Maintained);
                    break;
                case EstablishmentType.MAT:
                    collectionName = _dataCollectionManager.GetCollectionIdByTermByDataGroup(term, DataGroups.MATOverview);
                    break;
            }

            var result =
                _client.CreateDocumentQuery<int>(UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName), $"SELECT VALUE COUNT(c) FROM c");

            return (await result.QueryAsync()).First();
        }

        private async Task<IEnumerable<SchoolTrustFinancialDataObject>> QueryDBSchoolCollectionAsync(BenchmarkCriteria criteria, string dataGroup, bool excludePartial = false)
        {
            var collectionName = _dataCollectionManager.GetLatestActiveCollectionIdByDataGroup(dataGroup);

            var query = BuildQueryFromBenchmarkCriteria(criteria);

            query = Exclude6Forms(query);

            if(excludePartial)
            {
                query = ExcludePartials(query);
            }

            if (string.IsNullOrEmpty(query))
            {
                return new List<SchoolTrustFinancialDataObject>();
            }

            IQueryable<SchoolTrustFinancialDataObject> result;

            try
            {
                if (dataGroup == DataGroups.Academies || dataGroup == DataGroups.MATAllocs)
                {
                    result = _client.CreateDocumentQuery<SchoolTrustFinancialDataObject>(
                        UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName),
                        $"SELECT c['{SchoolTrustFinanceDBFieldNames.URN}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.SCHOOL_NAME}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.SCHOOL_TYPE}'], " +
                        $"'{DataGroups.Academies}' AS {SchoolTrustFinanceDBFieldNames.FINANCE_TYPE}, " +
                        $"c['{SchoolTrustFinanceDBFieldNames.NO_PUPILS}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.SCHOOL_PHASE}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.SCHOOL_OVERALL_PHASE}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.KS2_PROGRESS}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.PROGRESS_8_MEASURE}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.TOTAL_EXP_PP}'] " +
                        $"FROM c WHERE {query}");
                }
                else
                {
                    result = _client.CreateDocumentQuery<SchoolTrustFinancialDataObject>(
                        UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName),
                        $"SELECT c['{SchoolTrustFinanceDBFieldNames.URN}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.SCHOOL_NAME}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.SCHOOL_TYPE}'], " +
                        $"'{DataGroups.Maintained}' AS {SchoolTrustFinanceDBFieldNames.FINANCE_TYPE}, " +
                        $"c['{SchoolTrustFinanceDBFieldNames.NO_PUPILS}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.SCHOOL_PHASE}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.SCHOOL_OVERALL_PHASE}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.KS2_PROGRESS}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.PROGRESS_8_MEASURE}'], " +
                        $"c['{SchoolTrustFinanceDBFieldNames.TOTAL_EXP_PP}'] " +
                        $"FROM c WHERE {query}");
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"{collectionName} could not be loaded! : {ex.Message} : {query}";
                base.LogException(ex, errorMessage);

                return null;
            }
            return await result.QueryAsync();
        }

        private async Task<IEnumerable<SchoolTrustFinancialDataObject>> QueryDBTrustCollectionAsync(BenchmarkCriteria criteria, string dataGroup)
        {
            var collectionName = _dataCollectionManager.GetLatestActiveCollectionIdByDataGroup(dataGroup);

            var query = BuildQueryFromBenchmarkCriteria(criteria);

            query = ExcludeSAMATs(query);

            if (string.IsNullOrEmpty(query))
            {
                return new List<SchoolTrustFinancialDataObject>();
            }

            IQueryable<SchoolTrustFinancialDataObject> result;

            try
            {
                result = _client.CreateDocumentQuery<SchoolTrustFinancialDataObject>(
                   UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName),
                   $"SELECT c['{SchoolTrustFinanceDBFieldNames.COMPANY_NUMBER}'], c['{SchoolTrustFinanceDBFieldNames.TRUST_COMPANY_NAME}'] FROM c WHERE {query}");
            }
            catch (Exception ex)
            {
                var errorMessage = $"{collectionName} could not be loaded! : {ex.Message} : {query}";
                base.LogException(ex, errorMessage);

                return null;
            }

            return await result.QueryAsync();
        }

        private async Task<IEnumerable<int>> QueryDBSchoolCollectionForCountAsync(BenchmarkCriteria criteria, string type, bool excludePartial = false)
        {
            var collectionName = _dataCollectionManager.GetLatestActiveCollectionIdByDataGroup(type);

            var query = BuildQueryFromBenchmarkCriteria(criteria);

            query = Exclude6Forms(query);

            if (excludePartial)
            {
                query = ExcludePartials(query);
            }

            if (string.IsNullOrEmpty(query))
            {
                return new List<int> { 0 };
            }

            var result =
                _client.CreateDocumentQuery<int>(UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName),
                    $"SELECT VALUE COUNT(c) FROM c WHERE {query}");

            return await result.QueryAsync();
        }

        private async Task<IEnumerable<int>> QueryDBTrustCollectionForCountAsync(BenchmarkCriteria criteria)
        {
            var collectionName = _dataCollectionManager.GetLatestActiveCollectionIdByDataGroup(DataGroups.MATOverview);

            var query = BuildQueryFromBenchmarkCriteria(criteria);

            query = ExcludeSAMATs(query);

            if (string.IsNullOrEmpty(query))
            {
                return new List<int> { 0 };
            }

            var result =
                _client.CreateDocumentQuery<int>(UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionName),
                    $"SELECT VALUE COUNT(c) FROM c WHERE {query}");

            return await result.QueryAsync();
        }

        private string ExcludeSAMATs(string query)
        {
            return $"{query} AND c.{SchoolTrustFinanceDBFieldNames.MEMBER_COUNT} > 1";
        }

        private string Exclude6Forms(string query)
        {
            if(string.IsNullOrEmpty(query))
            {
                return $"c['{SchoolTrustFinanceDBFieldNames.SCHOOL_TYPE}'] != 'Free 16-19'";
            }
            return $"{query} AND c['{SchoolTrustFinanceDBFieldNames.SCHOOL_TYPE}'] != 'Free 16-19'";
        }

        private string ExcludePartials(string query)
        {
            if(string.IsNullOrEmpty(query))
            {
                return $"c['{SchoolTrustFinanceDBFieldNames.PERIOD_COVERED_BY_RETURN}'] = 12";
            }
            return $"{query} AND c['{SchoolTrustFinanceDBFieldNames.PERIOD_COVERED_BY_RETURN}'] = 12";
        }

        private string BuildQueryFromBenchmarkCriteria(BenchmarkCriteria criteria)
        {
            var queryBuilder = new StringBuilder();
            foreach (var property in (typeof(BenchmarkCriteria)).GetProperties())
            {
                var attribute = property.GetCustomAttributes(typeof(DBFieldAttribute)).FirstOrDefault();

                if (attribute != null && attribute is DBFieldAttribute)
                {
                    var fieldValue = criteria.GetType().GetProperty(property.Name).GetValue(criteria, null);
                    if (fieldValue != null)
                    {
                        var docName = (attribute as DBFieldAttribute).DocName;
                        var fieldName = (attribute as DBFieldAttribute).Name;
                        var fieldType = (attribute as DBFieldAttribute).Type;
                        switch (fieldType)
                        {
                            case CriteriaFieldComparisonTypes.MIN:
                                if (string.IsNullOrEmpty(docName))
                                {
                                    queryBuilder.Append($"c['{fieldName}'] >= {fieldValue}");
                                }
                                else
                                {
                                    queryBuilder.Append($"c['{docName}']['{fieldName}'] >= {fieldValue}");
                                }
                                break;
                            case CriteriaFieldComparisonTypes.MAX:
                                if (string.IsNullOrEmpty(docName))
                                {
                                    queryBuilder.Append($"c['{fieldName}'] <= {fieldValue}");
                                }
                                else
                                {
                                    queryBuilder.Append($"c['{docName}']['{fieldName}'] <= {fieldValue}");
                                }
                                break;
                            case CriteriaFieldComparisonTypes.EQUALTO:
                                if (fieldValue is int)
                                {
                                    if (string.IsNullOrEmpty(docName))
                                    {
                                        queryBuilder.Append($"c['{fieldName}'] = {fieldValue}");
                                    }
                                    else
                                    {
                                        queryBuilder.Append($"c['{docName}']['{fieldName}'] = {fieldValue}");
                                    }
                                }
                                else if (fieldValue is string)
                                {
                                    if (string.IsNullOrEmpty(docName))
                                    {
                                        queryBuilder.Append($"c['{fieldName}'] = '{fieldValue}'");
                                    }
                                    else
                                    {
                                        queryBuilder.Append($"c['{docName}']['{fieldName}'] = '{fieldValue}'");
                                    }
                                }
                                else if (fieldValue is Array)
                                {
                                    queryBuilder.Append(
                                        $"c['{fieldName}'] IN ('{string.Join("','", fieldValue as string[])}')");
                                }
                                break;
                        }

                        queryBuilder.Append(" AND ");
                    }
                }

            }
            var query = queryBuilder.ToString();
            return string.IsNullOrEmpty(query) ? query : query.Substring(0, query.Length - 5);
        }        
    }
}
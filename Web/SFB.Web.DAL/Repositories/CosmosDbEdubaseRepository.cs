﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using SFB.Web.Common;
using SFB.Web.DAL.Helpers;
using SFB.Web.Common.DataObjects;

namespace SFB.Web.DAL.Repositories
{
    public class CosmosDbEdubaseRepository : AppInsightsLoggable, IEdubaseRepository
    {
        private static readonly string DatabaseId = ConfigurationManager.AppSettings["database"];
        private static DocumentClient _client;
        private readonly string _collectionName;

        public CosmosDbEdubaseRepository(IDataCollectionManager dataCollectionManager)
        {
            _client = new DocumentClient(new Uri(ConfigurationManager.AppSettings["endpoint"]),
                ConfigurationManager.AppSettings["authKey"],
                new ConnectionPolicy
                {
                    ConnectionMode = ConnectionMode.Direct,
                    ConnectionProtocol = Protocol.Tcp
                });

            _collectionName = dataCollectionManager.GetLatestActiveCollectionByDataGroup(DataGroups.Edubase);

            CreateUDFs();
        }

        private void CreateUDFs()
        {
            UserDefinedFunction parseFtUdf = new UserDefinedFunction()
            {
                Id = "PARSE_FINANCIAL_TYPE_CODE",
                Body = @"function(code) {
                switch (code) {
                   case 'A':
                       return 'Academies';
                   case 'M':
                       return 'Maintained';
                   default:
                       return 'Maintained';
                    }
                }"
            };

            _client.CreateUserDefinedFunctionAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, _collectionName), parseFtUdf);
        }

        public EdubaseDataObject GetSchoolDataObjectByUrn(int urn)
        {
            return GetSchoolDataObjectById(new Dictionary<string, object> { { SchoolTrustFinanceDBFieldNames.URN, urn } }).FirstOrDefault();
        }

        public List<EdubaseDataObject> GetMultipleSchoolDataObjectsByUrns(List<int> urns)
        {
            return GetMultipleSchoolDataObjetsByIds(SchoolTrustFinanceDBFieldNames.URN, urns);
        }

        public List<EdubaseDataObject> GetSchoolsByLaEstab(string laEstab, bool openOnly)
        {
            var parameters = new Dictionary<string, object>
            {
                {EdubaseDBFieldNames.LA_CODE, Int32.Parse(laEstab.Substring(0, 3))},
                {EdubaseDBFieldNames.ESTAB_NO, Int32.Parse(laEstab.Substring(3))}
            };

            if(openOnly)
            {
                parameters.Add(EdubaseDBFieldNames.ESTAB_STATUS, "Open");
            }

            return GetSchoolDataObjectById(parameters);
        }

        #region Private methods
       
        private List<EdubaseDataObject> GetSchoolDataObjectById(Dictionary<string, object> fields)
        {

            var sb = new StringBuilder();
            foreach (var field in fields)
            {
                sb.Append($"c.{field.Key}=@{field.Key} AND ");
            }

            var where = sb.ToString().Substring(0, sb.ToString().Length - 5);

            var query =
                $"SELECT c['{EdubaseDBFieldNames.URN}'], c['{EdubaseDBFieldNames.ESTAB_NAME}'], c['{EdubaseDBFieldNames.OVERALL_PHASE}'], c['{EdubaseDBFieldNames.PHASE_OF_EDUCATION}'], c['{EdubaseDBFieldNames.TYPE_OF_ESTAB}'], c['{EdubaseDBFieldNames.STREET}'], c['{EdubaseDBFieldNames.TOWN}'], c['{EdubaseDBFieldNames.LOCATION}'], c['{EdubaseDBFieldNames.POSTCODE}'], c['{EdubaseDBFieldNames.TRUSTS}'], c['{EdubaseDBFieldNames.MAT_NUMBER}'], c['{EdubaseDBFieldNames.COMPANY_NUMBER}']," +
                $"c['{EdubaseDBFieldNames.LA_CODE}'], c['{EdubaseDBFieldNames.ESTAB_NO}'], c['{EdubaseDBFieldNames.TEL_NO}'], c['{EdubaseDBFieldNames.NO_PUPIL}'], c['{EdubaseDBFieldNames.STAT_LOW}'], c['{EdubaseDBFieldNames.STAT_HIGH}'], c['{EdubaseDBFieldNames.HEAD_FIRST_NAME}'], " +
                $"c['{EdubaseDBFieldNames.HEAD_LAST_NAME}'], c['{EdubaseDBFieldNames.OFFICIAL_6_FORM}'], c['{EdubaseDBFieldNames.SCHOOL_WEB_SITE}'], c['{EdubaseDBFieldNames.OFSTED_RATING}'], c['{EdubaseDBFieldNames.OFSTE_LAST_INSP}'], udf.PARSE_FINANCIAL_TYPE_CODE(c['{EdubaseDBFieldNames.FINANCE_TYPE}']) AS {EdubaseDBFieldNames.FINANCE_TYPE}, c['{EdubaseDBFieldNames.OPEN_DATE}'], c['{EdubaseDBFieldNames.CLOSE_DATE}'] FROM c WHERE {where}";

            SqlQuerySpec querySpec = new SqlQuerySpec(query);
            querySpec.Parameters = new SqlParameterCollection();
            foreach (var field in fields)
            {
                querySpec.Parameters.Add(new SqlParameter($"@{field.Key}", field.Value));
            }

            List<EdubaseDataObject> result;
            try
            {
                result = _client.CreateDocumentQuery<EdubaseDataObject>(UriFactory.CreateDocumentCollectionUri(DatabaseId, _collectionName), querySpec, new FeedOptions() { MaxItemCount = 1 }).ToList();
            }catch(Exception ex)
            {
                var errorMessage = $"{_collectionName} could not be loaded! : {ex.Message} : {querySpec.Parameters[0].Name} = {querySpec.Parameters[0].Value}";
                base.LogException(ex, errorMessage);
                return null;
            }
            if (result == null)
            {
                throw new ApplicationException("School document not found in Edubase collection!");
            }
            return result;
        }

        private List<EdubaseDataObject> GetMultipleSchoolDataObjetsByIds(string fieldName, List<int> ids)
        {
            var sb = new StringBuilder();
            ids.ForEach(u => sb.Append(u + ","));

            var query = $"SELECT c['{EdubaseDBFieldNames.URN}'], " +
                $"c['{EdubaseDBFieldNames.ESTAB_NAME}'], " +
                $"c['{EdubaseDBFieldNames.OVERALL_PHASE}'], " +
                $"c['{EdubaseDBFieldNames.TYPE_OF_ESTAB}'], " +
                $"c['{EdubaseDBFieldNames.STREET}'], " +
                $"c['{EdubaseDBFieldNames.TOWN}'], " +
                $"c['{EdubaseDBFieldNames.POSTCODE}'], " +
                $"c['{EdubaseDBFieldNames.LA_CODE}'], " +
                $"c['{EdubaseDBFieldNames.NO_PUPIL}'], " +
                $"udf.PARSE_FINANCIAL_TYPE_CODE(c['{EdubaseDBFieldNames.FINANCE_TYPE}']) AS {EdubaseDBFieldNames.FINANCE_TYPE} " +
                $"FROM c WHERE c.{fieldName} IN ({sb.ToString().TrimEnd(',')})";

            List<EdubaseDataObject> result = null;
            try
            {
                result = _client.CreateDocumentQuery<EdubaseDataObject>(UriFactory.CreateDocumentCollectionUri(DatabaseId, _collectionName), query).ToList();
                if(result.Count < ids.Count)
                {
                    throw new Newtonsoft.Json.JsonSerializationException();
                }
            }catch(Exception ex)
            {
                var errorMessage = $"{_collectionName} could not be loaded! : {ex.Message} : URNs = {sb.ToString().TrimEnd(',')}";
                base.LogException(ex, errorMessage);
                throw new ApplicationException($"One or more documents could not be loaded from {_collectionName} : URNs = {sb.ToString().TrimEnd(',')}");
            }
            return result;
        }

        public List<int> GetAllSchoolUrns()
        {
            var query = $"SELECT VALUE c.URN FROM c";

            var result = _client.CreateDocumentQuery<int>(UriFactory.CreateDocumentCollectionUri(DatabaseId, _collectionName), query).ToList();

            return result;
        }


        #endregion
    }
}
﻿using Moq;
using NUnit.Framework;
using SFB.Web.UI.Controllers;
using SFB.Web.UI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using SFB.Web.UI.Helpers.Enums;
using SFB.Web.ApplicationCore.Services.DataAccess;
using SFB.Web.UI.Helpers;
using SFB.Web.ApplicationCore.Entities;
using SFB.Web.ApplicationCore.Services.Comparison;
using SFB.Web.ApplicationCore.DataAccess;
using SFB.Web.ApplicationCore.Helpers.Enums;
using SFB.Web.ApplicationCore.Models;
using SFB.Web.ApplicationCore.Services.LocalAuthorities;
using SFB.Web.ApplicationCore.Services;

namespace SFB.Web.UI.UnitTests
{
    public class BenchmarkCriteriaControllerUnitTests
    {
        [Test]
        public void AskForOverwriteStrategyIfMultipleSchoolsInComparisonList()
        {
            var mockCookieManager = new Mock<IBenchmarkBasketCookieManager>();
            var fakeSchoolComparisonList = new SchoolComparisonListModel();
            fakeSchoolComparisonList.BenchmarkSchools.Add(new BenchmarkSchoolModel() { Name = "test" });
            fakeSchoolComparisonList.BenchmarkSchools.Add(new BenchmarkSchoolModel() { Name = "test" });
            mockCookieManager.Setup(m => m.ExtractSchoolComparisonListFromCookie()).Returns(fakeSchoolComparisonList);

            var mockComparisonService = new Mock<IComparisonService>();

            var controller = new BenchmarkCriteriaController(null, null, null, null, mockCookieManager.Object, mockComparisonService.Object);

            var response = controller.OverwriteStrategyAsync(10000, ComparisonType.Advanced, EstablishmentType.Maintained, new BenchmarkCriteriaVM(new BenchmarkCriteria() { Gender = new[] { "Boys" } }), ComparisonArea.All, 306, "test", 10);

            Assert.IsNotNull(response);
            Assert.IsNotNull((response as ViewResult).Model);
            Assert.AreEqual("", (response as ViewResult).ViewName);
        }

        [Test]
        public void DoNotAskForOverwriteStrategyIfOnlyBenchmarkSchoolInList()
        {
            var mockCookieManager = new Mock<IBenchmarkBasketCookieManager>();
            var fakeSchoolComparisonList = new SchoolComparisonListModel();
            fakeSchoolComparisonList.HomeSchoolUrn = "100";
            fakeSchoolComparisonList.HomeSchoolName = "home school";
            fakeSchoolComparisonList.BenchmarkSchools.Add(new BenchmarkSchoolModel() { Name = "test", Urn = "100" });

            mockCookieManager.Setup(m => m.ExtractSchoolComparisonListFromCookie()).Returns(fakeSchoolComparisonList);

            var _mockDocumentDbService = new Mock<IFinancialDataService>();
            var testResult = new SchoolTrustFinancialDataObject();
            testResult.URN = 321;
            testResult.SchoolName = "test";
            Task<List<SchoolTrustFinancialDataObject>> task = Task.Run(() =>
            {
                return new List<SchoolTrustFinancialDataObject> { testResult };
            });

            _mockDocumentDbService.Setup(m => m.SearchSchoolsByCriteriaAsync(It.IsAny<BenchmarkCriteria>(), It.IsAny<EstablishmentType>(), It.IsAny<bool>()))
                .Returns((BenchmarkCriteria criteria, EstablishmentType estType, bool excludePartial) => task);

            var _mockDataCollectionManager = new Mock<IDataCollectionManager>();
            _mockDataCollectionManager.Setup(m => m.GetLatestFinancialDataYearPerEstabTypeAsync(It.IsAny<EstablishmentType>()))
                .Returns(2015);

            var _mockEdubaseDataService = new Mock<IContextDataService>();
            var testEduResult = new EdubaseDataObject();
            testEduResult.URN = 100;
            testEduResult.EstablishmentName = "test";
            _mockEdubaseDataService.Setup(m => m.GetSchoolDataObjectByUrnAsync(100)).Returns((string urn) => testEduResult);

            var mockComparisonService = new Mock<IComparisonService>();

            var controller = new BenchmarkCriteriaController(null, _mockDocumentDbService.Object, _mockEdubaseDataService.Object, null, mockCookieManager.Object, mockComparisonService.Object);

            var result = controller.OverwriteStrategyAsync(10000, ComparisonType.Advanced, EstablishmentType.Maintained, new BenchmarkCriteriaVM(new BenchmarkCriteria() { Gender = new[] { "Boys" } }), ComparisonArea.All, 306, "test", 10);

            Assert.AreEqual("BenchmarkCharts", (result as RedirectToRouteResult).RouteValues["Controller"]);
            Assert.AreEqual("GenerateNewFromAdvancedCriteria", (result as RedirectToRouteResult).RouteValues["Action"]);
        }

        [Test]
        public void DisplayReplaceViewIfBasketLimitExceed()
        {
            var mockCookieManager = new Mock<IBenchmarkBasketCookieManager>();
            var fakeSchoolComparisonList = new SchoolComparisonListModel();
            fakeSchoolComparisonList.BenchmarkSchools.Add(new BenchmarkSchoolModel() { Name = "test" });
            fakeSchoolComparisonList.BenchmarkSchools.Add(new BenchmarkSchoolModel() { Name = "test" });
            mockCookieManager.Setup(m => m.ExtractSchoolComparisonListFromCookie()).Returns(fakeSchoolComparisonList);

            var mockComparisonService = new Mock<IComparisonService>();

            var controller = new BenchmarkCriteriaController(null, null, null, null, mockCookieManager.Object, mockComparisonService.Object);

            var response = controller.OverwriteStrategyAsync(10000, ComparisonType.Advanced, EstablishmentType.Maintained, new BenchmarkCriteriaVM(new BenchmarkCriteria() { Gender = new[] { "Boys" } }), ComparisonArea.All, 306, "test", 29);

            Assert.IsNotNull(response);
            Assert.AreEqual("OverwriteReplace", (response as ViewResult).ViewName);
        }

        [Test]
        public void SelectSchoolTypeActionClearsBaseSchoolWhenAdvancedComparisonWithoutBaseSchool()
        {
            var mockCookieManager = new Mock<IBenchmarkBasketCookieManager>();

            var _mockDocumentDbService = new Mock<IFinancialDataService>();

            var _mockDataCollectionManager = new Mock<IDataCollectionManager>();

            var _mockEdubaseDataService = new Mock<IContextDataService>();

            var mockComparisonService = new Mock<IComparisonService>();

            var controller = new BenchmarkCriteriaController(null, _mockDocumentDbService.Object, _mockEdubaseDataService.Object, null, mockCookieManager.Object, mockComparisonService.Object);

            var result = controller.SelectSchoolTypeAsync(null, ComparisonType.Advanced, EstablishmentType.Maintained, 15);

            mockCookieManager.Verify(m => m.UpdateSchoolComparisonListCookie(CookieActions.UnsetDefault, null));
        }

        [Test]
        public void AdvancedCharacteristicsShouldReturnErrorIfLaCodeIsNotFound()
        {
            var mockCookieManager = new Mock<IBenchmarkBasketCookieManager>();

            var _mockDocumentDbService = new Mock<IFinancialDataService>();

            var _mockDataCollectionManager = new Mock<IDataCollectionManager>();

            var _mockEdubaseDataService = new Mock<IContextDataService>();

            var mockComparisonService = new Mock<IComparisonService>();

            var mockLaSearchService = new Mock<ILaSearchService>();

            var mockLaService = new Mock<ILocalAuthoritiesService>();

            mockLaSearchService.Setup(m => m.LaCodesContain(123)).Returns(false);

            var controller = new BenchmarkCriteriaController(mockLaService.Object, _mockDocumentDbService.Object, _mockEdubaseDataService.Object, mockLaSearchService.Object, mockCookieManager.Object, mockComparisonService.Object);

            var response = controller.AdvancedCharacteristicsAsync(null, ComparisonType.Advanced, EstablishmentType.All, ComparisonArea.LaCode, 123, "", null);

            Assert.IsNotNull(response);
            Assert.IsNotNull((response as ViewResult).Model);
            Assert.IsTrue(((response as ViewResult).Model as SchoolViewModel).HasError());
            Assert.AreEqual("Please enter a valid Local authority code", ((response as ViewResult).Model as SchoolViewModel).ErrorMessage);
        }

    }
}

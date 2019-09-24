﻿using Newtonsoft.Json;
using SFB.Web.Domain.Services;
using SFB.Web.UI.Helpers;
using SFB.Web.UI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using SFB.Web.Domain.Services.DataAccess;
using SFB.Web.Domain.Services.Search;
using SFB.Web.UI.Helpers.Constants;
using SFB.Web.UI.Services;
using System;
using SFB.Web.UI.Helpers.Enums;
using SFB.Web.UI.Attributes;
using SFB.Web.Common.DataObjects;

namespace SFB.Web.UI.Controllers
{
    [CustomAuthorize]
    public class SchoolSearchController : SearchBaseController
    {
        private readonly ILocalAuthoritiesService _laService;
        private readonly ILaSearchService _laSearchService;
        private readonly ILocationSearchService _locationSearchService;
        private readonly IValidationService _valService;
        private readonly IContextDataService _contextDataService;

        public SchoolSearchController(ILocalAuthoritiesService laService, 
            ILaSearchService laSearchService, ILocationSearchService locationSearchService, IFilterBuilder filterBuilder,
            IValidationService valService, IContextDataService contextDataService,
            ISchoolSearchService schoolSearchService,
            IBenchmarkBasketCookieManager benchmarkBasketCookieManager)
            : base(schoolSearchService, null, benchmarkBasketCookieManager, filterBuilder)
        {
            _laService = laService;
            _laSearchService = laSearchService;
            _locationSearchService = locationSearchService;
            _valService = valService;
            _contextDataService = contextDataService;
        }

        public async Task<ActionResult> Search(
            string nameId,
            string trustNameId,
            string searchType,
            string suggestionUrn,
            string locationorpostcode,
            string locationCoordinates,
            string laCodeName,
            decimal? radius,
            bool openOnly = false,
            string orderby = "", 
            int page = 1,
            string tab = "list",
            string referrer = "home/index")
        {
            dynamic searchResp = null;
            string errorMessage;
            ViewBag.tab = tab;

            var schoolComparisonList = _benchmarkBasketCookieManager.ExtractSchoolComparisonListFromCookie();

            switch (searchType)
            {
                case SearchTypes.SEARCH_BY_NAME_ID:
                    var nameIdSanitized = Regex.Replace(nameId, @"(-|/)", "");
                    if (IsNumeric(nameIdSanitized))
                    {
                        errorMessage = _valService.ValidateSchoolIdParameter(nameIdSanitized);
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            try
                            {
                                if(IsLaEstab(nameId))
                                {
                                    searchResp = _contextDataService.GetSchoolDataObjectByLaEstab(nameIdSanitized, openOnly);
                                    if (searchResp.Count == 0)
                                    {
                                        return View("EmptyResult", new SearchViewModel(schoolComparisonList, SearchTypes.SEARCH_BY_NAME_ID));
                                    }
                                    else if(searchResp.Count == 1)
                                    {                                         
                                        return RedirectToAction("Detail", "School", new { urn = (searchResp as List<EdubaseDataObject>).First().URN });
                                    }
                                    else
                                    {
                                        searchResp = await GetSearchResults(nameId, SearchTypes.SEARCH_BY_LA_ESTAB, null, null, null, radius, openOnly, orderby, page);
                                    }
                                }
                                else
                                {
                                    searchResp = _contextDataService.GetSchoolDataObjectByUrn(Int32.Parse(nameIdSanitized));
                                    return RedirectToAction("Detail", "School", new { urn = searchResp.URN });
                                }
                            }
                            catch(Exception)
                            {
                                return View("EmptyResult", new SearchViewModel(schoolComparisonList, SearchTypes.SEARCH_BY_NAME_ID));
                            }                                                     
                        }
                        else
                        {
                            var searchVM = new SearchViewModel(schoolComparisonList, searchType)
                            {
                                SearchType = searchType,
                                ErrorMessage = errorMessage,
                                Authorities = _laService.GetLocalAuthorities()
                            };

                            return View("../" + referrer, searchVM);
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(_valService.ValidateSchoolIdParameter(suggestionUrn)))
                        {
                            return RedirectToAction("Detail", "School", new {urn = suggestionUrn});
                        }

                        errorMessage = _valService.ValidateNameParameter(nameId);
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            // first see if we get a match on the word
                            searchResp = await GetSearchResults(nameId, searchType, null, null, null, radius, openOnly, orderby, page);
                            if (searchResp.NumberOfResults == 0)
                            {
                                return RedirectToActionPermanent("SuggestSchool", "SchoolSearch",
                                    new RouteValueDictionary {{"nameId", nameId}, { "openOnly", openOnly} });
                            }
                        }
                        else
                        {
                            var searchVM = new SearchViewModel(schoolComparisonList, searchType)
                            {
                                SearchType = searchType,
                                ErrorMessage = errorMessage,
                                Authorities = _laService.GetLocalAuthorities()
                            };

                            return View("../" + referrer, searchVM);
                        }
                    }
                    break;

                case SearchTypes.SEARCH_BY_LA_CODE_NAME:
                    if (!IsNumeric(laCodeName))
                    {
                        errorMessage = _valService.ValidateLaNameParameter(laCodeName);
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            var exactMatch = _laSearchService.SearchExactMatch(laCodeName);
                            if (exactMatch != null)
                            {
                                laCodeName = exactMatch.id;
                                return await Search(nameId, trustNameId, searchType, suggestionUrn, locationorpostcode,
                                    locationCoordinates, laCodeName, radius, openOnly, orderby, page, tab);
                            }
                            TempData["SearchMethod"] = "Random";
                            return RedirectToAction("Search", "La", new {name = laCodeName, openOnly = openOnly});
                        }
                        else
                        {
                            var searchVM = new SearchViewModel(schoolComparisonList, searchType)
                            {
                                SearchType = searchType,
                                ErrorMessage = errorMessage,
                                Authorities = _laService.GetLocalAuthorities()
                            };

                            return View("../" + referrer, searchVM);
                        }
                    }
                    else
                    {
                        errorMessage = _valService.ValidateLaCodeParameter(laCodeName);
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            searchResp = await GetSearchResults(nameId, searchType, locationorpostcode, locationCoordinates, laCodeName, radius, openOnly, orderby, page);

                            int resultCount = searchResp.NumberOfResults;
                            switch (resultCount)
                            {
                                case 0:
                                    return View("EmptyResult",
                                        new SearchViewModel(schoolComparisonList, searchType));
                                case 1:
                                    return RedirectToAction("Detail", "School",
                                        new
                                        {
                                            urn = ((Domain.Models.QueryResultsModel) searchResp).Results.First()["URN"]
                                        });
                            }
                        }
                        else
                        {
                            var searchVM = new SearchViewModel(schoolComparisonList, searchType)
                            {
                                SearchType = searchType,
                                ErrorMessage = errorMessage,
                                Authorities = _laService.GetLocalAuthorities()
                            };

                            return View("../" + referrer, searchVM);
                        }
                    }

                    break;

                case SearchTypes.SEARCH_BY_LOCATION:
                    errorMessage = _valService.ValidateLocationParameter(locationorpostcode);
                    if (string.IsNullOrEmpty(errorMessage))
                    {
                        if (string.IsNullOrEmpty(locationCoordinates))
                        {
                            var result = _locationSearchService.SuggestLocationName(locationorpostcode);
                            switch (result.Matches.Count)
                            {
                                case 0:
                                    return View("EmptyLocationResult",
                                        new SearchViewModel(schoolComparisonList, searchType));
                                default:
                                    TempData["LocationResults"] = result;
                                    TempData["SearchMethod"] = "Random";
                                    return RedirectToAction("Suggest", "Location", new { locationOrPostcode = locationorpostcode, openOnly = openOnly });
                            }                            
                        }
                        else
                        {
                            searchResp = await GetSearchResults(nameId, searchType, locationorpostcode, locationCoordinates, laCodeName, radius, openOnly, orderby, page);

                            int resultCnt = searchResp.NumberOfResults;
                            switch (resultCnt)
                            {
                                case 0:
                                    return View("EmptyLocationResult",
                                        new SearchViewModel(schoolComparisonList, searchType));
                                case 1:
                                    return RedirectToAction("Detail", "School",
                                        new { urn = ((Domain.Models.QueryResultsModel)searchResp).Results.First()["URN"] });
                            }
                        }
                    }
                    else
                    {
                        var searchVM = new SearchViewModel(schoolComparisonList, searchType)
                        {
                            SearchType = searchType,
                            ErrorMessage = errorMessage,
                            Authorities = _laService.GetLocalAuthorities()
                        };

                        return View("../" + referrer, searchVM);
                    }
                    break;
            }

            var laName = _laService.GetLaName(laCodeName);
            return View("SearchResults", GetSchoolViewModelList(searchResp, schoolComparisonList, orderby, page, searchType, nameId, locationorpostcode, laName));
        }

        public ActionResult AddSchools()
        {
            var schoolComparisonListModel = _benchmarkBasketCookieManager.ExtractSchoolComparisonListFromCookie();
            var vm = new SearchViewModel(schoolComparisonListModel, "");
            vm.Authorities = _laService.GetLocalAuthorities();

            return View(vm);
        }

        public PartialViewResult UpdateBenchmarkBasket(int urn, CookieActions withAction)
        {
            var benchmarkSchool = new SchoolViewModel(_contextDataService.GetSchoolDataObjectByUrn(urn), null);

            _benchmarkBasketCookieManager.UpdateSchoolComparisonListCookie(withAction,
                new BenchmarkSchoolModel()
                {
                    Name = benchmarkSchool.Name,
                    Urn = benchmarkSchool.Id.ToString(),
                    Type = benchmarkSchool.Type,
                    EstabType = benchmarkSchool.EstablishmentType.ToString()
                });                      

            return PartialView("Partials/BenchmarkListBanner",
                new SchoolViewModel(null, _benchmarkBasketCookieManager.ExtractSchoolComparisonListFromCookie()));
        }

        public async Task<ActionResult> SuggestSchool(string nameId, bool openOnly = false)
        {
            var vm = new SchoolNotFoundViewModel
            {
                SearchKey = nameId,
                Suggestions = await _schoolSearchService.SuggestSchoolByName(nameId, openOnly)
            };
            return View("NotFound", vm);
        }

        public async Task<ActionResult> Suggest(string nameId, bool openOnly = false)
        {
            string json = null;

            if (!IsNumeric(nameId))
            {
                dynamic response = await _schoolSearchService.SuggestSchoolByName(nameId, openOnly);
                json = JsonConvert.SerializeObject(response);
            }

            return Content(json, "application/json");
        }

        [Route("SchoolSearch/Search-js")]
        public async Task<PartialViewResult> SearchJS(string nameId, string searchType, string suggestionurn,
            string locationorpostcode, string locationCoordinates, string laCodeName, string schoolId, decimal? radius, bool openOnly = false,
            string orderby = "", int page = 1)

        {
            dynamic searchResponse;
            var schoolComparisonList = _benchmarkBasketCookieManager.ExtractSchoolComparisonListFromCookie();

            if (IsLaEstab(nameId))
            {
                searchResponse = await GetSearchResults(nameId, SearchTypes.SEARCH_BY_LA_ESTAB, locationorpostcode, locationCoordinates, laCodeName, radius, openOnly, orderby, page);
            }
            else
            {
                searchResponse = await GetSearchResults(nameId, searchType, locationorpostcode, locationCoordinates, laCodeName, radius, openOnly, orderby, page);
            }
            var vm = GetSchoolViewModelList(searchResponse, schoolComparisonList, orderby,page, searchType, nameId, locationorpostcode, laCodeName);

            return PartialView("Partials/Search/SchoolResults", vm);
        }

        [Route("SchoolSearch/Search-json")]
        public async Task<JsonResult> SearchJson(string nameId, string searchType, string suggestionurn,
            string locationorpostcode, string locationCoordinates, string laCodeName, string schoolId, decimal? radius, 
            int? companyNo, bool openOnly = false, string orderby = "", int page = 1)

        {
            dynamic searchResponse;
            if (!companyNo.HasValue)
            {
                if (IsLaEstab(nameId))
                {
                    searchResponse = await GetSearchResults(nameId, SearchTypes.SEARCH_BY_LA_ESTAB, locationorpostcode,
                        locationCoordinates, laCodeName, radius, openOnly, orderby, page, 1000);
                }
                else
                {
                    searchResponse = await GetSearchResults(nameId, searchType, locationorpostcode,
                        locationCoordinates, laCodeName, radius, openOnly, orderby, page, 1000);
                }
            }
            else
            {
                searchResponse = await _schoolSearchService.SearchSchoolByCompanyNo(companyNo.GetValueOrDefault(),
                    0, SearchDefaults.TRUST_SCHOOLS_PER_PAGE, "", Request.QueryString);
            }

            var results = new List<SchoolSummaryViewModel>();
            foreach (var result in searchResponse.Results)
            {
                var schoolVm = new SchoolSummaryViewModel(result);
                results.Add(schoolVm);
            }

            return Json(new { count = results.Count, results = results }, JsonRequestBehavior.AllowGet);
        }
    }
}
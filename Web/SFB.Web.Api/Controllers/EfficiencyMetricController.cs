﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SFB.Web.ApplicationCore.Entities;
using SFB.Web.ApplicationCore.Models;
using SFB.Web.ApplicationCore.Services.DataAccess;

namespace SFB.Web.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EfficiencyMetricController : ControllerBase
    {
        private readonly IEfficiencyMetricDataService _efficiencyMetricDataService;
        private readonly IContextDataService _contextDataService;

        private readonly ILogger _logger;

        public EfficiencyMetricController(IContextDataService contextDataService, 
            IEfficiencyMetricDataService efficiencyMetricDataService,
            ILogger<EfficiencyMetricController> logger)
        {
            _contextDataService = contextDataService;
            _efficiencyMetricDataService = efficiencyMetricDataService;

            _logger = logger;

        }

        // GET api/efficiencymetric/138082
        [HttpGet("{urn}")]
        public async Task<ActionResult<EfficiencyMetricModel>> GetAsync(int urn)
        {            
            EfficiencyMetricDataObject defaultSchoolEMData = null;

            try
            {
                defaultSchoolEMData = await _efficiencyMetricDataService.GetSchoolDataObjectByUrnAsync(urn);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            if (defaultSchoolEMData == null)
            {
                _logger.LogWarning("No school found with URN: {urn}", urn);
                return NoContent();
            }
            else
            {
                return await BuildModelWithEMNeighbours(defaultSchoolEMData);
            }
        }

        private async Task<EfficiencyMetricModel> BuildModelWithEMNeighbours(EfficiencyMetricDataObject defaultSchoolEMData)
        {
            var neighbourContextDataList = await _contextDataService.GetMultipleSchoolDataObjectsByUrnsAsync(defaultSchoolEMData.NeighbourRecords.Select(n => n.URN).ToList());
            var neighbourEMDataList = await _efficiencyMetricDataService.GetMultipleSchoolDataObjectsByUrnsAsync(defaultSchoolEMData.NeighbourRecords.Select(n => n.URN).ToList());

            var neighbourDataModels = new List<EfficiencyMetricNeighbourModel>();
            foreach (var neighbourRecord in defaultSchoolEMData.NeighbourRecords)
            {
                neighbourDataModels.Add(new EfficiencyMetricNeighbourModel(
                    neighbourRecord.URN,
                    neighbourRecord.Rank,
                    neighbourContextDataList.Find(cd => cd.URN == neighbourRecord.URN),
                    neighbourEMDataList.Find(em => em.Urn == neighbourRecord.URN)
                    ));
            }

            return new EfficiencyMetricModel(defaultSchoolEMData.Urn, defaultSchoolEMData.Efficiencydecileingroup, defaultSchoolEMData.Name, neighbourDataModels);            
        }
    }
}
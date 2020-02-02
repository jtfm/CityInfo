using CityInfo.API.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace CityInfo.API.Controllers
{
    [ApiController]
    [Route("api/cities/{cityId}/pointsofinterest")]
    public class PointsOfInterestController : ControllerBase
    {
        private readonly ILogger<PointsOfInterestController> _logger;

        public PointsOfInterestController(ILogger<PointsOfInterestController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        [HttpGet]
        public IActionResult GetPointsOfInterest(int cityId)
        {
            try
            {
                throw new Exception("Exception example");
                
                CityDto city = GetCityDto(cityId);
                if (!CityIsAvailable(city))
                {
                    _logger.LogInformation($"City with name \"{city.Name}\" wasn't found when accessing points of interest.");
                    return NotFound();
                }
                return Ok(city.PointsOfInterest);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Exception while getting points of interest for city with id {cityId}");
                return StatusCode(500, "A problem occurred while handling your request.");
            }
        }

        [HttpGet("{id}", Name = "GetPointOfInterest")]
        public IActionResult GetPointOfInterest(int cityId, int id)
        {
            CityDto city = GetCityDto(cityId);
            if (!CityIsAvailable(city)) return NotFound();

            var pointOfInterest = city.PointsOfInterest
                .FirstOrDefault(c => c.Id == id);

            if (pointOfInterest == null)
            {
                return NotFound();
            }

            return Ok(pointOfInterest);
        }

        [HttpPost]
        public IActionResult CreatePointOfInterest(int cityId,
            [FromBody] PointOfInterestForCreationDto pointOfInterest)
        {
            if (pointOfInterest.Description == pointOfInterest.Name)
            {
                ModelState.AddModelError(
                    "Description",
                    "The provided description should be different from the name.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            CityDto city = GetCityDto(cityId);
            if (!CityIsAvailable(city))
            {
                _logger.LogInformation($"City with name \"{city.Name}\" wasn't found when accessing points of interest.");
                return NotFound();
            }

            var maxPointOfInterestId = CitiesDataStore.Current.Cities
                .SelectMany(c => c.PointsOfInterest).Max(p => p.Id);

            var finalPointOfInterest = new PointOfInterestDto()
            {
                Id = ++maxPointOfInterestId,
                Name = pointOfInterest.Name,
                Description = pointOfInterest.Description
            };

            city.PointsOfInterest.Add(finalPointOfInterest);

            return CreatedAtRoute("GetPointOfInterest",
                new { cityId, id = finalPointOfInterest.Id },
                finalPointOfInterest);
        }

        [HttpPut("{id}")]
        public IActionResult UpdatePointOfInterest(int cityId, int id,
            [FromBody] PointOfInterestForUpdateDto pointOfInterest)
        {
            if (pointOfInterest.Description == pointOfInterest.Name)
            {
                ModelState.AddModelError(
                    "Description",
                    "The provided description should be different from the name.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            CityDto city = GetCityDto(cityId);
            if (!CityIsAvailable(city))
            {
                _logger.LogInformation($"City with name \"{city.Name}\" wasn't found when accessing points of interest.");
                return NotFound();
            }
            PointOfInterestDto pointOfInterestFromStore = GetPointOfInterestDto(id, city);

            pointOfInterestFromStore.Name = pointOfInterest.Name;
            pointOfInterestFromStore.Description = pointOfInterest.Description;

            return NoContent();
        }

        // Currently not usable
        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdatePointOfInterest(int cityId, int id,
            [FromBody] JsonPatchDocument<PointOfInterestForUpdateDto> patchDoc)
        {
            CityDto city = GetCityDto(cityId);
            if (!CityIsAvailable(city))
            {
                _logger.LogInformation($"City with name \"{city.Name}\" wasn't found when accessing points of interest.");
                return NotFound();
            }
            PointOfInterestDto pointOfInterestFromStore = GetPointOfInterestDto(id, city);
            if (!PointOfInterestIsAvailable(pointOfInterestFromStore)) return NotFound();

            var pointOfInterestToPatch =
                new PointOfInterestForUpdateDto()
                {
                    Name = pointOfInterestFromStore.Name,
                    Description = pointOfInterestFromStore.Description
                };

            patchDoc.ApplyTo(pointOfInterestToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (pointOfInterestToPatch.Description == pointOfInterestToPatch.Name)
            {
                ModelState.AddModelError("Description", "The provided description should be different from the name.");
            }

            if (!TryValidateModel(pointOfInterestToPatch))
            {
                return BadRequest(ModelState);
            }

            pointOfInterestFromStore.Name = pointOfInterestToPatch.Name;
            pointOfInterestFromStore.Description = pointOfInterestToPatch.Description;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeletePointOfInterest(int cityId, int id)
        {
            var city = GetCityDto(cityId);
            if (!CityIsAvailable(city))
            {
                _logger.LogInformation($"City with name \"{city.Name}\" wasn't found when accessing points of interest.");
                return NotFound();
            }

            var pointsOfInterestFromStore = GetPointOfInterestDto(id, city);
            if (!PointOfInterestIsAvailable(pointsOfInterestFromStore))
            {
                return NotFound();
            }

            city.PointsOfInterest.Remove(pointsOfInterestFromStore);

            return NoContent();
        }

        private CityDto GetCityDto(int cityId)
        {
            return CitiesDataStore.Current.Cities
                .FirstOrDefault(c => c.Id == cityId);
        }

        private bool CityIsAvailable(
            CityDto city)
        {
            return city != null;
        }

        private PointOfInterestDto GetPointOfInterestDto(
            int id, CityDto city)
        {
            return city.PointsOfInterest
                .FirstOrDefault(p => p.Id == id);
        }

        private bool PointOfInterestIsAvailable(
            PointOfInterestDto pointOfInterestFromStore)
        {
            return pointOfInterestFromStore != null;
        }
    }
}

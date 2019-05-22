using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using QuizRequestExtendedService;
using QuizRequestExtendedService.DTO;

namespace QuizLevelManager.Controllers
{
    [Route("proxy")]
    [ApiController]
    public class ProxyController : Controller
    {
        private readonly IQuizServiceExtended service;

        public ProxyController(IQuizServiceExtended service)
        {
            this.service = service;
        }

        [HttpGet("topics")]
        public ActionResult<IEnumerable<TopicDTO>> GetTopics()
        {
            return Ok(service.GetTopics());
        }
        [HttpGet("{topicId}/levels")]
        public ActionResult<IEnumerable<LevelDTO>> GetLevels(Guid topicId)
        {
            return Ok(service.GetLevels(topicId));
        }
        [HttpGet("{topicId}/{levelId}/templateGenerators")]
        public ActionResult<IEnumerable<AdminTemplateGeneratorDTO>> GetTemplateGenerators(Guid topicId, Guid levelId)
        {
            return Ok(service.GetTemplateGenerators(topicId, levelId));
        }
    }
}
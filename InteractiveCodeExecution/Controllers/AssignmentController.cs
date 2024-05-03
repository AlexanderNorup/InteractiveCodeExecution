using InteractiveCodeExecution.ExecutorEntities;
using Microsoft.AspNetCore.Mvc;

namespace InteractiveCodeExecution.Controllers
{
    [ApiController]
    [Route("api/assignments")]
    public class AssignmentController : ControllerBase
    {
        private IExecutorAssignmentProvider _assignmentProvider;
        public AssignmentController(IExecutorAssignmentProvider assignmentProvider)
        {
            _assignmentProvider = assignmentProvider ?? throw new ArgumentNullException(nameof(assignmentProvider));
        }

        [HttpGet]
        public List<AssignmentWithoutMetadata> GetAssignments()
        {
            var namesAndIds = _assignmentProvider.GetAllAssignments()
                .Where(assignment => !string.IsNullOrEmpty(assignment.AssignmentId))
                .Select(assignment => new AssignmentWithoutMetadata(assignment.AssignmentId ?? "", assignment.AssignmentName));

            return new(namesAndIds);
        }

        [HttpGet("{id}")]
        public ActionResult<ExecutorAssignment> GetAssignment(string id)
        {
            if (!_assignmentProvider.TryGetAssignment(id, out var assignment)
                || assignment is null)
            {
                return NotFound();
            }

            return Ok(assignment);
        }

        public record AssignmentWithoutMetadata(string Id, string? Name);
    }
}

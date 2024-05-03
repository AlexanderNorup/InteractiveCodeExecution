using InteractiveCodeExecution.ExecutorEntities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace InteractiveCodeExecution.Controllers
{
    [ApiController]
    [Route("api/assignments")]
    public class AssignmentController : ControllerBase
    {
        private IExecutorAssignmentProvider _assignmentProvider;
        private IExecutorAssignmentSubmissionHandler _submissionHandler;
        public AssignmentController(IExecutorAssignmentProvider assignmentProvider, IExecutorAssignmentSubmissionHandler submissionHandler)
        {
            _assignmentProvider = assignmentProvider ?? throw new ArgumentNullException(nameof(assignmentProvider));
            _submissionHandler = submissionHandler ?? throw new ArgumentNullException(nameof(submissionHandler));
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

        [HttpPost("submit")]
        public async Task<ActionResult<ExecutorAssignment>> SubmitAssignment(PoCAssignmentSubmission submission)
        {
            try
            {
                await _submissionHandler.SubmitAssignmentAsync(submission.Payload, submission.UserId, HttpContext.RequestAborted).ConfigureAwait(false);
            }
            catch (ExecutorPayloadSubmissionException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok();
        }

        // THIS METHOD SHOULD HAVE AUTHORIZATION TO MAKE SURE ONLY ADMINS/TEACHERS CAN DO THIS
        [HttpGet("submissions/{assignmentId}")]
        public async Task<ActionResult<ExecutorAssignment>> GetAllSubmissions(string assignmentId)
        {
            try
            {
                var assignments = await _submissionHandler.GetAllSubmissionsForAssignmentAsync(assignmentId, HttpContext.RequestAborted).ConfigureAwait(false);
                return Ok(assignments);
            }
            catch (ExecutorPayloadSubmissionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // THIS METHOD SHOULD HAVE AUTHORIZATION TO MAKE SURE ONLY ADMINS/TEACHERS CAN DO THIS
        [HttpGet("submissions/{assignmentId}/{userId}")]
        public async Task<ActionResult> GetSingleSubmissionTarBall(string assignmentId, string userId)
        {
            try
            {
                var assignments = _submissionHandler.GetSubmissionTarBall(assignmentId, userId, HttpContext.RequestAborted);
                return new FileStreamResult(assignments, new MediaTypeHeaderValue("application/x-tar"))
                {
                    FileDownloadName = $"{userId}_{assignmentId}.tar"
                };
            }
            catch (ExecutorPayloadSubmissionException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // This is a temporary for the PoC. The UserId should not be user-controller, it should come from authentication
        public record PoCAssignmentSubmission(string UserId, ExecutorPayload Payload);
        public record AssignmentWithoutMetadata(string Id, string? Name);
    }
}

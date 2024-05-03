using InteractiveCodeExecution.ExecutorEntities;

namespace InteractiveCodeExecution.Services
{
    public class PoCAssignmentSubmissionHandler : IExecutorAssignmentSubmissionHandler
    {
        const string FileExtention = ".tar";
        private static readonly string PocHandInLocation = Path.Combine(AppContext.BaseDirectory, "Hand-Ins");

        public async Task SubmitAssignmentAsync(ExecutorPayload payload, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(payload.AssignmentId))
            {
                throw new ExecutorPayloadSubmissionException("This payload cannot be handed in, because the assignment id is null");
            }

            if (payload.Files is null || !payload.Files.Any())
            {
                throw new ExecutorPayloadSubmissionException("This payload cannot be handed in, because there are no files to save");
            }

            // We assume the userId does not have path traversal in it. That would be pretty horrible if it did.
            var assignmentFolder = Path.Combine(PocHandInLocation, payload.AssignmentId);
            var assignmentTarFile = Path.Combine(assignmentFolder, userId + FileExtention);

            if (File.Exists(assignmentTarFile))
            {
                const bool AllowReHandIn = false; // Let's just say this field is dynamic ;) 

                if (!AllowReHandIn)
                {
                    throw new ExecutorPayloadSubmissionException("You have already handed in something. You cannot redo it!");
                }

                File.Delete(assignmentTarFile); // This is where we hope userId does not have path traversal :))
            }

            if (!Directory.Exists(assignmentFolder))
            {
                Directory.CreateDirectory(assignmentFolder);
            }

            using (var tarBall = File.Open(assignmentTarFile, FileMode.OpenOrCreate))
            {
                await PayloadUtils.WritePayloadToTarball(payload, tarBall, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<IEnumerable<string>> GetAllSubmissionsForAssignmentAsync(string assignmentId, CancellationToken cancellationToken = default)
        {
            var assignmentDirectory = Path.Combine(PocHandInLocation, assignmentId);
            if (!Directory.Exists(assignmentDirectory))
            {
                throw new ExecutorPayloadSubmissionException("This assignment does not exist (or no submissions handed in yet. Either or..)!");
            }

            return Task.FromResult(Directory.EnumerateFiles(assignmentDirectory)
                .Select(path => new FileInfo(path))
                .Where(file => file.Extension == FileExtention)
                .Select(file => file.Name.Remove(file.Name.Length - FileExtention.Length, FileExtention.Length)));
        }

        public FileStream GetSubmissionTarBall(string assignmentId, string userId, CancellationToken cancellationToken = default)
        {
            var file = Path.Combine(PocHandInLocation, assignmentId, userId + FileExtention);
            if (!File.Exists(file))
            {
                throw new ExecutorPayloadSubmissionException("This hand-in does not exist!");
            }

            return File.OpenRead(file);
        }


    }
}
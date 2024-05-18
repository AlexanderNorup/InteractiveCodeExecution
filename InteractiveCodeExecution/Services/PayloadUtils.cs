using InteractiveCodeExecution.ExecutorEntities;
using System.Formats.Tar;
using System.Text;

namespace InteractiveCodeExecution.Services
{
    public static class PayloadUtils
    {
        public static long CalculatePayloadSizeInBytes(ExecutorPayload payload)
        {
            long totalSize = 0;
            if (payload.Files is null || !payload.Files.Any())
            {
                return totalSize;
            }

            foreach (var file in payload.Files)
            {
                totalSize += file.ContentType switch
                {
                    ExecutorFileType.Base64BinaryFile => (3 * file.Content.Length) / 4, // 3 bytes per 4 characters in base64
                    _ => (long)Encoding.UTF8.GetMaxByteCount(file.Content.Length),
                };
            }

            return totalSize;
        }

        public static async Task WritePayloadToTarball(ExecutorPayload payload, Stream tarBall, CancellationToken cancellationToken)
        {
            if (payload.Files is null || !payload.Files.Any())
            {
                throw new Exception("Payload does not contain any files!");
            }

            var tarWriter = new TarWriter(tarBall);

            foreach (var file in payload.Files)
            {
                using var dataStream = new MemoryStream(GetFileContentAsByteArray(file));

                var tarEntry = new GnuTarEntry(TarEntryType.RegularFile, file.Filepath)
                {
                    DataStream = dataStream
                };

                await tarWriter.WriteEntryAsync(tarEntry, cancellationToken).ConfigureAwait(false);
            }

            tarBall.Seek(0, SeekOrigin.Begin);
        }

        public static byte[] GetFileContentAsByteArray(ExecutorFile file) => file.ContentType switch
        {
            ExecutorFileType.Base64BinaryFile => Convert.FromBase64String(file.Content),
            _ => Encoding.UTF8.GetBytes(file.Content),
        };
    }
}

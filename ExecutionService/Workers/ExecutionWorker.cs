using Docker.DotNet;
using Docker.DotNet.Models;
using ExecutionService.Data;
using ExecutionService.Hubs;
using ExecutionService.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ExecutionService.Workers
{
    public class ExecutionWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExecutionWorker> _logger;
        // ADD — SignalR hub context for streaming output
        private readonly IHubContext<ExecutionHub> _hubContext;

        private const int MaxExecutionSeconds = 10;
        private const long MaxMemoryBytes = 256 * 1024 * 1024;

        public ExecutionWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<ExecutionWorker> logger,
            IHubContext<ExecutionHub> hubContext) // ADD
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hubContext = hubContext; // ADD
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation("Execution Worker started!");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessQueuedJobs(stoppingToken);
                await Task.Delay(2000, stoppingToken);
            }
        }

        private async Task ProcessQueuedJobs(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ExecutionDbContext>();

            var job = await context.ExecutionJobs
                .FirstOrDefaultAsync(j => j.Status == "QUEUED", token);

            if (job == null) return;

            await ExecuteJob(job, context, token);
        }

        private async Task ExecuteJob(
            ExecutionJob job,
            ExecutionDbContext context,
            CancellationToken token)
        {
            var startTime = DateTime.UtcNow;
            var jobId = job.JobId.ToString();

            try
            {
                job.Status = "RUNNING";
                await context.SaveChangesAsync(token);

                // ADD — notify frontend job has started
                await _hubContext.Clients
                    .Group(jobId)
                    .SendAsync("JobStarted", jobId, token);

                _logger.LogInformation(
                    "Executing job {JobId} in Docker container", job.JobId);

                var dockerClient = new DockerClientConfiguration()
                    .CreateClient();

                var language = await context.SupportedLanguages
                    .FirstOrDefaultAsync(
                        l => l.Name.ToLower() == job.Language.ToLower(),
                        token);

                if (language == null)
                {
                    job.Status = "FAILED";
                    job.Stderr = "Unsupported language!";
                    job.CompletedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync(token);

                    // ADD — stream error to frontend
                    await _hubContext.Clients
                        .Group(jobId)
                        .SendAsync("JobFailed", jobId,
                            "Unsupported language!", token);
                    return;
                }

                var tempDir = Path.Combine(
                    Path.GetTempPath(), job.JobId.ToString());
                Directory.CreateDirectory(tempDir);
                var sourceFile = Path.Combine(
                    tempDir, $"main{language.FileExtension}");
                await File.WriteAllTextAsync(
                    sourceFile, job.SourceCode, token);

                var container = await dockerClient.Containers
                    .CreateContainerAsync(new CreateContainerParameters
                    {
                        Image = language.DockerImage,
                        Cmd = GetRunCommand(
                            language.Name, language.FileExtension),
                        NetworkDisabled = true,
                        HostConfig = new HostConfig
                        {
                            Memory = MaxMemoryBytes,
                            ReadonlyRootfs = true,
                            Binds = new List<string>
                            {
                                $"{tempDir}:/app:ro",
                                "/tmp:/tmp:rw"
                            }
                        }
                    }, token);

                await dockerClient.Containers
                    .StartContainerAsync(container.ID,
                        new ContainerStartParameters(), token);

                // ADD — stream stdout in real time while container runs
                var stdoutBuilder = new System.Text.StringBuilder();
                var stderrBuilder = new System.Text.StringBuilder();

                using var timeoutToken = new CancellationTokenSource(
                    TimeSpan.FromSeconds(MaxExecutionSeconds));

                // Stream logs as they come in
                var logs = await dockerClient.Containers
                    .GetContainerLogsAsync(container.ID, false,
                        new ContainerLogsParameters
                        {
                            ShowStdout = true,
                            ShowStderr = true,
                            Follow = true  // ADD — follow=true streams in real time
                        }, timeoutToken.Token);

                var buffer = new byte[4096];

                while (true)
                {
                    var result = await logs.ReadOutputAsync(
                        buffer, 0, buffer.Length, timeoutToken.Token);

                    if (result.EOF) break;

                    var text = System.Text.Encoding.UTF8
                        .GetString(buffer, 0, result.Count);

                    if (result.Target ==
                        MultiplexedStream.TargetStream.StandardOut)
                    {
                        stdoutBuilder.Append(text);

                        // ADD — stream stdout chunk to frontend in real time
                        await _hubContext.Clients
                            .Group(jobId)
                            .SendAsync("StdoutChunk", jobId, text, token);
                    }
                    else
                    {
                        stderrBuilder.Append(text);

                        // ADD — stream stderr chunk to frontend in real time
                        await _hubContext.Clients
                            .Group(jobId)
                            .SendAsync("StderrChunk", jobId, text, token);
                    }
                }

                var waitResult = await dockerClient.Containers
                    .WaitContainerAsync(container.ID, token);

                var stdout = stdoutBuilder.ToString();
                var stderr = stderrBuilder.ToString();

                job.Status = "COMPLETED";
                job.Stdout = stdout;
                job.Stderr = stderr;
                job.ExitCode = (int)waitResult.StatusCode;
                job.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime)
                    .TotalMilliseconds;
                job.CompletedAt = DateTime.UtcNow;

                await dockerClient.Containers
                    .RemoveContainerAsync(container.ID,
                        new ContainerRemoveParameters { Force = true }, token);

                Directory.Delete(tempDir, true);

                // ADD — notify frontend job completed with final result
                await _hubContext.Clients
                    .Group(jobId)
                    .SendAsync("JobCompleted", jobId, stdout, stderr,
                        job.ExitCode, token);
            }
            catch (OperationCanceledException)
            {
                job.Status = "TIMED_OUT";
                job.Stderr = "Execution exceeded 10 second time limit!";
                job.CompletedAt = DateTime.UtcNow;

                // ADD — notify frontend of timeout
                await _hubContext.Clients
                    .Group(jobId)
                    .SendAsync("JobTimedOut", jobId, token);
            }
            catch (Exception ex)
            {
                job.Status = "FAILED";
                job.Stderr = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Job {JobId} failed", job.JobId);

                // ADD — notify frontend of failure
                await _hubContext.Clients
                    .Group(jobId)
                    .SendAsync("JobFailed", jobId, ex.Message, token);
            }

            await context.SaveChangesAsync(token);
        }

        private static List<string> GetRunCommand(
            string language, string extension)
        {
            return language.ToLower() switch
            {
                "python" => new List<string>
                    { "python", "/app/main.py" },
                "javascript" => new List<string>
                    { "node", "/app/main.js" },
                "java" => new List<string>
                    { "sh", "-c",
                        "cd /app && javac main.java && java main" },
                "c" => new List<string>
                    { "sh", "-c",
                        "cd /app && gcc main.c -o main && ./main" },
                "c++" => new List<string>
                    { "sh", "-c",
                        "cd /app && g++ main.cpp -o main && ./main" },
                "csharp" => new List<string>
                    { "sh", "-c",
                        "cd /tmp && dotnet-script /app/main.cs" },
                "go" => new List<string>
                    { "sh", "-c", "cd /app && go run main.go" },
                "rust" => new List<string>
                    { "sh", "-c",
                        "cd /app && rustc main.rs && ./main" },
                "php" => new List<string>
                    { "php", "/app/main.php" },
                "ruby" => new List<string>
                    { "ruby", "/app/main.rb" },
                _ => new List<string>
                    { "sh", "-c", "echo Unsupported language" }
            };
        }
    }
}
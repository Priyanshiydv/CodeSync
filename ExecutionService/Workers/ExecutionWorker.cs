using Docker.DotNet;
using Docker.DotNet.Models;
using ExecutionService.Data;
using ExecutionService.Models;
using Microsoft.EntityFrameworkCore;

namespace ExecutionService.Workers
{
    /// <summary>
    /// Background worker that processes queued execution jobs.
    /// Runs as IHostedService - picks up QUEUED jobs and executes
    /// them in isolated Docker containers with resource limits.
    /// </summary>
    public class ExecutionWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExecutionWorker> _logger;

        // Sandbox resource limits from PDF requirements
        private const int MaxExecutionSeconds = 10;
        private const long MaxMemoryBytes = 256 * 1024 * 1024; // 256MB

        public ExecutionWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<ExecutionWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation("Execution Worker started!");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessQueuedJobs(stoppingToken);
                // Poll every 2 seconds for new jobs
                await Task.Delay(2000, stoppingToken);
            }
        }

        private async Task ProcessQueuedJobs(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ExecutionDbContext>();

            // Pick up next queued job
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

            try
            {
                // Update status to RUNNING
                job.Status = "RUNNING";
                await context.SaveChangesAsync(token);

                _logger.LogInformation(
                    "Executing job {JobId} in Docker container", job.JobId);

                // Connect to Docker daemon
                var dockerClient = new DockerClientConfiguration()
                    .CreateClient();

                // Get language config
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
                    return;
                }

                // Write source code to temp file
                var tempDir = Path.Combine(Path.GetTempPath(), job.JobId.ToString());
                Directory.CreateDirectory(tempDir);
                var sourceFile = Path.Combine(
                    tempDir, $"main{language.FileExtension}");
                await File.WriteAllTextAsync(sourceFile, job.SourceCode, token);

                // Create container with resource limits
                var container = await dockerClient.Containers
                    .CreateContainerAsync(new CreateContainerParameters
                    {
                        Image = language.DockerImage,
                        Cmd = GetRunCommand(language.Name, language.FileExtension),
                        // No network access in sandbox
                        NetworkDisabled = true,
                        HostConfig = new HostConfig
                        {
                            // Max 256MB memory
                            Memory = MaxMemoryBytes,
                            // No filesystem write outside /tmp
                            ReadonlyRootfs = true,
                            Binds = new List<string>
                            {
                                $"{tempDir}:/app:ro",
                                "/tmp:/tmp:rw"
                            }
                        }
                    }, token);

                // Start container
                await dockerClient.Containers
                    .StartContainerAsync(container.ID,
                        new ContainerStartParameters(), token);

                // Wait for completion with timeout
                using var timeoutToken = new CancellationTokenSource(
                    TimeSpan.FromSeconds(MaxExecutionSeconds));

                var waitResult = await dockerClient.Containers
                    .WaitContainerAsync(container.ID, timeoutToken.Token);
                
                // Get output logs using MultiplexedStream
                var logs = await dockerClient.Containers
                    .GetContainerLogsAsync(container.ID, false,
                        new ContainerLogsParameters
                        {
                            ShowStdout = true,
                            ShowStderr = true
                        }, token);

                // MultiplexedStream separates stdout and stderr
                var stdoutBuilder = new System.Text.StringBuilder();
                var stderrBuilder = new System.Text.StringBuilder();
                var buffer = new byte[4096];

                while (true)
                {
                    var result = await logs.ReadOutputAsync(
                        buffer, 0, buffer.Length, token);

                    if (result.EOF) break;

                    var text = System.Text.Encoding.UTF8
                        .GetString(buffer, 0, result.Count);

                    // MultiplexedStream tells us which stream this chunk is from
                    if (result.Target == MultiplexedStream.TargetStream.StandardOut)
                        stdoutBuilder.Append(text);
                    else
                        stderrBuilder.Append(text);
                }

                var stdout = stdoutBuilder.ToString();
                var stderr = stderrBuilder.ToString();

                
                // Update job with results
                job.Status = "COMPLETED";
                job.Stdout = stdout;
                job.Stderr = stderr;
                job.ExitCode = (int)waitResult.StatusCode;
                job.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime)
                    .TotalMilliseconds;
                job.CompletedAt = DateTime.UtcNow;

                // Cleanup container
                await dockerClient.Containers
                    .RemoveContainerAsync(container.ID,
                        new ContainerRemoveParameters { Force = true }, token);

                // Cleanup temp files
                Directory.Delete(tempDir, true);
            }
            catch (OperationCanceledException)
            {
                // Job exceeded max execution time
                job.Status = "TIMED_OUT";
                job.Stderr = "Execution exceeded 10 second time limit!";
                job.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                job.Status = "FAILED";
                job.Stderr = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Job {JobId} failed", job.JobId);
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
                    { "sh", "-c", "cd /app && javac main.java && java main" },
                "c" => new List<string>
                    { "sh", "-c", "cd /app && gcc main.c -o main && ./main" },
                "c++" => new List<string>
                    { "sh", "-c", "cd /app && g++ main.cpp -o main && ./main" },
                "csharp" => new List<string>
                    { "sh", "-c", "cd /tmp && dotnet-script /app/main.cs" },   
                "go" => new List<string>
                    { "sh", "-c", "cd /app && go run main.go" },
                "rust" => new List<string>
                    { "sh", "-c", "cd /app && rustc main.rs && ./main" },
                "php" => new List<string>
                    { "php", "/app/main.php" },
                "ruby" => new List<string>
                    { "ruby", "/app/main.rb" },
                _ => new List<string> { "sh", "-c", "echo Unsupported language" }
            };
        }
    }
}
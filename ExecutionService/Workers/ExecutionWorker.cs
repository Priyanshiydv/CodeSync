using System.Text;
using System.Text.RegularExpressions;
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
        private readonly IHubContext<ExecutionHub> _hubContext;

        private const int MaxExecutionSeconds = 30;
        private const long MaxMemoryBytes = 256 * 1024 * 1024;

        private readonly HashSet<string> _supportedLanguages = new()
        {
            "python", "java", "csharp", "c", "c++"
        };

        public ExecutionWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<ExecutionWorker> logger,
            IHubContext<ExecutionHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Execution Worker started! " +
                "Supported: Python, Java, C#, C, C++");

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
            var startTime = DateTime.Now;
            var jobId = job.JobId.ToString();

            try
            {
                job.Status = "RUNNING";
                await context.SaveChangesAsync(token);

                // Notify frontend job has started
                await _hubContext.Clients
                    .Group(jobId)
                    .SendAsync("JobStarted", jobId, token);

                if (!_supportedLanguages.Contains(
                    job.Language.ToLower()))
                {
                    job.Status = "FAILED";
                    job.Stderr =
                        $"Language '{job.Language}' not supported";
                    job.CompletedAt = DateTime.Now;
                    await context.SaveChangesAsync(token);
                    await _hubContext.Clients
                        .Group(jobId)
                        .SendAsync("JobFailed", jobId,
                            "Unsupported language!", token);
                    return;
                }

                _logger.LogInformation(
                    "Executing {Language} job {JobId}",
                    job.Language, job.JobId);

                var dockerClient =
                    new DockerClientConfiguration().CreateClient();
                var tempDir = Path.Combine(
                    Path.GetTempPath(), job.JobId.ToString());
                Directory.CreateDirectory(tempDir);

                string workingDir = "/app";
                List<string> runCommand;
                string fileName = GetDefaultFileName(job.Language);

                // Write stdin file if provided
                // used by all languages via shell redirection
                string stdinArg = "";
                if (!string.IsNullOrEmpty(job.Stdin))
                {
                    var stdinFile = Path.Combine(tempDir, "stdin.txt");
                    await File.WriteAllTextAsync(
                        stdinFile, job.Stdin, token);
                    stdinArg = " < /app/stdin.txt";
                }

                switch (job.Language.ToLower())
                {
                    case "python":
                        var pyFile = Path.Combine(
                            tempDir, $"{fileName}.py");
                        await File.WriteAllTextAsync(
                            pyFile, job.SourceCode, token);

                        runCommand = new List<string>
                        {
                            "sh", "-c",
                            $"python -u /app/{fileName}.py{stdinArg}"
                        };
                        break;

                    case "java":
                        string className =
                            ExtractJavaClassName(job.SourceCode);
                        var javaFile = Path.Combine(
                            tempDir, $"{className}.java");
                        await File.WriteAllTextAsync(
                            javaFile, job.SourceCode, token);

                        runCommand = new List<string>
                        {
                            "sh", "-c",
                            $"cd /app && javac {className}.java " +
                            $"&& java {className}{stdinArg}"
                        };
                        break;

                    case "csharp":
                    var csDir = Path.Combine(tempDir, "CSharpApp");
                    Directory.CreateDirectory(csDir);

                    await File.WriteAllTextAsync(
                        Path.Combine(csDir, "Program.cs"),
                        job.SourceCode, token);

                    var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
                <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <!-- Suppress all warnings for clean output -->
                    <NoWarn>CS8600;CS8601;CS8602;CS8603;CS8604;CS8605;CS8618;CS8625</NoWarn>
                </PropertyGroup>
                </Project>";
                    await File.WriteAllTextAsync(
                        Path.Combine(csDir, "CSharpApp.csproj"),
                        csproj, token);

                    workingDir = "/app/CSharpApp";

                    // FIX — /clp:NoSummary suppresses build output
                    // only program output goes to stdout
                    runCommand = new List<string>
                    {
                        "sh", "-c",
                        // Build first silently, then run separately
                        // 2>/dev/null redirects all build noise to null
                        $"dotnet build -v q --nologo 2>/dev/null " +
                        $"&& dotnet run --no-build --nologo{stdinArg}"
                    };
                    break;

                    case "c":
                        var cFile = Path.Combine(
                            tempDir, $"{fileName}.c");
                        await File.WriteAllTextAsync(
                            cFile, job.SourceCode, token);

                        runCommand = new List<string>
                        {
                            "sh", "-c",
                            $"gcc /app/{fileName}.c -o /app/{fileName}" +
                            $" && /app/{fileName}{stdinArg}"
                        };
                        break;

                    case "c++":
                        var cppFile = Path.Combine(
                            tempDir, $"{fileName}.cpp");
                        await File.WriteAllTextAsync(
                            cppFile, job.SourceCode, token);

                        runCommand = new List<string>
                        {
                            "sh", "-c",
                            $"g++ /app/{fileName}.cpp -o /app/{fileName}" +
                            $" && /app/{fileName}{stdinArg}"
                        };
                        break;

                    default:
                        runCommand = new List<string>
                        {
                            "echo", "Unsupported language"
                        };
                        break;
                }

                var container = await dockerClient.Containers
                    .CreateContainerAsync(
                        new CreateContainerParameters
                        {
                            Image = GetDockerImage(job.Language),
                            Cmd = runCommand,
                            WorkingDir = workingDir,
                            NetworkDisabled = true,
                            HostConfig = new HostConfig
                            {
                                Memory = MaxMemoryBytes,
                                ReadonlyRootfs = false,
                                Binds = new List<string>
                                {
                                    $"{tempDir}:/app:rw"
                                }
                            }
                        }, token);

                await dockerClient.Containers
                    .StartContainerAsync(
                        container.ID,
                        new ContainerStartParameters(),
                        token);

                // Stream stdout/stderr in real time via SignalR
                // Follow=true keeps reading until container exits
                var stdoutBuilder = new StringBuilder();
                var stderrBuilder = new StringBuilder();

                using var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(MaxExecutionSeconds));

                try
                {
                    var logStream = await dockerClient.Containers
                        .GetContainerLogsAsync(
                            container.ID,
                            false,
                            new ContainerLogsParameters
                            {
                                ShowStdout = true,
                                ShowStderr = true,
                                // Follow=true streams as code runs
                                // instead of waiting for completion
                                Follow = true
                            },
                            cts.Token);

                    var buffer = new byte[4096];

                    while (true)
                    {
                        var readResult = await logStream
                            .ReadOutputAsync(
                                buffer, 0,
                                buffer.Length,
                                cts.Token);

                        if (readResult.EOF) break;

                        var text = Encoding.UTF8.GetString(
                            buffer, 0, readResult.Count);

                        if (readResult.Target ==
                            MultiplexedStream.TargetStream.StandardOut)
                        {
                            stdoutBuilder.Append(text);

                            // Stream stdout chunk to frontend
                            await _hubContext.Clients
                                .Group(jobId)
                                .SendAsync(
                                    "StdoutChunk", jobId, text, token);
                        }
                        else
                        {
                            stderrBuilder.Append(text);

                            // Stream stderr chunk to frontend
                            await _hubContext.Clients
                                .Group(jobId)
                                .SendAsync(
                                    "StderrChunk", jobId, text, token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Re-throw so outer handler marks job TIMED_OUT
                    throw;
                }

                // Wait for container exit code
                var waitResult = await dockerClient.Containers
                    .WaitContainerAsync(container.ID, token);

                var stdout = stdoutBuilder.ToString();
                var stderr = stderrBuilder.ToString();

                // Only set fallback message if truly no output
                if (string.IsNullOrWhiteSpace(stdout)
                    && waitResult.StatusCode == 0)
                {
                    stdout = "Program executed successfully (no output)";
                }

                job.Status = waitResult.StatusCode == 0
                    ? "COMPLETED" : "FAILED";
                job.Stdout = stdout;
                job.Stderr = stderr;
                job.ExitCode = (int)waitResult.StatusCode;
                job.ExecutionTimeMs = (long)(DateTime.Now - startTime)
                    .TotalMilliseconds;
                job.CompletedAt = DateTime.Now;

                await dockerClient.Containers
                    .RemoveContainerAsync(
                        container.ID,
                        new ContainerRemoveParameters { Force = true },
                        token);

                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);

                // Notify frontend job is fully done
                await _hubContext.Clients
                    .Group(jobId)
                    .SendAsync(
                        "JobCompleted",
                        jobId, stdout, stderr,
                        job.ExitCode, token);
            }
            catch (OperationCanceledException)
            {
                job.Status = "TIMED_OUT";
                job.Stderr = "Execution exceeded time limit!";
                job.CompletedAt = DateTime.Now;

                await _hubContext.Clients
                    .Group(jobId)
                    .SendAsync("JobTimedOut", jobId, token);
            }
            catch (Exception ex)
            {
                job.Status = "FAILED";
                job.Stderr = ex.Message;
                job.CompletedAt = DateTime.Now;
                _logger.LogError(ex, "Job {JobId} failed", job.JobId);

                await _hubContext.Clients
                    .Group(jobId)
                    .SendAsync("JobFailed", jobId, ex.Message, token);
            }

            await context.SaveChangesAsync(token);
        }

        private string GetDefaultFileName(string language)
        {
            return language.ToLower() switch
            {
                "python" => "main",
                "java" => "Main",
                "csharp" => "Program",
                "c" => "main",
                "c++" => "main",
                _ => "code"
            };
        }

        private string ExtractJavaClassName(string sourceCode)
        {
            var match = Regex.Match(
                sourceCode, @"public\s+class\s+(\w+)");
            if (match.Success)
                return match.Groups[1].Value;

            match = Regex.Match(sourceCode, @"class\s+(\w+)");
            if (match.Success)
                return match.Groups[1].Value;

            return "Main";
        }

        private string GetFileExtension(string language)
        {
            return language.ToLower() switch
            {
                "python" => ".py",
                "java" => ".java",
                "csharp" => ".cs",
                "c" => ".c",
                "c++" => ".cpp",
                _ => ".txt"
            };
        }

        private string GetDockerImage(string language)
        {
            return language.ToLower() switch
            {
                "python" => "python:3.11-slim",
                "java" => "eclipse-temurin:17-jdk-alpine",
                "csharp" => "mcr.microsoft.com/dotnet/sdk:8.0",
                "c" => "gcc:latest",
                "c++" => "gcc:latest",
                _ => "alpine:latest"
            };
        }
    }
}
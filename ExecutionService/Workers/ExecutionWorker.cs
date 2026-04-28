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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Execution Worker started! Supported: Python, Java, C#, C, C++");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessQueuedJobs(stoppingToken);
                await Task.Delay(2000, stoppingToken);
            }
        }

        private async Task ProcessQueuedJobs(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ExecutionDbContext>();

            var job = await context.ExecutionJobs
                .FirstOrDefaultAsync(j => j.Status == "QUEUED", token);

            if (job == null) return;

            await ExecuteJob(job, context, token);
        }

        private async Task ExecuteJob(ExecutionJob job, ExecutionDbContext context, CancellationToken token)
        {
            var startTime = DateTime.Now;
            var jobId = job.JobId.ToString();

            try
            {
                job.Status = "RUNNING";
                await context.SaveChangesAsync(token);
                await _hubContext.Clients.Group(jobId).SendAsync("JobStarted", jobId, token);

                if (!_supportedLanguages.Contains(job.Language.ToLower()))
                {
                    job.Status = "FAILED";
                    job.Stderr = $"Language '{job.Language}' not supported";
                    job.CompletedAt = DateTime.Now;
                    await context.SaveChangesAsync(token);
                    await _hubContext.Clients.Group(jobId).SendAsync("JobFailed", jobId, "Unsupported language!", token);
                    return;
                }

                _logger.LogInformation("Executing {Language} job {JobId}", job.Language, job.JobId);

                var dockerClient = new DockerClientConfiguration().CreateClient();
                var tempDir = Path.Combine(Path.GetTempPath(), job.JobId.ToString());
                Directory.CreateDirectory(tempDir);

                string extension = GetFileExtension(job.Language);
                string workingDir = "/app";
                List<string> runCommand;
                string fileName = GetDefaultFileName(job.Language);

                // Handle each language with dynamic file naming
                switch (job.Language.ToLower())
                {
                    case "python":
                        var pyFile = Path.Combine(tempDir, $"{fileName}.py");
                        await File.WriteAllTextAsync(pyFile, job.SourceCode, token);
                        runCommand = new List<string> { "python", $"/app/{fileName}.py" };
                        break;

                    case "java":
                        // Extract class name from Java code
                        string className = ExtractJavaClassName(job.SourceCode);
                        var javaFile = Path.Combine(tempDir, $"{className}.java");
                        await File.WriteAllTextAsync(javaFile, job.SourceCode, token);
                        runCommand = new List<string> { "sh", "-c", $"cd /app && javac {className}.java && java {className}" };
                        break;

                    case "csharp":
                        // Create a simple C# project structure
                        var csDir = Path.Combine(tempDir, "CSharpApp");
                        Directory.CreateDirectory(csDir);
                        
                        // Create Program.cs
                        await File.WriteAllTextAsync(Path.Combine(csDir, "Program.cs"), job.SourceCode, token);
                        
                        // Create .csproj file
                        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>";
                        await File.WriteAllTextAsync(Path.Combine(csDir, "CSharpApp.csproj"), csproj, token);
                        
                        workingDir = "/app/CSharpApp";
                        runCommand = new List<string> { "sh", "-c", "dotnet restore && dotnet run" };
                        break;

                    case "c":
                        var cFile = Path.Combine(tempDir, $"{fileName}.c");
                        await File.WriteAllTextAsync(cFile, job.SourceCode, token);
                        runCommand = new List<string> { "sh", "-c", $"gcc /app/{fileName}.c -o /app/{fileName} && /app/{fileName}" };
                        break;

                    case "c++":
                        var cppFile = Path.Combine(tempDir, $"{fileName}.cpp");
                        await File.WriteAllTextAsync(cppFile, job.SourceCode, token);
                        runCommand = new List<string> { "sh", "-c", $"g++ /app/{fileName}.cpp -o /app/{fileName} && /app/{fileName}" };
                        break;

                    default:
                        runCommand = new List<string> { "echo", "Unsupported language" };
                        break;
                }

                var container = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = GetDockerImage(job.Language),
                    Cmd = runCommand,
                    WorkingDir = workingDir,
                    NetworkDisabled = true,
                    HostConfig = new HostConfig
                    {
                        Memory = MaxMemoryBytes,
                        ReadonlyRootfs = false,
                        Binds = new List<string> { $"{tempDir}:/app:rw" }
                    }
                }, token);

                await dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), token);

                // Wait for completion
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(MaxExecutionSeconds));
                var waitResult = await dockerClient.Containers.WaitContainerAsync(container.ID, cts.Token);
                
                // Small delay for Java output to flush
                if (job.Language.ToLower() == "java")
                {
                    await Task.Delay(500, token);
                }
                
                // Get output
                string stdout = "";
                try
                {
                    var logStream = await dockerClient.Containers.GetContainerLogsAsync(
                        container.ID,
                        false,
                        new ContainerLogsParameters { ShowStdout = true, ShowStderr = true },
                        token);

                    var stdoutBuilder = new StringBuilder();
                    var buffer = new byte[4096];

                    while (true)
                    {
                        var readResult = await logStream.ReadOutputAsync(buffer, 0, buffer.Length, token);
                        if (readResult.EOF) break;

                        var text = Encoding.UTF8.GetString(buffer, 0, readResult.Count);
                        if (readResult.Target == MultiplexedStream.TargetStream.StandardOut)
                        {
                            stdoutBuilder.Append(text);
                        }
                    }
                    stdout = stdoutBuilder.ToString();
                    
                    if (string.IsNullOrWhiteSpace(stdout))
                    {
                        stdout = "Program executed successfully";
                    }
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Could not retrieve logs");
                    stdout = "Program executed successfully";
                }

                job.Status = "COMPLETED";
                job.Stdout = stdout;
                job.Stderr = "";
                job.ExitCode = (int)waitResult.StatusCode;
                job.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
                job.CompletedAt = DateTime.Now;

                await dockerClient.Containers.RemoveContainerAsync(container.ID,
                    new ContainerRemoveParameters { Force = true }, token);

                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);

                await _hubContext.Clients.Group(jobId).SendAsync("JobCompleted", jobId, stdout, "", job.ExitCode, token);
            }
            catch (OperationCanceledException)
            {
                job.Status = "TIMED_OUT";
                job.Stderr = "Execution exceeded time limit!";
                job.CompletedAt = DateTime.Now;
                await _hubContext.Clients.Group(jobId).SendAsync("JobTimedOut", jobId, token);
            }
            catch (Exception ex)
            {
                job.Status = "FAILED";
                job.Stderr = ex.Message;
                job.CompletedAt = DateTime.Now;
                _logger.LogError(ex, "Job {JobId} failed", job.JobId);
                await _hubContext.Clients.Group(jobId).SendAsync("JobFailed", jobId, ex.Message, token);
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
            // Look for "public class ClassName" pattern
            var match = Regex.Match(sourceCode, @"public\s+class\s+(\w+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            // Look for "class ClassName" pattern
            match = Regex.Match(sourceCode, @"class\s+(\w+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            return "Main"; // default fallback
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
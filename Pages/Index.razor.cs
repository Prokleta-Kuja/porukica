using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using porukica.Jobs;
using Quartz;

namespace porukica.Pages
{
    public partial class Index
    {
        [Inject] ISchedulerFactory Q { get; set; }
        [Inject] IJSRuntime JS { get; set; }
        bool TextForm { get; set; } = true;
        string Secret { get; set; }
        string Text { get; set; }
        IBrowserFile File { get; set; }
        double UploadProgress { get; set; } = -1;
        CancellationTokenSource cts;
        private async Task Post(TimeSpan ts)
        {
            var scheduler = await Q.GetScheduler();
            var key = Guid.NewGuid().ToString();
            var message = new Message { Secret = Secret };
            var jobData = CreateJobData(key);
            var trigger = TriggerBuilder.Create().StartAt(DateTimeOffset.UtcNow.Add(ts)).Build();

            if (!string.IsNullOrWhiteSpace(Text))
            {
                message.Value = Text;
                Database.Texts.Add(key, message);

                var job = JobBuilder.Create<TextJob>().SetJobData(jobData).Build();
                await scheduler.ScheduleJob(job, trigger);

                Text = null;

                return;
            }

            if (File != null)
            {
                var di = CreateFileDir(key);
                message.Value = Path.Combine(di.FullName, File.Name);
                var fi = new FileInfo(message.Value);

                using var remote = File.OpenReadStream(maxAllowedSize: C.MAX_FILE_SIZE);
                using var local = fi.OpenWrite();

                int bytesRead;
                var totalRead = 0d;
                var totalSize = File.Size;
                cts = new CancellationTokenSource();

                var buffer = new byte[C.UPLOAD_BUFFER_SIZE];
                var memory = new Memory<byte>(buffer);
                while ((bytesRead = await remote.ReadAsync(memory, cts.Token)) != 0)
                {
                    await local.WriteAsync(memory, cts.Token);
                    totalRead += bytesRead;
                    var complete = totalRead / totalSize;
                    var completePercent = Math.Floor(complete * 100);

                    if (completePercent > UploadProgress)
                    {
                        UploadProgress = completePercent;
                        StateHasChanged();
                    }
                }

                await local.FlushAsync();
                remote.Close();
                local.Close();

                Database.Files.Add(key, message);

                var job = JobBuilder.Create<FileJob>().SetJobData(jobData).Build();
                await scheduler.ScheduleJob(job, trigger);

                UploadProgress = -1;
            }

        }
        private async Task CopyTextToClipboard(string text) => await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
        private void CancelUpload()
        {
            cts.Cancel();
            UploadProgress = -1;
            // TODO: find which file is not in files list, and delete it's folder
        }

        private void FileSelected(InputFileChangeEventArgs e)
        {
            File = e.File;
        }
        private static JobDataMap CreateJobData(string key)
        {
            var kv = new Dictionary<string, string> { { "Id", key } };

            return new JobDataMap(kv);
        }
        private static DirectoryInfo CreateFileDir(string key)
        {
            var uploads = new DirectoryInfo(C.UPLOAD_DIR);
            var fdi = uploads.CreateSubdirectory(key);
            return fdi;
        }
    }
}
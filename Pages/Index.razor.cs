using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using porukica.Jobs;
using Quartz;

namespace porukica.Pages
{
    public partial class Index
    {
        [Inject] ISchedulerFactory Q { get; set; }
        bool TextForm { get; set; }
        string Secret { get; set; }
        string Text { get; set; }
        IBrowserFile File { get; set; }
        double UploadProgress { get; set; } = -1;
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

                var buffer = new byte[C.UPLOAD_BUFFER_SIZE];
                while ((bytesRead = await remote.ReadAsync(buffer, 0, buffer.Length)) != 0) // Cancellation token here
                {
                    await local.WriteAsync(buffer, 0, bytesRead); // Cancellation token here
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
        private void FileSelected(InputFileChangeEventArgs e)
        {
            var files = e.GetMultipleFiles(1);
            File = files.FirstOrDefault();
        }
        private JobDataMap CreateJobData(string key)
        {
            var kv = new Dictionary<string, string> { { "Id", key } };

            return new JobDataMap(kv);
        }
        private DirectoryInfo CreateFileDir(string key)
        {
            var uploads = new DirectoryInfo(C.UPLOAD_DIR);
            var fdi = uploads.CreateSubdirectory(key);
            return fdi;
        }
    }
}
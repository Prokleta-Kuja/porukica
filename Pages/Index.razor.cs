using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using porukica.Jobs;
using porukica.Models;
using Quartz;

namespace porukica.Pages
{
    public partial class Index
    {
        [Inject] ISchedulerFactory Q { get; set; }
        [Inject] IJSRuntime JS { get; set; }
        ElementReference TextInput { get; set; }
        bool TextForm { get; set; } = true;
        string Secret { get; set; }
        string Text { get; set; }
        int Time { get; set; } = 3;
        IBrowserFile File { get; set; }
        double UploadProgress { get; set; } = -1;
        CancellationTokenSource cts;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            await TextInput.FocusAsync();
        }
        private async Task Post(TimeType type)
        {
            Time = Math.Abs(Time);
            var ts =
                type == TimeType.Minutes ? TimeSpan.FromMinutes(Time > 1440 ? 1440 : Time) :
                type == TimeType.Hours ? TimeSpan.FromHours(Time > 24 ? 24 : Time) :
                TimeSpan.FromDays(Time > 1 ? 1 : Time);

            if (TextForm)
                await AddText(ts);
            else
                await AddFile(ts);

            Text = null;
        }
        private async Task AddText(TimeSpan ts)
        {
            var scheduler = await Q.GetScheduler();
            var key = Guid.NewGuid().ToString();
            var jobData = CreateJobData(key);
            var trigger = TriggerBuilder.Create().StartAt(DateTimeOffset.UtcNow.Add(ts)).Build();

            var message = new TextModel(Secret, Text);
            Database.Texts.Add(key, message);

            var job = JobBuilder.Create<TextJob>().SetJobData(jobData).Build();
            await scheduler.ScheduleJob(job, trigger);
        }
        private async Task AddFile(TimeSpan ts)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();

            var key = Guid.NewGuid().ToString();
            var di = CreateFileDir(key);
            var fileName = Path.Combine(di.FullName, File.Name);
            var fi = new FileInfo(fileName);

            using var local = fi.OpenWrite();
            using var remote = File.OpenReadStream(maxAllowedSize: C.MAX_FILE_SIZE);

            var success = false;
            int bytesRead;
            var totalRead = 0d;
            var totalSize = File.Size;

            var buffer = new byte[C.UPLOAD_BUFFER_SIZE];
            var memory = new Memory<byte>(buffer);
            try
            {
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

                success = true;
            }
            catch (OperationCanceledException) { }
            finally
            {
                await local.FlushAsync();
                remote.Close();
                local.Close();
            }

            if (success)
            {
                var scheduler = await Q.GetScheduler();
                var jobData = CreateJobData(key);
                var trigger = TriggerBuilder.Create().StartAt(DateTimeOffset.UtcNow.Add(ts)).Build();

                var message = new FileModel(Secret, Text, fi);
                message.Size = totalSize;
                Database.Files.Add(key, message);

                var job = JobBuilder.Create<FileJob>().SetJobData(jobData).Build();
                await scheduler.ScheduleJob(job, trigger);
            }
            else
                di.Delete(true);

            UploadProgress = -1;
            cts = null;
        }
        private async Task CopyTextToClipboard(string text) => await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
        private void CancelUpload()
        {
            cts?.Cancel();
            UploadProgress = -1;
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
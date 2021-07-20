using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
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
        [Inject] IOptions<Settings> Config { get; set; }
        ElementReference TextInput { get; set; }
        string Error { get; set; }
        bool TextForm { get; set; } = true;
        string Secret { get; set; }
        string Text { get; set; }
        int Time { get; set; } = 3;
        string Authorization { get; set; }
        IBrowserFile File { get; set; }
        double UploadProgress { get; set; } = -1;
        CancellationTokenSource cts;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            await TextInput.FocusAsync();
        }
        protected void Refresh() => StateHasChanged();
        private async Task Post(TimeType type)
        {
            Error = string.Empty;
            Time = Math.Abs(Time);
            var ts = type == TimeType.Minutes
                ? TimeSpan.FromMinutes(Time)
                : type == TimeType.Hours
                    ? TimeSpan.FromHours(Time)
                    : TimeSpan.FromDays(Time);

            if (!Config.Value.ValidTimeout(ts))
            {
                Error = $"Max timeout exceeded ({Config.Value.MaxTimeout})";
                return;
            }

            if (TextForm)
                await AddText(ts);
            else
                await AddFile(ts);
        }
        private async Task AddText(TimeSpan ts)
        {
            if (!Config.Value.ValidAuthorizationText(Authorization))
            {
                Error = "Not Allowed";
                return;
            }

            var scheduler = await Q.GetScheduler();
            var key = Guid.NewGuid().ToString();
            var jobData = CreateJobData(key);
            var trigger = TriggerBuilder.Create().StartAt(DateTimeOffset.UtcNow.Add(ts)).Build();

            var message = new TextModel(Secret, Text);
            Database.Texts.Add(key, message);

            var job = JobBuilder.Create<TextJob>().SetJobData(jobData).Build();
            await scheduler.ScheduleJob(job, trigger);

            Text = null;
        }
        private async Task AddFile(TimeSpan ts)
        {
            if (!Config.Value.ValidAuthorizationFile(Authorization))
            {
                Error = "Not Allowed";
                return;
            }
            if (Config.Value.MaxFileSize < File.Size)
            {
                Error = $"Max file size exceeded ({C.BytesToString(Config.Value.MaxFileSize)})";
                return;
            }

            cts?.Cancel();
            cts = new CancellationTokenSource();

            var key = Guid.NewGuid().ToString();
            var di = CreateFileDir(key);
            var fileName = Path.Combine(di.FullName, File.Name);
            var fi = new FileInfo(fileName);

            using var local = fi.OpenWrite();
            using var remote = File.OpenReadStream(maxAllowedSize: Config.Value.MaxFileSize);

            var success = false;
            int bytesRead;
            var totalRead = 0d;
            var totalSize = File.Size;

            var buffer = new byte[Config.Value.BufferSize];
            try
            {
                //await remote.CopyToAsync(local);
                while ((bytesRead = await remote.ReadAsync(buffer, 0, buffer.Length, cts.Token)) != 0)
                {
                    local.Write(buffer, 0, bytesRead);
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

                using var md5 = MD5.Create();
                using var stream = fi.OpenRead();
                var hash = md5.ComputeHash(stream);
                foreach (byte b in hash)
                    message.Hash += b.ToString("x2");

                Database.Files.Add(key, message);

                var job = JobBuilder.Create<FileJob>().SetJobData(jobData).Build();
                await scheduler.ScheduleJob(job, trigger);
            }
            else
                di.Delete(true);

            UploadProgress = -1;
            cts = null;
            Text = null;
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
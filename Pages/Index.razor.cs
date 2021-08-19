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
    public partial class Index : IAsyncDisposable
    {
        [Inject] ISchedulerFactory Q { get; set; }
        [Inject] IJSRuntime JS { get; set; }
        [Inject] IOptions<Settings> Config { get; set; }
        IJSObjectReference module;
        DotNetObjectReference<Index> objRef;
        ElementReference TextInput { get; set; }
        string Error { get; set; }
        bool TextForm { get; set; } = true;
        string Secret { get; set; }
        string Text { get; set; }
        int Time { get; set; } = 3;
        string Authorization { get; set; }
        IBrowserFile File { get; set; }
        double UploadProgress { get; set; } = -1;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            await TextInput.FocusAsync();
            objRef = DotNetObjectReference.Create(this);
            module = await JS.InvokeAsync<IJSObjectReference>("import", "/js/file.js");
        }
        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            objRef?.Dispose();
            if (module is not null)
                await module.DisposeAsync();
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

            var key = Guid.NewGuid().ToString();
            var di = CreateFileDir(key);
            var fileName = Path.Combine(di.FullName, File.Name);
            var fi = new FileInfo(fileName);

            // Schedule removal of upload state
            var timeout = TimeSpan.FromMinutes(Config.Value.UPLOAD_TIMEOUT_M);
            var scheduler = await Q.GetScheduler();
            var jobData = CreateJobData(key);
            var trigger = TriggerBuilder.Create().StartAt(DateTimeOffset.UtcNow.Add(timeout)).Build();

            var upload = new UploadModel(ts, fi);
            upload.Size = File.Size;

            Database.Uploads.Add(key, upload);

            var job = JobBuilder.Create<UploadJob>().SetJobData(jobData).Build();
            await scheduler.ScheduleJob(job, trigger);

            await module.InvokeVoidAsync("start", objRef, key);
        }
        [JSInvokable]
        public async Task UpdateProgress(string key, double complete)
        {
            if (complete != 100)
                UploadProgress = complete;
            else
            {
                if (!Database.Uploads.TryGetValue(key, out var upload))
                    return;

                UploadProgress = 100;
                StateHasChanged();

                var scheduler = await Q.GetScheduler();
                var jobData = CreateJobData(key);
                var trigger = TriggerBuilder.Create().StartAt(DateTimeOffset.UtcNow.Add(upload.ExpireAfter)).Build();

                var fi = new FileInfo(upload.Path);
                if (!fi.Exists)
                    return;
                var message = new FileModel(Secret, Text, fi);
                message.Size = fi.Length;

                using var md5 = MD5.Create();
                using var stream = fi.OpenRead();
                var hash = md5.ComputeHash(stream);
                foreach (byte b in hash)
                    message.Hash += b.ToString("x2");

                Database.Files.Add(key, message);

                var job = JobBuilder.Create<FileJob>().SetJobData(jobData).Build();
                await scheduler.ScheduleJob(job, trigger);

                UploadProgress = -1;
                Text = null;
            }

            StateHasChanged();
        }
        private async Task CopyTextToClipboard(string text) => await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
        private async Task CancelUpload()
        {
            await module.InvokeVoidAsync("cancel");
            UploadProgress = -1;
            StateHasChanged();
        }
        private void FileSelected(InputFileChangeEventArgs e) => File = e.File;
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
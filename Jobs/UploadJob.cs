using System.IO;
using System.Threading.Tasks;
using Quartz;

namespace porukica.Jobs
{
    public class UploadJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var id = context.MergedJobDataMap["Id"]?.ToString();

            if (!Database.Uploads.ContainsKey(id))
                return;

            var message = Database.Uploads[id];
            var fi = new FileInfo(message.Path);

            fi.Directory.Delete(true);
            Database.Uploads.Remove(id);

            await Task.CompletedTask;
        }
    }

}
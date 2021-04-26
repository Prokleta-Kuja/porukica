using System;
using System.IO;
using System.Threading.Tasks;
using Quartz;

namespace porukica.Jobs
{
    public class FileJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var id = context.MergedJobDataMap["Id"]?.ToString();

            if (!Database.Files.ContainsKey(id))
                return;

            var message = Database.Files[id];
            var fi = new FileInfo(message.Path);

            fi.Directory.Delete(true);
            Database.Files.Remove(id);

            await Task.CompletedTask;
        }
    }

}
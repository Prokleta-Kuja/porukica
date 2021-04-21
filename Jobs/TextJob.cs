using System;
using System.Threading.Tasks;
using Quartz;

namespace porukica.Jobs
{
    public class TextJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var id = context.MergedJobDataMap["Id"]?.ToString();

            if (!Database.Texts.ContainsKey(id))
                return;

            Database.Texts.Remove(id);

            await Task.CompletedTask;
        }
    }

}
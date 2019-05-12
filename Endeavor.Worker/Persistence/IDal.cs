using Endeavor.Steps;
using System;
using System.Collections.Generic;
using System.Text;

namespace Endeavor.Worker.Persistence
{
    public interface IDal
    {
        Dictionary<string, object> GetStep(int stepId, string stepType);
        string GetTaskData(long taskId);
        void UpdateTaskStatus(long taskId, StatusType status);
        void ReleaseTask(long taskId, string releaseValue, string output);
    }
}

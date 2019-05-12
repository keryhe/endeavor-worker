using System;
using System.Collections.Generic;
using System.Text;

namespace Endeavor.Worker.Messaging
{
    [Serializable]
    public class TaskToBeWorked
    {
        public long TaskId;
        public int StepId;
        public string StepType;
    }
}

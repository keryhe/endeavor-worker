using Keryhe.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Endeavor.Worker.Messaging;
using Endeavor.Worker.Persistence;
using System.Collections.Generic;
using Endeavor.Steps;

namespace Endeavor.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IMessageListener<TaskToBeWorked> _listener;
        private readonly Func<string, IStep> _stepAccessor;
        private readonly IDal _dal;
        private readonly ILogger<Worker> _logger;
        private ManualResetEvent _resetEvent = new ManualResetEvent(false);


        public Worker(IMessageListener<TaskToBeWorked> listener, Func<string, IStep> stepAccessor, IDal dal, ILogger<Worker> logger)
        {
            _listener = listener;
            _stepAccessor = stepAccessor;
            _dal = dal;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker Started");

            await Task.Run(() =>
            {
                _listener.Start(Callback);
                _resetEvent.WaitOne();
            });

            _logger.LogInformation("Worker Stopped");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _listener.Stop();
            _listener.Dispose();
            _resetEvent.Set();
            return base.StopAsync(cancellationToken);
        }

        private void Callback(TaskToBeWorked message)
        {
            try
            {
                _logger.LogDebug("Processing {0} for task {1}" , message.StepType, message.TaskId);
                _dal.UpdateTaskStatus(message.TaskId, StatusType.Processing);

                TaskRequest request = new TaskRequest
                {
                    TaskId = message.TaskId,
                    Status = StatusType.Processing,
                    Input = _dal.GetTaskData(message.TaskId)
                };

                TaskResponse response = ExecuteStep(message.StepId, message.StepType, request);

                if (response.Status == StatusType.Complete)
                {
                    _dal.ReleaseTask(message.TaskId, response.ReleaseValue, response.Output);
                }
                else
                {
                    _dal.UpdateTaskStatus(message.TaskId, response.Status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task {0}", message.TaskId);
                _dal.UpdateTaskStatus(message.TaskId, StatusType.Error);
            }
        }

        public TaskResponse ExecuteStep(int stepId, string stepType, TaskRequest request)
        {
            Dictionary<string, object> stepData = _dal.GetStep(stepId, stepType);

            IStep step = _stepAccessor(stepType);
            step.Initialize(stepData);
            TaskResponse response = step.Execute(request);

            return response;
        }
    }
}

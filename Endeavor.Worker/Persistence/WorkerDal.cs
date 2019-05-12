using Endeavor.Steps;
using Keryhe.Persistence;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Endeavor.Worker.Persistence
{
    public class WorkerDal : IDal
    {
        private readonly IPersistenceProvider _provider;

        public WorkerDal(IPersistenceProvider provider)
        {
            _provider = provider;
        }

        public Dictionary<string, object> GetStep(int stepId, string stepType)
        {
            Dictionary<string, object> results = new Dictionary<string, object>();

            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@StepId", stepId }
            };

            List<Dictionary<string, object>> steps = Query("Get" + stepType + "Step", CommandType.StoredProcedure, parameters);

            if (steps.Count > 0)
            {
                results = steps[0];
            }
            return results;
        }

        public string GetTaskData(long taskId)
        {
            string result = "";

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT TaskData FROM Taak WHERE ID = ");
            sb.Append(taskId.ToString());

            List<Dictionary<string, object>> steps = Query(sb.ToString(), CommandType.Text, null);

            if (steps.Count > 0)
            {
                result = steps[0]["TaskOutput"].ToString();
            }
            return result;

        }

        public void UpdateTaskStatus(long taskID, StatusType status)
        {
            int statusValue = (int)status;
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE Task SET StatusValue = ");
            sb.Append(statusValue.ToString());
            sb.Append(" WHERE TaskID = ");
            sb.Append(taskID.ToString());

            _provider.ExecuteNonQuery(sb.ToString(), CommandType.Text);
        }

        public void ReleaseTask(long taskId, string releaseValue, string output)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@TaskID", taskId },
                { "@ReleaseValue", releaseValue },
                { "@TaskData", output }
            };

            _provider.ExecuteNonQuery("ReleaseTask", CommandType.StoredProcedure, parameters);
        }

        private List<Dictionary<string, object>> Query(string name, CommandType commandType, Dictionary<string, object> parameters)
        {
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

            using (var reader = _provider.ExecuteQuery(name, commandType, parameters))
            {
                while (reader.Read())
                {
                    Dictionary<string, object> result = new Dictionary<string, object>();

                    var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

                    foreach (var column in columns)
                    {
                        object columnValue = reader[column];
                        if (columnValue == DBNull.Value)
                        {
                            result.Add(column, null);
                        }
                        else
                        {
                            result.Add(column, columnValue);
                        }
                    }
                    results.Add(result);
                }
            }
            return results;
        }
    }
}

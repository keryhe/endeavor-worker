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

            List<Dictionary<string, object>> steps = Query("Get" + stepType, CommandType.StoredProcedure, parameters);

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
            sb.Append("SELECT TaskData FROM Task WHERE ID = ");
            sb.Append(taskId.ToString());

            List<Dictionary<string, object>> tasks = Query(sb.ToString(), CommandType.Text, null);

            if (tasks.Count > 0)
            {
                result = tasks[0]["TaskData"]?.ToString();
            }
            return result;

        }

        public void UpdateTaskStatus(long taskID, StatusType status)
        {
            int statusValue = (int)status;
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE Task SET StatusValue = ");
            sb.Append(statusValue.ToString());
            sb.Append(" WHERE ID = ");
            sb.Append(taskID.ToString());

            using (IDbConnection connection = _provider.CreateConnection())
            {
                connection.Open();
                _provider.ExecuteNonQuery(connection, sb.ToString(), CommandType.Text);
            }
        }

        public void ReleaseTask(long taskId, string releaseValue, string output)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>
            {
                { "@TaskID", taskId },
                { "@ReleaseValue", releaseValue },
                { "@TaskData", output }
            };

            using (IDbConnection connection = _provider.CreateConnection())
            {
                connection.Open();
                _provider.ExecuteNonQuery(connection, "ReleaseTask", CommandType.StoredProcedure, parameters);
            }
        }

        private List<Dictionary<string, object>> Query(string name, CommandType commandType, Dictionary<string, object> parameters)
        {
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

            using (IDbConnection connection = _provider.CreateConnection())
            {
                connection.Open();
                using (var reader = _provider.ExecuteQuery(connection, name, commandType, parameters))
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
            }
            return results;
        }
    }
}

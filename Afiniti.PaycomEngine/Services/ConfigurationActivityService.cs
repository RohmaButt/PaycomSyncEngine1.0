using Afiniti.Paycom.DAL;
using Afiniti.Paycom.Shared.Models;
using System;
using System.Linq;

namespace Afiniti.PaycomEngine.Services
{
    public class ConfigurationActivityService
    {
        public ConfigActivityResponseModel RegisterConfigurationActivity(string ActivityName, string ActivityDetail)
        {
            ConfigurationActivityModel configurationActivity = null;
            ConfigActivityResponseModel responseModel = null;

            if (ActivityDetail == "Started")
            {
                bool status;
                using (var dbContext = new PaycomEngineContext())
                {
                    status = dbContext.ConfigurationActivity.Any(x => x.Description == ActivityName && x.IsActive == true);
                }
                if (status)//another process is in progress 
                {
                    responseModel = new ConfigActivityResponseModel
                    {
                        ResponseStatus = 1,
                        ResponseDescription = "Another process is already in progress. Please wait"
                    };

                    return responseModel;
                }
                else//not in progress 
                {
                    configurationActivity = new ConfigurationActivityModel()
                    {
                        IsActive = true,
                        StartDate = DateTime.Now,
                        EndDate = null,
                        Description = ActivityName,
                        ActivityDetail = ActivityDetail
                    };
                    responseModel = new ConfigActivityResponseModel
                    {
                        ResponseStatus = 0,
                        ResponseDescription = ActivityDetail
                    };
                }
            }
            else if (ActivityDetail.Contains("Completed") || ActivityDetail.Contains("Error"))
            {
                configurationActivity = new ConfigurationActivityModel()
                {
                    IsActive = false,
                    EndDate = DateTime.Now,
                    Description = ActivityName,
                    ActivityDetail = ActivityDetail
                };
                responseModel = new ConfigActivityResponseModel
                {
                    ResponseStatus = 0,
                    ResponseDescription = ActivityDetail
                };
            }

            AddConfigurationActivityInDB(configurationActivity);

            return responseModel;
        }

        public static void AddConfigurationActivityInDB(ConfigurationActivityModel model)
        {
            using (var dbContext = new PaycomEngineContext())
            {
                ConfigurationActivity configurationActivity = new ConfigurationActivity()
                {
                    ActivityKey = Guid.NewGuid(),
                    IsActive = model.IsActive,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Description = model.Description,
                    ActivityDetail = model.ActivityDetail,
                    ConfigURLKey = dbContext.ConfigurationSetting.AsNoTracking().FirstOrDefault(x => x.ConfigAppEvent == model.Description).ConfigValueKey
                };
                if (model.ActivityDetail == "Started")
                {
                    dbContext.ConfigurationActivity.Add(configurationActivity);
                }
                else if (model.ActivityDetail.Contains("Completed") || model.ActivityDetail.Contains("Error"))
                {
                    var obj = dbContext.ConfigurationActivity.SingleOrDefault(x => x.Description == model.Description && x.IsActive == true);
                    if (obj != null)
                    {
                        obj.ActivityKey = Guid.NewGuid();
                        obj.IsActive = model.IsActive;
                        obj.EndDate = model.EndDate;
                        obj.Description = model.Description;
                        obj.ActivityDetail = model.ActivityDetail;
                    }
                }
                dbContext.SaveChanges();
            }
        }
    }
}
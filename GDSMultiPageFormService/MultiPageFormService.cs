namespace GDS.MultiPageFormData
{
    using GDS.MultiPageFormData.Enums;
    using GDS.MultiPageFormData.Models;
    using GDS.MultiPageFormData.Services;
    using LearningHub.Nhs.Caching;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Newtonsoft.Json;
    using System;
    using System.Data;


    public interface IMultiPageFormService
    {
        Task SetMultiPageFormData(object formData, MultiPageFormDataFeature feature, ITempDataDictionary tempData);


        Task<T> GetMultiPageFormData<T>(MultiPageFormDataFeature feature, ITempDataDictionary tempData);


        Task ClearMultiPageFormData(MultiPageFormDataFeature feature, ITempDataDictionary tempData);


        Task<bool> FormDataExistsForGuidAndFeature(MultiPageFormDataFeature feature, Guid tempDataGuid);

    }

    public class MultiPageFormService : IMultiPageFormService
    {
        private static IDbConnection _DbConnection;
        private readonly ICacheService cacheService;

        public MultiPageFormService(ICacheService cacheService)
        {
            this.cacheService = cacheService;
        }

        private static bool useRedisCache = AppConfig.GetMultiPageFormDataStore();

        public static void InitConnection(IDbConnection Connection)
        {
            try
            {
                if (Connection != null)
                {
                    _DbConnection = Connection;
                    _DbConnection.Open();
                    DbService db = new DbService(_DbConnection);
                    if (!db.IsDbTableExist())
                    {
                        db.CreateTable();
                    }

                }
                else
                {
                    throw new Exception("Connection object is null or empty");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (_DbConnection.State == System.Data.ConnectionState.Open)
                {
                    _DbConnection.Close();
                }
            }
        }

        public async Task SetMultiPageFormData(object formData, MultiPageFormDataFeature feature, ITempDataDictionary tempData)
        {
            var json = JsonConvert.SerializeObject(formData);
            if (useRedisCache)
            {
                var tempDataGuid = tempData[feature.TempDataKey] == null ? Guid.NewGuid() : (Guid)tempData[feature.TempDataKey];
                var multiPageFormData = new MultiPageFormData
                {
                    TempDataGuid = tempDataGuid,
                    Json = json,
                    Feature = feature.Name,
                    CreatedDate = ClockService.UtcNow,
                };
                string MultiPageFormCacheKey = GetMultiPageFormCacheKey(multiPageFormData.TempDataGuid, multiPageFormData.Feature);
                await this.cacheService.SetAsync(MultiPageFormCacheKey, JsonConvert.SerializeObject(multiPageFormData));
                tempData[feature.TempDataKey] = tempDataGuid;
                return;
            }
            else if (_DbConnection != null)
            {
                MultiPageFormDataService multiPageFormDataService = new MultiPageFormDataService(_DbConnection);
                if (tempData[feature.TempDataKey] != null)
                {
                    var tempDataGuid = (Guid)tempData[feature.TempDataKey];

                    var existingMultiPageFormData =
                        multiPageFormDataService.GetMultiPageFormDataByGuidAndFeature(tempDataGuid, feature.Name);
                    if (existingMultiPageFormData != null)
                    {
                        multiPageFormDataService.UpdateJsonByGuid(tempDataGuid, json);
                        tempData[feature.TempDataKey] = tempDataGuid;
                        return;
                    }
                }

                var multiPageFormData = new MultiPageFormData
                {
                    TempDataGuid = Guid.NewGuid(),
                    Json = json,
                    Feature = feature.Name,
                    CreatedDate = ClockService.UtcNow,
                };
                multiPageFormDataService.InsertMultiPageFormData(multiPageFormData);
                tempData[feature.TempDataKey] = multiPageFormData.TempDataGuid;
            }
            else
            {
                throw new Exception("Connection object is null or empty");
            }
        }

        public async Task<T> GetMultiPageFormData<T>(MultiPageFormDataFeature feature, ITempDataDictionary tempData)
        {
            if (tempData[feature.TempDataKey] == null)
            {
                throw new Exception("Attempted to get data with no Guid identifier");
            }
            var settings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
            var tempDataGuid = (Guid)tempData.Peek(feature.TempDataKey);
            if (useRedisCache)
            {
                string MultiPageFormCacheKey = GetMultiPageFormCacheKey(tempDataGuid, feature.Name);
                var existingMultiPageFormData = await this.cacheService.GetAsync<MultiPageFormData>(MultiPageFormCacheKey);

                if (existingMultiPageFormData == null)
                {
                    throw new Exception($"MultiPageFormData not found for {tempDataGuid}");
                }

                tempData[feature.TempDataKey] = tempDataGuid;
                return JsonConvert.DeserializeObject<T>(existingMultiPageFormData.Json, settings);


            }
            else if (_DbConnection != null)
            {
                MultiPageFormDataService multiPageFormDataService = new MultiPageFormDataService(_DbConnection);

                var existingMultiPageFormData =
                    multiPageFormDataService.GetMultiPageFormDataByGuidAndFeature(tempDataGuid, feature.Name);
                if (existingMultiPageFormData == null)
                {
                    throw new Exception($"MultiPageFormData not found for {tempDataGuid}");
                }
                tempData[feature.TempDataKey] = tempDataGuid;
                return JsonConvert.DeserializeObject<T>(existingMultiPageFormData.Json, settings);
            }
            else
            {
                throw new Exception("Connection object is null or empty");
            }

        }

        public async Task ClearMultiPageFormData(MultiPageFormDataFeature feature, ITempDataDictionary tempData)
        {
            if (tempData[feature.TempDataKey] == null)
            {
                throw new Exception("Attempted to clear data with no Guid identifier");
            }
            var tempDataGuid = (Guid)tempData.Peek(feature.TempDataKey);
            if (useRedisCache)
            {
                string MultiPageFormCacheKey = GetMultiPageFormCacheKey(tempDataGuid, feature.Name);
                await this.cacheService.RemoveAsync(MultiPageFormCacheKey);
                tempData.Remove(feature.TempDataKey);
            }
            else if (_DbConnection != null)
            {
                MultiPageFormDataService multiPageFormDataService = new MultiPageFormDataService(_DbConnection);
                multiPageFormDataService.DeleteByGuid(tempDataGuid);
                tempData.Remove(feature.TempDataKey);
            }
            else
            {
                throw new Exception("Connection object is null or empty");
            }
        }

        public async Task<bool> FormDataExistsForGuidAndFeature(MultiPageFormDataFeature feature, Guid tempDataGuid)
        {
            try
            {
                if (useRedisCache)
                {
                    string MultiPageFormCacheKey = GetMultiPageFormCacheKey(tempDataGuid, feature.Name);
                    var existingMultiPageFormData =await this.cacheService.GetAsync<MultiPageFormData>(MultiPageFormCacheKey);
                    return existingMultiPageFormData != null;
                }
                else if (_DbConnection != null)
                {
                    MultiPageFormDataService multiPageFormDataService = new MultiPageFormDataService(_DbConnection);

                    var existingMultiPageFormData =
                        multiPageFormDataService.GetMultiPageFormDataByGuidAndFeature(tempDataGuid, feature.Name);
                    return existingMultiPageFormData != null;
                }
                else
                {
                    throw new Exception("Connection object is null or empty");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                if (_DbConnection.State == ConnectionState.Open)
                {
                    _DbConnection.Close();
                }
            }

        }

        private string GetMultiPageFormCacheKey(Guid guid, string? featureName)
        {
            return string.IsNullOrWhiteSpace(featureName) ? $"{guid}:MultiPageFormData" : $"{guid}-{featureName}:MultiPageFormData";
        }

    }
}
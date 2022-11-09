namespace GDS.MultiPageFormData
{
    using GDS.MultiPageFormData.Enums;
    using GDS.MultiPageFormData.Models;
    using GDS.MultiPageFormData.Services;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Newtonsoft.Json;
    using System;
    using System.Data;

    public class MultiPageFormService
    {
        private static IDbConnection _DbConnection;

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

        public static void SetMultiPageFormData(object formData, MultiPageFormDataFeature feature, ITempDataDictionary tempData )
        {
            if (_DbConnection != null)
            {
                var json = JsonConvert.SerializeObject(formData);

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

        public static T GetMultiPageFormData<T>(MultiPageFormDataFeature feature, ITempDataDictionary tempData)
        {
            if (_DbConnection != null)
            {
                MultiPageFormDataService multiPageFormDataService = new MultiPageFormDataService(_DbConnection);


                if (tempData[feature.TempDataKey] == null)
                {
                    throw new Exception("Attempted to get data with no Guid identifier");
                }

                var settings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
                var tempDataGuid = (Guid)tempData.Peek(feature.TempDataKey);
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

        public static void ClearMultiPageFormData(MultiPageFormDataFeature feature, ITempDataDictionary tempData)
        {
            if (_DbConnection != null)
            {
                MultiPageFormDataService multiPageFormDataService = new MultiPageFormDataService(_DbConnection);

                if (tempData[feature.TempDataKey] == null)
                {
                    throw new Exception("Attempted to clear data with no Guid identifier");
                }

                var tempDataGuid = (Guid)tempData.Peek(feature.TempDataKey);
                multiPageFormDataService.DeleteByGuid(tempDataGuid);
                tempData.Remove(feature.TempDataKey);
            }
            else
            {
                throw new Exception("Connection object is null or empty");
            }
        }

        public static bool FormDataExistsForGuidAndFeature(MultiPageFormDataFeature feature, Guid tempDataGuid)
        {
            try
            {
                if (_DbConnection != null)
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

    }
}

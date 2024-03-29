﻿namespace GDS.MultiPageFormData.Services
{
    using System;
    using System.Data;
    using Dapper;
    using GDS.MultiPageFormData.Models;

    internal interface IMultiPageFormDataService
    {
        MultiPageFormData? GetMultiPageFormDataByGuidAndFeature(Guid tempDataGuid, string feature);

        void InsertMultiPageFormData(MultiPageFormData multiPageFormData);

        void UpdateJsonByGuid(Guid tempDataGuid, string json);

        void DeleteByGuid(Guid tempDataGuid);
    }

    internal class MultiPageFormDataService : IMultiPageFormDataService
    {
        private readonly IDbConnection connection;

        public MultiPageFormDataService(IDbConnection connection)
        {
            this.connection = connection;
        }

        public MultiPageFormData? GetMultiPageFormDataByGuidAndFeature(Guid tempDataGuid, string feature)
        {
            return connection.QuerySingleOrDefault<MultiPageFormData>(
                @"SELECT
                        ID,
                        TempDataGuid,
                        Json,
                        Feature,
                        CreatedDate
                    FROM MultiPageFormData
                    WHERE TempDataGuid = @tempDataGuid AND Feature = @feature",
                new { tempDataGuid, feature }
            );
        }

        public void InsertMultiPageFormData(MultiPageFormData multiPageFormData)
        {
            connection.Execute(
                @"INSERT INTO MultiPageFormData (TempDataGuid, Json, Feature, CreatedDate)
                    VALUES (@TempDataGuid, @Json, @Feature, @CreatedDate)",
                multiPageFormData
            );
        }

        public void UpdateJsonByGuid(Guid tempDataGuid, string json)
        {
            connection.Execute(
                @"UPDATE MultiPageFormData SET Json = @json WHERE TempDataGuid = @tempDataGuid",
                new { tempDataGuid, json }
            );
        }

        public void DeleteByGuid(Guid tempDataGuid)
        {
            connection.Execute(
                @"DELETE FROM MultiPageFormData WHERE TempDataGuid = @tempDataGuid",
                new { tempDataGuid }
            );
        }

    }
}

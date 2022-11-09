namespace GDS.MultiPageFormData.Services
{
    using Dapper;
    using System;
    using System.Data;

    internal interface IDbService
    {
        int CreateTable();
        bool IsDbTableExist();
    }

    internal class DbService : IDbService
    {
        private readonly IDbConnection connection;
        public DbService(IDbConnection connection)
        {
            this.connection = connection;
        }

        public int CreateTable()
        {
            int Result = 1;
            try
            {
                connection.Execute(@"CREATE TABLE [MultiPageFormData](
	                                    [ID] [int] IDENTITY(1,1) NOT NULL ,
	                                    [TempDataGuid] [uniqueidentifier] NOT NULL,
	                                    [Json] [nvarchar](max) NOT NULL,
	                                    [Feature] [nvarchar](100) NOT NULL,
	                                    [CreatedDate] [datetime] NOT NULL,
                                        CONSTRAINT PK_MultiPageFormData PRIMARY KEY (ID))");
            }
            catch (Exception)
            {
                Result = 0;
            }
            return Result;
        }

        public bool IsDbTableExist()
        {
            bool IsExists = false;
            try
            {
                int existingId = (int)connection.ExecuteScalar(@"SELECT count(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'MultiPageFormData'");
                if (existingId > 0)
                    IsExists = true;
            }
            catch (Exception)
            {
                
            }
            return IsExists;
        }

    }
}

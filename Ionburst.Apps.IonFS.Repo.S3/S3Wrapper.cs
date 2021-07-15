// Copyright Cyborn Limited 2020
using System;
using System.IO;
using static System.Environment;

using Microsoft.Extensions.Configuration;

using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;

namespace Ionburst.Apps.IonFS.Repo.S3
{
    internal class S3Wrapper
    {
        private readonly IConfiguration configuration;
        internal IAmazonS3 S3;
        private string _bucket;

        internal S3Wrapper()
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .AddJsonFile($"{GetFolderPath(SpecialFolder.UserProfile, SpecialFolderOption.None)}/.ionfs/appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            AWSOptions awsOptions = configuration.GetAWSOptions();
            S3 = awsOptions.CreateServiceClient<IAmazonS3>();

            _bucket = configuration["S3_BUCKET_NAME"];
        }

        internal string GetBucket()
        {
            return _bucket;
        }

        internal void SetBucket(string bucket)
        {
            _bucket = bucket;
        }
    }
}


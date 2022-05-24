using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System;
using System.Configuration;
using Umbraco.Core;
using Umbraco.Core.Logging.Serilog;
using Umbraco.Web;
using Umbraco.Web.Runtime;

namespace GlobalCMSUmbraco.Web
{
    public class GlobalCmsUmbracoApplication : UmbracoApplication
    {
        protected override IRuntime GetRuntime()
        {
            LoggerConfiguration loggerConfig = new LoggerConfiguration()
                                                    .MinimalConfiguration()
                                                    .ReadFromConfigFile()
                                                    .ReadFromUserConfigFile();

            string awsAccessKey = ConfigurationManager.AppSettings["AWSAccessKey"];
            string awsSecretKey = ConfigurationManager.AppSettings["AWSSecretKey"];
            string bucketName = ConfigurationManager.AppSettings["BucketLogging:BucketName"];
            string awsLogPrefix = ConfigurationManager.AppSettings["BucketLogging:LogPrefix"] ?? "";

            string disabledReason = null;
            if (string.IsNullOrWhiteSpace(awsAccessKey))
            {
                disabledReason = "the AppSetting AWSAccessKey is not populated";
            }
            else if (string.IsNullOrWhiteSpace(awsSecretKey))
            {
                disabledReason = "the AppSetting AWSSecretKey is not populated";
            }
            else if (string.IsNullOrWhiteSpace(bucketName))
            {
                disabledReason = "the AppSetting BucketLogging:BucketName is not populated";
            }
            else {
                // Add the S3 Sink

                string endpointSetting = ConfigurationManager.AppSettings["BucketLogging:Region"].IfNullOrWhiteSpace("eu-west-1");
                Amazon.RegionEndpoint endpoint = Amazon.RegionEndpoint.GetBySystemName(endpointSetting);

                // Warning, Debug, Information, etc
                string logLevelSetting = ConfigurationManager.AppSettings["BucketLogging:MinimumLoggingLevel"];

                if (!Enum.TryParse(logLevelSetting, ignoreCase: true, out LogEventLevel minimumLevel))
                {
                    minimumLevel = LogEventLevel.Warning;
                }

                loggerConfig = loggerConfig
                                        .WriteTo
                                            .AmazonS3(
                                                client: new Amazon.S3.AmazonS3Client(awsAccessKey, awsSecretKey, endpoint),
                                                path: Server.MapPath($"~/App_Data/Temp/AmazonS3Sink/UmbracoTraceLog.{Environment.MachineName}.log.json"),
                                                bucketName: bucketName,
                                                bucketPath: awsLogPrefix.EnsureEndsWith('/'),
                                                formatter: new CompactJsonFormatter(),
                                                levelSwitch: new LoggingLevelSwitch { MinimumLevel = minimumLevel },
                                                rollingInterval: Serilog.Sinks.AmazonS3.RollingInterval.Minute);
            }

            SerilogLogger logger = new SerilogLogger(loggerConfig);


            if (disabledReason != null)
            {
                logger.Warn(typeof(GlobalCmsUmbracoApplication), "S3 Logging is not enabled because " + disabledReason);
            }


            WebRuntime runtime = new WebRuntime(this, logger, GetMainDom(logger));

            return runtime;
        }
    }
}
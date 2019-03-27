# Konnect.Log4NetAlaAppender

This is a small, but hopefully a production-grade Log4Net adapter for Azure Log Analytics.

This appender uses BufferedAppenderSkeleton class from Log4Net, which allows this project to be free of concerns such as buffering and flushing.

The log messages are sent using the [Log Analytics Data Collector API](https://docs.microsoft.com/en-us/azure/log-analytics/log-analytics-data-collector-api) asynchronously (using Task.Run), but failed attempts are not retried currently.

## Captured fields

Currently, Log4net.Core.LoggingEvent objects are mapped to JSON (as required by the Data Collector API) using this code:

```C#
var eventsToSend = events.Select(e => new
                {
                    Level = e.Level.Name, 
                    e.LoggerName, 
                    Message = e.RenderedMessage, 
                    e.TimeStamp, 
                    e.Identity,
                    e.ThreadName, 
                    e.UserName,
                    Exception = e.GetExceptionString()
                });
                
var json = JsonConvert.SerializeObject(eventsToSend);
```

The mapping function is currently not configurable.


## Usage

Add this appender description in your Log4Net configuration:

```xml
<appender name="AzureLogAppender" type="Konnect.Log4NetAzureLogAnalytics.KnlAlaAppender, Konnect.Log4NetAlaAppender">
  <WorkspaceId>#{YourWorkspaceId}</WorkspaceId>
  <SharedKey>#{YourSharedKey}</SharedKey>
  <LogType>#{YourLogType}</LogType>
</appender>
```

Please note, that if #{YourLogType} includes a space, it will be stripped - otherwise, ALA API will return an error. 
Any other whitespace characters are not stripped, so please be careful.

And then, add you can reference this logger from the logger configuration - for example:

```xml
<root>
  <level value="INFO" />
  <appender-ref ref="AzureLogAppender" />
</root>
```  

There is a Debug feature, in case there is a problem with connectivity - you can set the EnableDebugLog option in the appender configuration like this:

```xml
<appender name="AzureLogAppender" type="Konnect.Log4NetAzureLogAnalytics.KnlAlaAppender, Konnect.Log4NetAlaAppender">
  <WorkspaceId>#{YourWorkspaceId}</WorkspaceId>
  ...
  <EnableDebugLog>true</EnableDebugLog>
</appender>
```

The debug output will be sent to the Console as well as the Debug output, which can be viewed through tools such as [SysInternal's DebugView](https://docs.microsoft.com/en-us/sysinternals/downloads/debugview)

If you're trying to troubleshoot this in a release environment, the Debug output will be disabled. You can use the Debug log file feature, like this:

```xml
<appender name="AzureLogAppender" type="Konnect.Log4NetAzureLogAnalytics.KnlAlaAppender, Konnect.Log4NetAlaAppender">
  <WorkspaceId>#{YourWorkspaceId}</WorkspaceId>
  ...
  <EnableDebugLog>true</EnableDebugLog>
  <DebugLogFile>C:\temp\ALAAppenderLog.log</DebugLogFile>
</appender>

``` 

There is also the master switch, which is enabled by default if the element is missing. This is useful for temporarily stopping logging. 

```xml
<appender name="AzureLogAppender" type="Konnect.Log4NetAzureLogAnalytics.KnlAlaAppender, Konnect.Log4NetAlaAppender">
  ...
  <IsEnabled>false</IsEnabled>
  ...
</appender>

``` 




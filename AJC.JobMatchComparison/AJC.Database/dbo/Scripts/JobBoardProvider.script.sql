PRINT 'Updating JobBoardProvider values'

SET DATEFORMAT mdy

SET NOCOUNT ON

DECLARE @DeleteMissingJobBoardProviderRecords BIT
SET @DeleteMissingJobBoardProviderRecords = 0

DECLARE @tblJobBoardProviderTempTable TABLE(
	[JobBoardProviderId] [int] NOT NULL,
	[JobBoardApplicationId] [varchar](25) NULL,
	[JobBoardName] [nvarchar](200) NOT NULL,
	[FeedUrl] [nvarchar](2048) NOT NULL,
	[AuthenticationType] [varchar](30) NOT NULL,
	[CredentialReference] [nvarchar](512) NULL,
	[ApiKeyHeaderName] [nvarchar](200) NULL,
	[ApiKeyQueryParameterName] [nvarchar](200) NULL,
	[OAuthTokenUrl] [nvarchar](2048) NULL,
	[ExpectedResponseType] [varchar](50) NOT NULL,
	[PaginationType] [varchar](30) NOT NULL,
	[PageParameterName] [nvarchar](200) NULL,
	[PageSizeParameterName] [nvarchar](200) NULL,
	[PageSize] [int] NULL,
	[OffsetParameterName] [nvarchar](200) NULL,
	[ContinuationTokenPath] [nvarchar](1000) NULL,
	[NextPageUrlPath] [nvarchar](1000) NULL,
	[MaximumPages] [int] NOT NULL,
	[MaximumResults] [int] NULL,
	[MaximumRetryCount] [int] NOT NULL,
	[InitialRetryDelaySeconds] [int] NOT NULL,
	[MaximumRetryDelaySeconds] [int] NOT NULL,
	[MaximumRateLimitWaitSeconds] [int] NOT NULL,
	[IsEnabled] [bit] NOT NULL
)

INSERT INTO @tblJobBoardProviderTempTable (
	[JobBoardProviderId], JobBoardApplicationId, JobBoardName, FeedUrl,
	AuthenticationType, CredentialReference, ApiKeyHeaderName, ApiKeyQueryParameterName, OAuthTokenUrl,
	ExpectedResponseType, PaginationType, PageParameterName, PageSizeParameterName, PageSize,
	OffsetParameterName, ContinuationTokenPath, NextPageUrlPath, MaximumPages, MaximumResults,
	MaximumRetryCount, InitialRetryDelaySeconds, MaximumRetryDelaySeconds, MaximumRateLimitWaitSeconds, IsEnabled
)
VALUES (
	1, 'c13d16f5', 'Adzuna', 'https://api.adzuna.com/v1/api/jobs/us/search',
	'ApiKey', 'adzuna-apikey', NULL, 'app_key', NULL,
	'adzuna-jobs-v1', 'PageNumber', 'page', 'results_per_page', 25,
	NULL, NULL, NULL, 10, 250,
	3, 1, 30, 300, 1
)


SET IDENTITY_INSERT dbo.JobBoardProvider ON

INSERT INTO dbo.JobBoardProvider (
	JobBoardProviderId, JobBoardApplicationId, JobBoardName, FeedUrl,
	AuthenticationType, CredentialReference, ApiKeyHeaderName, ApiKeyQueryParameterName, OAuthTokenUrl,
	ExpectedResponseType, PaginationType, PageParameterName, PageSizeParameterName, PageSize,
	OffsetParameterName, ContinuationTokenPath, NextPageUrlPath, MaximumPages, MaximumResults,
	MaximumRetryCount, InitialRetryDelaySeconds, MaximumRetryDelaySeconds, MaximumRateLimitWaitSeconds, IsEnabled
)
SELECT
	tmp.JobBoardProviderId, tmp.JobBoardApplicationId, tmp.JobBoardName, tmp.FeedUrl,
	tmp.AuthenticationType, tmp.CredentialReference, tmp.ApiKeyHeaderName, tmp.ApiKeyQueryParameterName, tmp.OAuthTokenUrl,
	tmp.ExpectedResponseType, tmp.PaginationType, tmp.PageParameterName, tmp.PageSizeParameterName, tmp.PageSize,
	tmp.OffsetParameterName, tmp.ContinuationTokenPath, tmp.NextPageUrlPath, tmp.MaximumPages, tmp.MaximumResults,
	tmp.MaximumRetryCount, tmp.InitialRetryDelaySeconds, tmp.MaximumRetryDelaySeconds, tmp.MaximumRateLimitWaitSeconds, tmp.IsEnabled
FROM @tblJobBoardProviderTempTable tmp
LEFT JOIN dbo.JobBoardProvider tbl ON tbl.[JobBoardProviderId] = tmp.[JobBoardProviderId]
WHERE tbl.[JobBoardProviderId] IS NULL

SET IDENTITY_INSERT dbo.JobBoardProvider OFF

UPDATE LiveTable SET
LiveTable.JobBoardApplicationId = tmp.JobBoardApplicationId,
LiveTable.JobBoardName = tmp.JobBoardName,
LiveTable.FeedUrl = tmp.FeedUrl,
LiveTable.AuthenticationType = tmp.AuthenticationType,
LiveTable.CredentialReference = tmp.CredentialReference,
LiveTable.ApiKeyHeaderName = tmp.ApiKeyHeaderName,
LiveTable.ApiKeyQueryParameterName = tmp.ApiKeyQueryParameterName,
LiveTable.OAuthTokenUrl = tmp.OAuthTokenUrl,
LiveTable.ExpectedResponseType = tmp.ExpectedResponseType,
LiveTable.PaginationType = tmp.PaginationType,
LiveTable.PageParameterName = tmp.PageParameterName,
LiveTable.PageSizeParameterName = tmp.PageSizeParameterName,
LiveTable.PageSize = tmp.PageSize,
LiveTable.OffsetParameterName = tmp.OffsetParameterName,
LiveTable.ContinuationTokenPath = tmp.ContinuationTokenPath,
LiveTable.NextPageUrlPath = tmp.NextPageUrlPath,
LiveTable.MaximumPages = tmp.MaximumPages,
LiveTable.MaximumResults = tmp.MaximumResults,
LiveTable.MaximumRetryCount = tmp.MaximumRetryCount,
LiveTable.InitialRetryDelaySeconds = tmp.InitialRetryDelaySeconds,
LiveTable.MaximumRetryDelaySeconds = tmp.MaximumRetryDelaySeconds,
LiveTable.MaximumRateLimitWaitSeconds = tmp.MaximumRateLimitWaitSeconds,
LiveTable.IsEnabled = tmp.IsEnabled
FROM dbo.JobBoardProvider LiveTable
INNER JOIN @tblJobBoardProviderTempTable tmp ON LiveTable.[JobBoardProviderId] = tmp.[JobBoardProviderId]

IF @DeleteMissingJobBoardProviderRecords = 1
BEGIN
	DELETE FROM dbo.JobBoardProvider FROM dbo.JobBoardProvider LiveTable
	LEFT JOIN @tblJobBoardProviderTempTable tmp ON LiveTable.[JobBoardProviderId] = tmp.[JobBoardProviderId]
	WHERE tmp.[JobBoardProviderId] IS NULL
END

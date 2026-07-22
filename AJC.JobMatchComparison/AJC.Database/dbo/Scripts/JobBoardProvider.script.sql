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
	[CredentialReference] [nvarchar](512) NULL,
	[ExpectedResponseType] [varchar](50) NOT NULL,
	[IsEnabled] [bit] NOT NULL
)

INSERT INTO @tblJobBoardProviderTempTable ([JobBoardProviderId], JobBoardApplicationId, JobBoardName, FeedUrl, CredentialReference, ExpectedResponseType, IsEnabled) VALUES (1,'c13d16f5', 'Adzuna', 'https://api.adzuna.com/v1/api/jobs/us/search', 'adzuna-apikey', 'adzuna-jobs-v1', 1)


SET IDENTITY_INSERT dbo.JobBoardProvider ON

INSERT INTO dbo.JobBoardProvider (JobBoardProviderId, JobBoardApplicationId, JobBoardName, FeedUrl, CredentialReference, ExpectedResponseType, IsEnabled)
SELECT tmp.[JobBoardProviderId], tmp.JobBoardApplicationId, tmp.JobBoardName, tmp.FeedUrl, tmp.CredentialReference, tmp.ExpectedResponseType, tmp.IsEnabled
FROM @tblJobBoardProviderTempTable tmp
LEFT JOIN dbo.JobBoardProvider tbl ON tbl.[JobBoardProviderId] = tmp.[JobBoardProviderId]
WHERE tbl.[JobBoardProviderId] IS NULL

SET IDENTITY_INSERT dbo.JobBoardProvider OFF

UPDATE LiveTable SET
LiveTable.JobBoardApplicationId = tmp.JobBoardApplicationId, LiveTable.JobBoardName = tmp.JobBoardName, LiveTable.FeedUrl = tmp.FeedUrl, LiveTable.CredentialReference = tmp.CredentialReference, LiveTable.ExpectedResponseType = tmp.ExpectedResponseType, LiveTable.IsEnabled = tmp.IsEnabled
FROM dbo.JobBoardProvider LiveTable
INNER JOIN @tblJobBoardProviderTempTable tmp ON LiveTable.[JobBoardProviderId] = tmp.[JobBoardProviderId]

IF @DeleteMissingJobBoardProviderRecords = 1
BEGIN
	DELETE FROM dbo.JobBoardProvider FROM dbo.JobBoardProvider LiveTable
	LEFT JOIN @tblJobBoardProviderTempTable tmp ON LiveTable.[JobBoardProviderId] = tmp.[JobBoardProviderId]
	WHERE tmp.[JobBoardProviderId] IS NULL
END

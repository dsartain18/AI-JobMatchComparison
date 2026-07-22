PRINT 'Updating JobSearchCriteria values'

SET DATEFORMAT mdy

SET NOCOUNT ON

DECLARE @DeleteMissingJobSearchCriteriaRecords BIT
SET @DeleteMissingJobSearchCriteriaRecords = 0

DECLARE @tblJobSearchCriteriaTempTable TABLE(
	[JobSearchCriteriaId] int,
	[JobSearchCriteriaDescription] VARCHAR(50)
)

INSERT INTO @tblJobSearchCriteriaTempTable ([JobSearchCriteriaId], [JobSearchCriteriaDescription]) VALUES (1, 'Software Engineer')
INSERT INTO @tblJobSearchCriteriaTempTable ([JobSearchCriteriaId], [JobSearchCriteriaDescription]) VALUES (2, 'Staff Engineer')
INSERT INTO @tblJobSearchCriteriaTempTable ([JobSearchCriteriaId], [JobSearchCriteriaDescription]) VALUES (3, 'Lead Engineer')

SET IDENTITY_INSERT dbo.JobSearchCriteria ON

INSERT INTO dbo.JobSearchCriteria ([JobSearchCriteriaId], [JobSearchCriteriaDescription])
SELECT tmp.[JobSearchCriteriaId], tmp.[JobSearchCriteriaDescription]
FROM @tblJobSearchCriteriaTempTable tmp
LEFT JOIN dbo.JobSearchCriteria tbl ON tbl.[JobSearchCriteriaId] = tmp.[JobSearchCriteriaId]
WHERE tbl.[JobSearchCriteriaId] IS NULL

SET IDENTITY_INSERT dbo.JobSearchCriteria OFF

UPDATE LiveTable SET
LiveTable.[JobSearchCriteriaDescription] = tmp.[JobSearchCriteriaDescription]
FROM dbo.JobSearchCriteria LiveTable
INNER JOIN @tblJobSearchCriteriaTempTable tmp ON LiveTable.[JobSearchCriteriaId] = tmp.[JobSearchCriteriaId]

IF @DeleteMissingJobSearchCriteriaRecords = 1
BEGIN
	DELETE FROM dbo.JobSearchCriteria FROM dbo.JobSearchCriteria LiveTable
	LEFT JOIN @tblJobSearchCriteriaTempTable tmp ON LiveTable.[JobSearchCriteriaId] = tmp.[JobSearchCriteriaId]
	WHERE tmp.[JobSearchCriteriaId] IS NULL
END

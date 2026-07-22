CREATE TABLE [dbo].[JobSearchCriteria] (
    [JobSearchCriteriaId]          INT          IDENTITY (1, 1) NOT NULL,
    [JobSearchCriteriaDescription] VARCHAR (50) NULL,
    PRIMARY KEY CLUSTERED ([JobSearchCriteriaId] ASC)
);


-- =====================================================================================================
-- Author:        Rock
-- Create Date: 
-- Modified Date: 03-11-2025
-- Description:   Populates pledges for semi-random people up to the @PledgeCount.
--
-- Change History:
--                 03-12-2025 NA - Add this script description and added a way to randomize pledge totals.
-- ======================================================================================================

DECLARE @StartDate DATE = '2025-1-1'
	,@EndDate DATE = '2025-12-31'
	,@PledgeCount int = 999
    ,@UseRandomPledgeTotal BIT = 1;

-- ------------------------------------------------------------------------------------------------------

-- Setup some variables for a right-skewed (positively skewed) distribution (if @UseRandomPledgeTotal is enabled)
DECLARE @Min INT = 100, @Max INT = 5000, @SkewFactor FLOAT = 2.5;

INSERT INTO [dbo].[FinancialPledge] (
	[AccountId]
	,[TotalAmount]
	,[PledgeFrequencyValueId]
	,[StartDate]
	,[EndDate]
	,[Guid]
	,[PersonAliasId]
	,[GroupId]
	)
SELECT TOP (@PledgeCount) fa.Id [AccountId]
    , CASE 
        WHEN @UseRandomPledgeTotal = 1 THEN 
           CAST( (@Min + (@Max - @Min) * POWER(ABS(CHECKSUM(NEWID())) % 10000 / 10000.0, @SkewFactor)) / 10 * 10 AS INT )
        ELSE 1234.56 
    END AS [TotalAmount]
	,pfdv.Id [PledgeFrequencyValueId]
	,@StartDate [StartDate]
	,@EndDate [EndDate]
	,newid() [Guid]
	,pa.Id [PersonAliasId]
	,NULL [GroupId]
FROM FinancialAccount fa
	,DefinedValue pfdv
	,PersonAlias pa
WHERE pfdv.DefinedTypeId = (
		SELECT TOP 1 Id
		FROM DefinedType
		WHERE [Guid] = '1F645CFB-5BBD-4465-B9CA-0D2104A1479B'
		) /* Recurring Transaction Frequency */
	AND pa.PersonId = pa.AliasPersonId
	AND pa.PersonId % 10 = 0

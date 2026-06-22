SET NOCOUNT ON;

DECLARE @SlotDate DATE = '2026-04-13';
DECLARE @RangeStart TIME(0) = '10:20';
DECLARE @RangeEnd TIME(0) = '12:00';
DECLARE @IntervalMinutes INT = 5;
DECLARE @Capacity INT = 1;

IF @IntervalMinutes <= 0
BEGIN
    THROW 50001, 'IntervalMinutes must be greater than 0.', 1;
END;

IF @Capacity <= 0
BEGIN
    THROW 50002, 'Capacity must be greater than 0.', 1;
END;

IF @RangeEnd <= @RangeStart
BEGIN
    THROW 50003, 'RangeEnd must be greater than RangeStart.', 1;
END;

IF OBJECT_ID('dbo.InterviewSlots', 'U') IS NULL
BEGIN
    THROW 50004, 'InterviewSlots table does not exist.', 1;
END;

DECLARE @CurrentStart TIME(0) = @RangeStart;
DECLARE @CurrentEnd TIME(0);
DECLARE @Inserted INT = 0;
DECLARE @Skipped INT = 0;

WHILE @CurrentStart < @RangeEnd
BEGIN
    SET @CurrentEnd = CAST(DATEADD(MINUTE, @IntervalMinutes, CAST(@CurrentStart AS DATETIME2(0))) AS TIME(0));

    IF @CurrentEnd > @RangeEnd
    BEGIN
        SET @CurrentEnd = @RangeEnd;
    END;

    IF @CurrentEnd <= @CurrentStart
    BEGIN
        BREAK;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.InterviewSlots
        WHERE [Date] = @SlotDate
          AND @CurrentStart < EndTime
          AND @CurrentEnd > StartTime
    )
    BEGIN
        INSERT INTO dbo.InterviewSlots ([Date], StartTime, EndTime, Capacity, CreatedAt)
        VALUES (@SlotDate, @CurrentStart, @CurrentEnd, @Capacity, SYSUTCDATETIME());

        SET @Inserted += 1;
    END
    ELSE
    BEGIN
        SET @Skipped += 1;
    END;

    SET @CurrentStart = @CurrentEnd;
END;

SELECT
    @Inserted AS InsertedSlots,
    @Skipped AS SkippedSlots,
    @SlotDate AS SlotDate,
    @RangeStart AS RangeStart,
    @RangeEnd AS RangeEnd,
    @IntervalMinutes AS IntervalMinutes,
    @Capacity AS Capacity;

SELECT
    Id,
    [Date],
    CONVERT(VARCHAR(5), StartTime, 108) AS StartTime,
    CONVERT(VARCHAR(5), EndTime, 108) AS EndTime,
    Capacity,
    CreatedAt
FROM dbo.InterviewSlots
WHERE [Date] = @SlotDate
ORDER BY StartTime;

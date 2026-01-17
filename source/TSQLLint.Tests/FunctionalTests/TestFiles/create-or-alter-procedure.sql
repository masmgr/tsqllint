CREATE OR ALTER PROCEDURE dbo.HistoryProc
AS
BEGIN
    DECLARE @Var1 VARCHAR(100)
    DECLARE @Var2 VARCHAR(100)
    DECLARE @Var3 VARCHAR(100)

    SET @Var1 = 'Test1'
    SET @Var2 = 'Test2'
    SET @Var3 = 'Test3'

    SELECT @Var1, @Var2, @Var3
END

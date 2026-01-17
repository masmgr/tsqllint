-- Test file for CREATE OR ALTER syntax with SET statements
-- This reproduces issue #337

CREATE OR ALTER PROCEDURE dbo.TestProc
AS
BEGIN
    DECLARE @Var1 VARCHAR(100);
    SELECT @Var1 = 'Test';
    SELECT @Var1;
END

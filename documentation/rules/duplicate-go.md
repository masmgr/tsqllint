# Disallow duplicate GO statements

## Rule Details

This rule disallows consecutive `GO` batch separators. It flags a `GO` that
directly follows another `GO`, ignoring whitespace, comments, and semicolons.

Examples of **incorrect** code for this rule:

```tsql
SELECT user_name
FROM dbo.MyTable;
GO
GO

SELECT user_id
FROM dbo.MyOtherTable;
```

Examples of **correct** code for this rule:

```tsql
SELECT user_name
FROM dbo.MyTable;
GO

SELECT user_id
FROM dbo.MyOtherTable;
```

# Disallow consecutive empty lines

## Rule Details

This rule disallows multiple empty lines in a row. It flags the second and
subsequent blank lines in any consecutive sequence.

Examples of **incorrect** code for this rule:

```tsql
SELECT user_name
FROM dbo.MyTable;


SELECT user_id
FROM dbo.MyOtherTable;
```

Examples of **correct** code for this rule:

```tsql
SELECT user_name
FROM dbo.MyTable;

SELECT user_id
FROM dbo.MyOtherTable;
```
